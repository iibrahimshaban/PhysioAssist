import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { InitialReportService } from '../../Core/Services/initial-report.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
// using standard HTML controls to keep this standalone component lightweight

@Component({
  selector: 'app-initial-report',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  templateUrl: './initial-report.component.html',
  styleUrl: './initial-report.component.css',
})
export class InitialReportComponent implements OnInit {

  
  patientId: string | null = null;
  noPatientSelected = false;
  reportId: string | null = null;

  patientName = signal('');
  patientBadge = signal('');
  age = signal<number | null>(null);
  gender = signal('');
  chiefComplaint = signal('');
  readonly injury = signal<string | undefined>(undefined);
  readonly injuryDate = signal<string | undefined>(undefined);
  readonly patientCategory = signal<string | undefined>(undefined);

  patientInitials = computed(() =>
    this.patientName()
      .split(' ')
      .filter(Boolean)
      .map(n => n[0])
      .join('')
  );

  // --- Two-way ngModel-bound fields. Signals here too, since the report load
  // (initialReport) is set asynchronously from an HTTP callback. ---
  examination = signal('');
  initialReport = signal('');
  treatmentPlan = signal('');
  attachments = signal<File[]>([]);
  chatText = '';
  activeVoiceField: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan' | null = null;

  // chat state
  messages = signal<Array<{ from: 'user' | 'assistant'; text: string; time?: string }>>([]);
  sending = signal(false);
  saving = signal(false);
  recognition: any = null;
  listening = signal(false);
  liveTranscript = signal('');

