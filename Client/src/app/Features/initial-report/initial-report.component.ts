import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { from, of } from 'rxjs';
import { catchError, concatMap, map, toArray } from 'rxjs/operators';
import {
  InitialReportService,
  InitialReportResponse,
  ReportAttachmentResponse,
} from '../../Core/Services/initial-report.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';

interface AttachmentEntry {
  id?: string;
  name: string;
  size: number;
  fileUrl?: string;
  fileType?: string;
  /** Present only for attachments the doctor has picked locally but that
   *  haven't been sent to the server yet — they upload when Submit is
   *  pressed, not on selection. Cleared once the upload succeeds. */
  file?: File;
}

const PATIENT_CATEGORY_LABELS = ['Orthopedic', 'Neurological', 'Pediatric', 'General / Other'];

@Component({
  selector: 'app-initial-report',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule, ButtonModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './initial-report.component.html',
  styleUrls: ['./initial-report.component.css'],
})
export class InitialReportComponent implements OnInit {
  patientId: string | null = null;
  reportId: string | null = null;
  noPatientSelected = false;

  // --- Patient header (populated from the intake summary endpoint) ---
  patientName = signal('');
  patientBadge = signal('');
  age = signal<number | null>(null);
  gender = signal('');
  chiefComplaint = signal('');
  injury = signal<string | undefined>(undefined);
  injuryDate = signal<string | undefined>(undefined);
  patientCategory = signal<string | undefined>(undefined);

  patientInitials = computed(() =>
    this.patientName()
      .split(' ')
      .filter(Boolean)
      .map(n => n[0])
      .join('')
  );

  // --- Report fields ---
  examination = signal('');
  treatmentPlan = signal('');
  attachments = signal<AttachmentEntry[]>([]);
  treatmentPlanPdfUrl = signal<string | undefined>(undefined);

  saving = signal(false);

  // --- Readonly / edit gating -----------------------------------------
  // A report that already had saved content when the page loaded opens in
  // readonly mode so the doctor can't accidentally change a finished report
  // — they have to explicitly press "Edit" to unlock it. A brand-new report
  // (nothing saved yet) is editable immediately, since there's nothing to
  // protect against yet.
  isExistingReport = signal(false);
  readonlyMode = signal(false);

  // --- Attachments staged locally, not yet sent to the server ----------
  // Ids of already-uploaded attachments the doctor removed in this session.
  // We don't call the delete endpoint the moment they click "Remove" —
  // that made an accidental click destructive with no way back. Instead we
  // just hide it locally and only actually delete it on the server once
  // the doctor presses Submit.
  private pendingDeletions = signal<string[]>([]);

  // --- Voice input (MediaRecorder -> backend transcribe/refine pipeline) ---
  activeVoiceField: 'examination' | 'treatmentPlan' | null = null;
  listening = signal(false);
  liveTranscript = signal('');
  private mediaRecorder: MediaRecorder | null = null;
  private recordedChunks: BlobPart[] = [];

