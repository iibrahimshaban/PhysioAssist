import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import {
  InitialReportService,
  InitialReportResponse,
  ReportAttachmentResponse,
} from '../../Core/Services/initial-report.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';

interface ChatMessage {
  from: 'user' | 'assistant';
  text: string;
  time?: string;
}

interface AttachmentEntry {
  id?: string;
  name: string;
  size: number;
  fileUrl?: string;
  fileType?: string;
}

const PATIENT_CATEGORY_LABELS = ['Orthopedic', 'Neurological', 'Pediatric', 'General / Other'];

@Component({
  selector: 'app-initial-report',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
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
  initialReport = signal('');
  treatmentPlan = signal('');
  attachments = signal<AttachmentEntry[]>([]);
  treatmentPlanPdfUrl = signal<string | undefined>(undefined);

  saving = signal(false);

  // --- Voice input (MediaRecorder -> backend transcribe/refine pipeline) ---
  activeVoiceField: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan' | null = null;
  listening = signal(false);
  liveTranscript = signal('');
  private mediaRecorder: MediaRecorder | null = null;
  private recordedChunks: BlobPart[] = [];

  recordingFieldLabel = computed(() => {
    switch (this.activeVoiceField) {
      case 'examination':
        return 'examination notes';
      case 'initialReport':
        return 'initial report';
      case 'treatmentPlan':
        return 'treatment plan';
      default:
        return 'voice note';
    }
  });

  // --- AI chat (WIP — backend endpoint not confirmed yet) ---
  chatText = '';
  messages = signal<ChatMessage[]>([]);
  sending = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService
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
   *  — expected for a brand new patient, not an error to surface. */
  private loadOrCreateReport(patientId: string): void {
    this.initialReportService.getReportByPatientId(patientId).subscribe({
      next: res => this.applyReportResponse(res),
      error: err => {
        if (err?.status === 404) {
          this.initialReportService.createReport({ patientId }).subscribe({
            next: res => this.applyReportResponse(res),
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

  private applyReportResponse(response: InitialReportResponse): void {
    this.reportId = response.id;
    this.treatmentPlanPdfUrl.set(response.treatmentPlanPdfUrl);
    this.parseReportText(response.reportText ?? '');
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

  /** Splits the combined reportText field back into the three UI sections. */
  private parseReportText(reportText: string): void {
    const examinationMatch = reportText.match(/=== Examination ===([\s\S]*?)(?=== Diagnosis ===|$)/);
    const diagnosisMatch = reportText.match(/=== Diagnosis ===([\s\S]*?)(?=== Treatment Plan ===|$)/);
    const treatmentMatch = reportText.match(/=== Treatment Plan ===([\s\S]*?)$/);

    this.examination.set(examinationMatch?.[1]?.trim() ?? '');
    this.initialReport.set(diagnosisMatch?.[1]?.trim() ?? '');
    this.treatmentPlan.set(treatmentMatch?.[1]?.trim() ?? '');
  }

  /** Combines the three UI sections into the single reportText field the
   *  backend stores. Mirrors parseReportText's markers so a reload restores
   *  all three fields correctly. */
  private buildReportText(): string {
    return `=== Examination ===\n${this.examination().trim()}\n=== Diagnosis ===\n${this.initialReport().trim()}\n=== Treatment Plan ===\n${this.treatmentPlan().trim()}`;
  }

  private getApiErrorDetail(err: any): string {
    return err?.error?.detail || err?.error?.message || err?.statusText || err?.message || '';
  }

  startVoice(field: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan'): void {
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
  private transcribeVoice(audioBlob: Blob, field: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan'): void {
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
          case 'chatText':
            this.chatText = trimmedText;
            break;
          case 'examination':
            this.examination.set(trimmedText);
            break;
          case 'initialReport':
            this.initialReport.set(trimmedText);
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

  // --- AI chat (WIP) ------------------------------------------------

  addUserMessage(text: string): void {
    this.messages.update(msgs => [...msgs, { from: 'user', text, time: new Date().toLocaleTimeString() }]);
  }

  addAssistantMessage(text: string): void {
    this.messages.update(msgs => [...msgs, { from: 'assistant', text, time: new Date().toLocaleTimeString() }]);
  }

  sendToAi(text: string): void {
    if (!text || this.sending() || !this.patientId) return;
    this.sending.set(true);
    this.initialReportService.sendChatMessage({ patientId: this.patientId, text }).subscribe({
      next: res => {
        this.addAssistantMessage(res?.reply || 'No response from AI.');
        this.sending.set(false);
      },
      error: err => {
        console.error('AI call failed', err);
        this.addAssistantMessage('AI service error.');
        this.sending.set(false);
      }
    });
  }

  onChatSubmit(): void {
    const text = this.chatText?.trim();
    if (!text) return;
    this.chatText = '';
    this.addUserMessage(text);
    this.sendToAi(text);
  }

  compileDraft(): void {
    const combined = this.messages().map(m => `${m.from}: ${m.text}`).join('\n');
    this.sendToAi(`Compile draft from conversation:\n${combined}`);
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.uploadFiles(input.files);
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      this.uploadFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  private uploadFiles(fileList: FileList): void {
    Array.from(fileList).forEach(file => this.uploadAttachmentFile(file));
  }

  private uploadAttachmentFile(file: File): void {
    if (!this.reportId) {
      this.snackbar.warning('Cannot upload attachment', ['Report is still being created — try again in a moment.']);
      return;
    }

    const isImage = file.type.startsWith('image/');
    const isPdf = file.type === 'application/pdf';
    if (!isImage && !isPdf) {
      this.snackbar.error('Invalid attachment', ['Only images and PDFs are allowed.']);
      return;
    }

    this.initialReportService.uploadAttachment(this.reportId, file).subscribe({
      next: (res: ReportAttachmentResponse) => {
        this.attachments.update(list => [
          ...list,
          { id: res.id, name: res.fileName, size: file.size, fileUrl: res.fileUrl, fileType: res.fileType },
        ]);
        this.snackbar.success('Attachment uploaded', [res.fileName]);
      },
      error: err => {
        console.error('Attachment upload failed', err);
        const message =
          err?.status === 400 ? 'Only images and PDFs are allowed.' : this.getApiErrorDetail(err) || 'Unable to upload attachment.';
        this.snackbar.error('Upload failed', [message]);
      }
    });
  }

  removeAttachment(index: number): void {
    const attachment = this.attachments()[index];
    if (!attachment) return;

    if (attachment.id && this.reportId) {
      this.initialReportService.deleteAttachment(this.reportId, attachment.id).subscribe({
        next: () => {
          this.attachments.update(list => list.filter((_, i) => i !== index));
          this.snackbar.success('Attachment removed');
        },
        error: err => {
          console.error('Attachment delete failed', err);
          this.snackbar.error('Delete failed', [this.getApiErrorDetail(err) || 'Unable to remove attachment.']);
        }
      });
      return;
    }

    this.attachments.update(list => list.filter((_, i) => i !== index));
  }

  saveDraft(): void {
    this.persistReportText(() => this.snackbar.success('Draft saved'));
  }

  submitAndSend(): void {
    this.persistReportText(() => {
      if (!this.reportId) return;
      this.initialReportService.submitReport(this.reportId).subscribe({
        next: res => {
          this.applyReportResponse(res);
          const linkMessage = res.treatmentPlanPdfUrl ? `PDF: ${res.treatmentPlanPdfUrl}` : 'Report submitted successfully.';
          this.snackbar.success('Submitted and sent', [linkMessage]);
        },
        error: err => {
          console.error('Submit failed', err);
          this.snackbar.error('Submit failed', [this.getApiErrorDetail(err) || 'Unable to submit report.']);
        }
      });
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

  goToPatients(): void {
    this.router.navigate(['/app/patients']);
  }
}