  recordingFieldLabel = computed(() => {
    switch (this.activeVoiceField) {
      case 'examination':
        return 'examination notes';
      case 'initialReport':
        return 'initial report';
      case 'treatmentPlan':
        return 'treatment plan';
      case 'chatText':
        return 'voice note';
      default:
        return 'voice note';
    }
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService
  ) {
    // init speech recognition if available
    const w: any = window as any;
    const SpeechRecognition = w.SpeechRecognition || w.webkitSpeechRecognition;
    if (SpeechRecognition) {
      this.recognition = new SpeechRecognition();
      this.recognition.lang = 'en-US';
      this.recognition.interimResults = false;
      this.recognition.maxAlternatives = 1;
      this.recognition.onresult = (ev: any) => {
        const transcript = Array.from(ev.results)
          .map((result: any) => result[0].transcript)
          .join(' ')
          .trim();

        if (!transcript) {
          return;
        }

        this.liveTranscript.set(transcript);

        switch (this.activeVoiceField) {
          case 'chatText':
            this.chatText = transcript;
            break;
          case 'examination':
            this.examination.set(transcript);
            break;
          case 'initialReport':
            this.initialReport.set(transcript);
            break;
          case 'treatmentPlan':
            this.treatmentPlan.set(transcript);
            break;
          default:
            this.chatText = transcript;
        }
      };
      this.recognition.onend = () => {
        this.listening.set(false);
        this.activeVoiceField = null;
      };
      this.recognition.onerror = () => {
        this.listening.set(false);
        this.activeVoiceField = null;
      };
    }
  }

  ngOnInit(): void {
    const navState = history.state as { patient?: any } | undefined;
    if (navState?.patient) {
      this.applyPatientSummary(navState.patient);
    }

    // patientId is mandatory, so it now lives on the route path, e.g.
    // /app/initial-report/:patientId — see MainLayoutRoutes.
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
      this.loadExistingReport(resolvedId);
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

  private loadIntakeHeader(patientId: string): void {
    const PATIENT_CATEGORY_LABELS = ['Orthopedic', 'Neurological', 'Pediatric', 'General / Other'];

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

  /** Loads an existing report for this patient, if one was already created.
   *  A 404 here just means "no report yet", which is expected for a brand
   *  new patient — not an error to surface. */
  private loadExistingReport(patientId: string): void {
    this.initialReportService.getReportByPatientId(patientId).subscribe({
      next: res => {
        this.reportId = res.id;
        this.initialReport.set(res.reportText ?? '');
      },
      error: err => {
        if (err?.status !== 404) {
          console.warn('Unable to load existing report', err);
          this.snackbar.warning('Unable to load report', ['Could not load an existing report.']);
        }
      }
    });
  }

  addUserMessage(text: string) {
    this.messages.update(msgs => [...msgs, { from: 'user', text, time: new Date().toLocaleTimeString() }]);
  }

  addAssistantMessage(text: string) {
    this.messages.update(msgs => [...msgs, { from: 'assistant', text, time: new Date().toLocaleTimeString() }]);
  }

  startVoice(field: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan') {
    if (!this.recognition) {
      this.snackbar.warning('Speech recognition unavailable', ['Your browser does not support audio transcription.']);
      return;
    }
    if (this.listening()) {
      this.recognition.stop();
      return;
    }
    this.liveTranscript.set('');
    this.activeVoiceField = field;
    this.listening.set(true);
    this.recognition.start();
  }

  stopVoice() {
    if (!this.recognition) return;
    this.recognition.stop();
    this.listening.set(false);
    this.activeVoiceField = null;
  }

  /** @deprecated sendChatMessage hits a legacy ai/initial-report endpoint not
   *  present on InitialReportController — kept only until the chat/AI flow is
   *  reconfirmed against the current backend. */
  sendToAi(text: string) {
    if (!text || this.sending() || this.patientId == null) return;
    this.sending.set(true);
    const payload = { patientId: this.patientId, text };
    this.initialReportService.sendChatMessage(payload).subscribe({
      next: res => {
        const reply = res?.reply || 'No response from AI.';
        this.addAssistantMessage(reply);
        this.sending.set(false);
      },
      error: err => {
        console.error('AI call failed', err);
        this.addAssistantMessage('AI service error.');
        this.sending.set(false);
      }
    });
  }

  onChatSubmit() {
    const text = this.chatText?.trim();
    if (!text) return;
    this.chatText = '';
    this.addUserMessage(text);
    this.sendToAi(text);
  }

  compileDraft() {
    const combined = this.messages().map(m => `${m.from}: ${m.text}`).join('\n');
    this.sendToAi(`Compile draft from conversation:\n${combined}`);
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.addFiles(input.files);
    input.value = '';
  }

  addFiles(fileList: FileList) {
    const newFiles: File[] = [];
    for (let i = 0; i < fileList.length; i++) {
      const file = fileList.item(i);
      if (file) newFiles.push(file);
    }
    if (newFiles.length > 0) {
      this.attachments.update(files => [...files, ...newFiles]);
    }
  }

  removeAttachment(index: number) {
    this.attachments.update(files => files.filter((_, i) => i !== index));
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      this.addFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  /** Combines the three UI sections into the single reportText field the
   *  backend currently stores. Pending decision on whether the backend DTO
   *  should split these into separate columns instead. */
  private buildReportText(): string {
    const parts: string[] = [];
    const examination = this.examination().trim();
    const initialReport = this.initialReport().trim();
    const treatmentPlan = this.treatmentPlan().trim();
    if (examination) parts.push(`Examination:\n${examination}`);
    if (initialReport) parts.push(`Initial Report:\n${initialReport}`);
    if (treatmentPlan) parts.push(`Treatment Plan:\n${treatmentPlan}`);
    return parts.join('\n\n');
  }

  /** Creates the report if none exists yet for this patient, otherwise
   *  updates the existing one's text. Uploads any attachments picked before
   *  the report existed (attachments require a reportId to attach to).
   *  Only image files are accepted by the backend (UploadAttachmentAsync). */
  private persistReport(onSuccess: () => void) {
    if (this.patientId == null) {
      this.snackbar.warning('No patient selected', ['Open this page from a patient in the list.']);
      return;
    }

    this.saving.set(true);
    const reportText = this.buildReportText();

    const afterReportSaved = (reportId: string) => {
      this.reportId = reportId;
      const currentAttachments = this.attachments();
      const imageAttachments = currentAttachments.filter(f => f.type.startsWith('image/'));
      const skipped = currentAttachments.length - imageAttachments.length;
      if (skipped > 0) {
        this.snackbar.warning('Some attachments skipped', [`${skipped} file(s) are not images and were not uploaded.`]);
      }

      if (imageAttachments.length === 0) {
        this.saving.set(false);
        onSuccess();
        return;
      }

      let remaining = imageAttachments.length;
      let hadError = false;
      imageAttachments.forEach(file => {
        this.initialReportService.uploadAttachment(reportId, file).subscribe({
          next: () => {
            remaining--;
            if (remaining === 0) {
              this.saving.set(false);
              this.attachments.set([]);
              if (!hadError) onSuccess();
            }
          },
          error: err => {
            console.error('Attachment upload failed', err);
            hadError = true;
            remaining--;
            if (remaining === 0) {
              this.saving.set(false);
              this.snackbar.error('Attachment upload failed', ['One or more files could not be uploaded.']);
            }
          }
        });
      });
    };

    if (this.reportId == null) {
      this.initialReportService.createReport({ patientId: this.patientId, reportText }).subscribe({
        next: res => afterReportSaved(res.id),
        error: err => {
          this.saving.set(false);
          console.error('Report create failed', err);
          const message = err?.error?.message || err?.statusText || err?.message || 'Unable to create report.';
          this.snackbar.error('Save failed', [message]);
        }
      });
    } else {
      this.initialReportService.updateReportText(this.reportId, { reportText }).subscribe({
        next: () => afterReportSaved(this.reportId!),
        error: err => {
          this.saving.set(false);
          console.error('Report update failed', err);
          const message = err?.error?.message || err?.statusText || err?.message || 'Unable to update report.';
          this.snackbar.error('Save failed', [message]);
        }
      });
    }
  }

  saveDraft() {
    this.persistReport(() => this.snackbar.success('Draft saved'));
  }

  submitAndSend() {
    this.persistReport(() => this.snackbar.success('Submitted and sent'));
  }

  goToPatients(): void {
    this.router.navigate(['/app/patients']);
  }
}