  recordingFieldLabel = computed(() => {
    switch (this.activeVoiceField) {
      case 'examination':
        return 'examination notes';
      case 'treatmentPlan':
        return 'treatment plan';
      default:
        return 'voice note';
    }
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService,
    private readonly confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    const navState = history.state as { patient?: any } | undefined;
    if (navState?.patient) {
      this.applyPatientSummary(navState.patient);
    }

    // patientId lives on the route path, e.g. /app/initial-report/:patientId
    this.route.paramMap.subscribe(params => {
      const resolvedId = params.get('patientId') ?? (navState?.patient?.id != null ? String(navState.patient.id) : null);

      if (!resolvedId) {
        this.noPatientSelected = true;
        this.snackbar.warning('No patient selected', ['Open this page from a patient in the list.']);
        return;
      }

      this.noPatientSelected = false;
      this.patientId = resolvedId;
      this.loadIntakeHeader(resolvedId);
      this.loadOrCreateReport(resolvedId);
    });
  }

  /** Pre-fills the header card from whatever summary the caller already had,
   *  so the page isn't blank while the intake/report calls are in flight. */
  private applyPatientSummary(patient: any): void {
    const name = patient.name ?? patient.fullName;
    if (name) this.patientName.set(name);
    if (patient.id != null) this.patientBadge.set(`Patient #${patient.id}`);
    if (patient.gender) this.gender.set(patient.gender);
    if (patient.chiefComplaint) this.chiefComplaint.set(patient.chiefComplaint);
  }

  /** Loads the condensed intake summary (name/age/gender/chief complaint/etc.)
   *  to populate the patient header card. */
  private loadIntakeHeader(patientId: string): void {
    this.initialReportService.getIntakeDataSummaryByPatientId(patientId).subscribe({
      next: intake => {
        if (intake.patientFullName) this.patientName.set(intake.patientFullName);
        if (intake.gender) this.gender.set(intake.gender);
        if (intake.age != null) this.age.set(intake.age);
        if (intake.chiefComplaint) this.chiefComplaint.set(intake.chiefComplaint);
        if (intake.injuryDescription) this.injury.set(intake.injuryDescription);
        if (intake.injuryDate) this.injuryDate.set(intake.injuryDate);
        if (intake.patientCategory != null) {
          this.patientCategory.set(PATIENT_CATEGORY_LABELS[intake.patientCategory]);
        }
        this.patientBadge.set(`Patient #${patientId}`);
      },
      error: err => {
        if (err?.status !== 404) {
          console.warn('Unable to load intake data', err);
          this.snackbar.warning('Unable to load intake details', ['Could not load patient intake data.']);
        }
      }
    });
  }

  /** Loads an existing report for this patient if one exists; otherwise
   *  creates an empty one immediately so attachments have somewhere to
   *  upload to right away. A 404 on the lookup just means "no report yet"
   *  — expected for a brand new patient, not an error to surface.
   *
   *  An existing report opens readonly (doctor must press Edit); a freshly
   *  created one opens editable right away since there's nothing saved to
   *  protect yet. */
  private loadOrCreateReport(patientId: string): void {
    this.initialReportService.getReportByPatientId(patientId).subscribe({
      next: res => {
        this.isExistingReport.set(true);
        this.readonlyMode.set(true);
        this.applyReportResponse(res);
      },
      error: err => {
        if (err?.status === 404) {
          this.initialReportService.createReport({ patientId }).subscribe({
            next: res => {
              this.isExistingReport.set(false);
              this.readonlyMode.set(false);
              this.applyReportResponse(res);
            },
            error: createErr => {
              console.error('Unable to create report', createErr);
              this.snackbar.error('Unable to start report', ['Could not create an initial report for this patient.']);
            }
          });
        } else {
          console.warn('Unable to load existing report', err);
          this.snackbar.warning('Unable to load report', ['Could not load an existing report.']);
        }
      }
    });
  }

  /** Unlocks the form for editing. Only meaningful for a report that already
   *  had saved content (readonlyMode is only ever true in that case). */
  enableEdit(): void {
    this.readonlyMode.set(false);
  }

  private applyReportResponse(response: InitialReportResponse): void {
    this.reportId = response.id;
    this.treatmentPlanPdfUrl.set(response.treatmentPlanPdfUrl);
    this.parseReportText(response.reportText ?? '');
    this.pendingDeletions.set([]);
    this.attachments.set(
      (response.attachments ?? []).map(a => ({
        id: a.id,
        name: a.fileName,
        size: 0,
        fileUrl: a.fileUrl,
        fileType: a.fileType,
      }))
    );
  }

  /** Splits the combined reportText field back into the UI sections.
   *  Uses plain indexOf/slice rather than regex lookaheads — an earlier
   *  version used a lookahead that was accidentally short one "=" character,
   *  which caused it to stop capturing one character early and leak a
   *  stray "=" onto its own line at the end of the Examination text every
   *  time a report was loaded and re-saved. stripStrayEqualsArtifact below
   *  cleans up any such leftover lines from reports saved while that bug
   *  was live, so they self-heal the next time they're saved. */
  private parseReportText(reportText: string): void {
    const examinationMarker = '=== Examination ===';
    const treatmentMarker = '=== Treatment Plan ===';

    const treatmentIndex = reportText.indexOf(treatmentMarker);
    const examinationRaw = treatmentIndex >= 0 ? reportText.slice(0, treatmentIndex) : reportText;
    const treatmentRaw = treatmentIndex >= 0 ? reportText.slice(treatmentIndex + treatmentMarker.length) : '';

    const examinationText = examinationRaw.replace(examinationMarker, '').trim();
    const treatmentText = treatmentRaw.trim();

    this.examination.set(this.stripStrayEqualsArtifact(examinationText));
    this.treatmentPlan.set(treatmentText);
  }

  private stripStrayEqualsArtifact(text: string): string {
    return text.replace(/(?:\r?\n=+\s*)+$/, '').trimEnd();
  }

  /** Combines the UI sections into the single reportText field the
   *  backend stores. Mirrors parseReportText's markers so a reload restores
   *  both fields correctly. */
  private buildReportText(): string {
    return `=== Examination ===\n${this.examination().trim()}\n=== Treatment Plan ===\n${this.treatmentPlan().trim()}`;
  }

  private getApiErrorDetail(err: any): string {
    return err?.error?.detail || err?.error?.message || err?.statusText || err?.message || '';
  }

  startVoice(field: 'examination' | 'treatmentPlan'): void {
    if (this.readonlyMode()) return;

    if (this.listening()) {
      this.stopVoice();
      return;
    }

    if (!this.reportId) {
      this.snackbar.warning('Report not ready', ['Please wait until the report finishes loading before recording audio.']);
      return;
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      this.snackbar.warning('Microphone unavailable', ['Your browser does not support audio recording.']);
      return;
    }

    this.activeVoiceField = field;
    this.liveTranscript.set('');
    this.recordedChunks = [];
    this.listening.set(true);

    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then(stream => {
        this.mediaRecorder = new MediaRecorder(stream);
        this.mediaRecorder.ondataavailable = (event: BlobEvent) => {
          if (event.data && event.data.size > 0) {
            this.recordedChunks.push(event.data);
          }
        };
        this.mediaRecorder.onstop = () => {
          this.listening.set(false);
          stream.getTracks().forEach(track => track.stop());
          const audioBlob = new Blob(this.recordedChunks, { type: 'audio/webm' });
          this.transcribeVoice(audioBlob, field);
        };
        this.mediaRecorder.start();
      })
      .catch(err => {
        this.listening.set(false);
        this.activeVoiceField = null;
        console.error('Microphone access denied', err);
        this.snackbar.warning('Microphone access denied', [this.getApiErrorDetail(err) || 'Cannot start recording.']);
      });
  }

  stopVoice(): void {
    if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
      this.listening.set(false);
      this.activeVoiceField = null;
      return;
    }
    this.mediaRecorder.stop();
  }

  /** WIP: teammate's transcription pipeline currently returns the whole
   *  updated report, not a per-field result — so for now this assigns the
   *  returned text wholesale to whichever field was being recorded. */
  private transcribeVoice(audioBlob: Blob, field: 'examination' | 'treatmentPlan'): void {
    if (!this.reportId) {
      this.snackbar.error('Cannot transcribe audio', ['Report ID is missing.']);
      this.activeVoiceField = null;
      return;
    }

    this.initialReportService.transcribeAudio(this.reportId, audioBlob).subscribe({
      next: res => {
        const trimmedText = res.reportText?.trim() ?? '';
        if (!trimmedText) {
          this.snackbar.warning('No transcription result', ['The audio did not return any text.']);
          this.activeVoiceField = null;
          return;
        }

        this.liveTranscript.set(trimmedText);
        switch (field) {
          case 'examination':
            this.examination.set(trimmedText);
            break;
          case 'treatmentPlan':
            this.treatmentPlan.set(trimmedText);
            break;
        }
        this.activeVoiceField = null;
      },
      error: err => {
        console.error('Audio transcription failed', err);
        this.snackbar.error('Transcription failed', [this.getApiErrorDetail(err) || 'Unable to transcribe audio.']);
        this.activeVoiceField = null;
      }
    });
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.stageFiles(input.files);
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    if (this.readonlyMode()) return;
    if (event.dataTransfer?.files) {
      this.stageFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  private isAllowedAttachmentType(file: File): boolean {
    return file.type.startsWith('image/') || file.type === 'application/pdf';
  }

  /** Adds files to the local attachments list only. Nothing is sent to the
   *  server here — uploads are deferred until the doctor presses Submit,
   *  so this just gives the doctor a visible record of what they've
   *  attached so far. */
  private stageFiles(fileList: FileList): void {
    if (this.readonlyMode()) return;

    const files = Array.from(fileList);
    const rejected: string[] = [];

    const staged = files.filter(file => {
      if (this.isAllowedAttachmentType(file)) return true;
      rejected.push(file.name);
      return false;
    });

    if (rejected.length > 0) {
      this.snackbar.error('Invalid attachment', [
        `Only images and PDFs are allowed: ${rejected.join(', ')}`,
      ]);
    }

    if (staged.length === 0) return;

    this.attachments.update(list => [
      ...list,
      ...staged.map(file => ({
        name: file.name,
        size: file.size,
        fileType: file.type,
        file,
      })),
    ]);
  }

  /** Removing an attachment never touches the server immediately — whether
   *  it's a file already saved on a previous submit or one just staged
   *  locally. For an already-saved attachment we just remember its id and
   *  hide it; the actual delete call only fires once the doctor confirms
   *  Submit, so an accidental click doesn't destroy anything on its own —
   *  reloading the page before submitting brings it right back. */
  removeAttachment(index: number): void {
    const attachment = this.attachments()[index];
    if (!attachment) return;

    if (attachment.id) {
      this.pendingDeletions.update(ids => [...ids, attachment.id!]);
    }

    this.attachments.update(list => list.filter((_, i) => i !== index));
  }

  saveDraft(): void {
    this.confirmationService.confirm({
      header: 'Save draft?',
      message: 'Save the current examination, treatment plan, and attachment changes as a draft?',
      icon: 'pi pi-save',
      acceptLabel: 'Save',
      rejectLabel: 'Cancel',
      accept: () => {
        this.persistReportText(() => {
          this.snackbar.success('Draft saved');
          this.isExistingReport.set(true);
        });
      }
    });
  }

  submitAndSend(): void {
    this.confirmationService.confirm({
      header: 'Submit report?',
      message: 'This finalizes the report and sends the treatment plan to the patient. Are you sure you want to continue?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Submit',
      rejectLabel: 'Cancel',
      accept: () => {
        this.persistReportText(() => {
          this.uploadPendingAttachments(() => {
            this.deletePendingAttachments(() => this.finalizeSubmit());
          });
        });
      }
    });
  }

  private finalizeSubmit(): void {
    if (!this.reportId) return;
    this.initialReportService.submitReport(this.reportId).subscribe({
      next: res => {
        this.applyReportResponse(res);
        this.isExistingReport.set(true);
        this.readonlyMode.set(true);
        this.snackbar.success('Submitted and sent', ['Report saved successfully.']);
      },
      error: err => {
        console.error('Submit failed', err);
        this.snackbar.error('Submit failed', [this.getApiErrorDetail(err) || 'Unable to submit report.']);
      }
    });
  }

  /** Uploads any attachments the doctor staged locally but hasn't sent to
   *  the server yet — this is the one point in the flow where new
   *  attachment uploads actually happen. Runs sequentially so a failure is
   *  reported against the specific file, rather than firing everything in
   *  parallel and losing track of which one broke. */
  private uploadPendingAttachments(onDone: () => void): void {
    if (!this.reportId) {
      onDone();
      return;
    }

    const pending = this.attachments().filter(a => !a.id && a.file);
    if (pending.length === 0) {
      onDone();
      return;
    }

    this.saving.set(true);

    from(pending)
      .pipe(
        concatMap(attachment =>
          this.initialReportService.uploadAttachment(this.reportId!, attachment.file!).pipe(
            map((res: ReportAttachmentResponse) => ({ attachment, res, error: null as any })),
            catchError(error => of({ attachment, res: null as ReportAttachmentResponse | null, error }))
          )
        ),
        toArray()
      )
      .subscribe(results => {
        this.saving.set(false);

        this.attachments.update(list =>
          list.map(entry => {
            const result = results.find(r => r.attachment === entry);
            if (!result || !result.res) return entry;
            return {
              id: result.res.id,
              name: result.res.fileName,
              size: entry.size,
              fileUrl: result.res.fileUrl,
              fileType: result.res.fileType,
            };
          })
        );

        const failed = results.filter(r => r.error);
        if (failed.length > 0) {
          const names = failed.map(f => f.attachment.name).join(', ');
          this.snackbar.error('Some attachments failed to upload', [
            `${names} — please retry before submitting.`,
          ]);
          return; // don't finalize submit until every attachment is up
        }

        onDone();
      });
  }

  /** Actually deletes, on the server, whatever attachments the doctor
   *  removed during this editing session. Deferred to Submit time so a
   *  removal made mid-edit isn't destructive on its own. */
  private deletePendingAttachments(onDone: () => void): void {
    const ids = this.pendingDeletions();
    if (!this.reportId || ids.length === 0) {
      onDone();
      return;
    }

    this.saving.set(true);

    from(ids)
      .pipe(
        concatMap(id =>
          this.initialReportService.deleteAttachment(this.reportId!, id).pipe(
            map(() => ({ id, error: null as any })),
            catchError(error => of({ id, error }))
          )
        ),
        toArray()
      )
      .subscribe(results => {
        this.saving.set(false);

        const failed = results.filter(r => r.error);
        if (failed.length > 0) {
          this.snackbar.error('Some attachments failed to remove', [
            `${failed.length} file(s) could not be deleted — they may still appear after reload.`,
          ]);
        }

        this.pendingDeletions.set([]);
        onDone();
      });
  }

  private persistReportText(onSuccess: () => void): void {
    if (!this.reportId) {
      this.snackbar.error('Unable to save', ['Report is still being created — try again in a moment.']);
      return;
    }

    this.saving.set(true);
    const reportText = this.buildReportText();

    this.initialReportService.updateReportText(this.reportId, { reportText }).subscribe({
      next: () => {
        this.saving.set(false);
        onSuccess();
      },
      error: err => {
        this.saving.set(false);
        console.error('Report save failed', err);
        this.snackbar.error('Save failed', [this.getApiErrorDetail(err) || 'Unable to save report.']);
      }
    });
  }

  /** Returns to whatever page the doctor came from (browser history), rather
   *  than assuming they always arrived from the patients list. */
  goBack(): void {
    this.location.back();
  }

  goToPatients(): void {
    this.router.navigate(['/app/patients']);
  }
}