import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { InitialReportService, InitialReportResponse } from '../../Core/Services/initial-report.service';
import { PatientService } from '../Patient/services/patient.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';

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

  patientName = signal('');
  patientBadge = signal('');
  age = signal<number | null>(null);
  gender = signal('');
  chiefComplaint = signal('');
  injury = signal<string | undefined>(undefined);
  injuryDate = signal<string | undefined>(undefined);
  patientCategory = signal<string | undefined>(undefined);

  examination = signal('');
  initialReport = signal('');
  treatmentPlan = signal('');
  attachments = signal<File[]>([]);

  chatText = ''; 
  activeVoiceField: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan' | null = null;

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
      default:
        return 'voice note';
    }
  });

  patientInitials = computed(() =>
    this.patientName()
      .split(' ')
      .filter(Boolean)
      .map(n => n[0])
      .join('')
  );

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService,
    private readonly patientService: PatientService
  ) {}

  ngOnInit(): void {
    const navState = history.state as { patient?: any } | undefined;
    if (navState?.patient) {
      this.applyPatientSummary(navState.patient);
    }

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
      next: (intake: any) => {
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
      error: (err: any) => {
        if (err?.status !== 404) {
          console.warn('Unable to load intake data', err);
          this.snackbar.warning('Unable to load intake details', ['Could not load patient intake data.']);
        }
      }
    });
  }

  private loadExistingReport(patientId: string): void {
    this.initialReportService.getReportByPatientId(patientId).subscribe({
      next: (res: InitialReportResponse) => {
        this.reportId = res.id;
        this.initialReport.set(res.reportText ?? '');
      },
      error: (err: any) => {
        if (err?.status !== 404) {
          console.warn('Unable to load existing report', err);
          this.snackbar.warning('Unable to load report', ['Could not load an existing report.']);
        }
      }
    });
  }

  private getValidAttachmentFiles(fileList: FileList): File[] {
    return Array.from(fileList).filter(file => {
      const isImage = file.type.startsWith('image/');
      const isPdf = file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf');
      return isImage || isPdf;
    });
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.uploadFiles(input.files);
    input.value = '';
  }

  addFiles(fileList: FileList) {
    const validFiles = this.getValidAttachmentFiles(fileList);
    if (validFiles.length === 0) {
      this.snackbar.error('Invalid attachment', ['Only images and PDFs are allowed.']);
      return;
    }
    this.attachments.update(files => [...files, ...validFiles]);
  }

  private uploadFiles(fileList: FileList) {
    this.addFiles(fileList);
  }

  removeAttachment(index: number) {
    this.attachments.update(files => files.filter((_, i) => i !== index));
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      this.uploadFiles(event.dataTransfer.files);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

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
      const attachmentsToUpload = currentAttachments.filter(file => file.type.startsWith('image/') || file.name.toLowerCase().endsWith('.pdf'));
      const skipped = currentAttachments.length - attachmentsToUpload.length;
      if (skipped > 0) {
        this.snackbar.warning('Some attachments skipped', [`${skipped} file(s) are not images or PDFs and were not uploaded.`]);
      }

      if (attachmentsToUpload.length === 0) {
        this.saving.set(false);
        onSuccess();
        return;
      }

      let remaining = attachmentsToUpload.length;
      let hadError = false;
      attachmentsToUpload.forEach(file => {
        this.initialReportService.uploadAttachment(reportId, file).subscribe({
          next: () => {
            remaining--;
            if (remaining === 0) {
              this.saving.set(false);
              this.attachments.set([]);
              if (!hadError) onSuccess();
            }
          },
          error: (err: any) => {
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
        error: (err: any) => {
          this.saving.set(false);
          console.error('Report create failed', err);
          const message = err?.error?.message || err?.statusText || err?.message || 'Unable to create report.';
          this.snackbar.error('Save failed', [message]);
        }
      });
    } else {
      this.initialReportService.updateReportText(this.reportId, { reportText }).subscribe({
        next: () => afterReportSaved(this.reportId!),
        error: (err: any) => {
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

  goToPatients() {
    this.router.navigate(['app/patients']);
  }

  startVoice(field: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan') {
    if (!this.recognition) {
      this.snackbar.warning('Microphone unavailable', ['Your browser does not support voice recording.']);
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

  addUserMessage(text: string) {
    this.messages.update(msgs => [...msgs, { from: 'user', text, time: new Date().toLocaleTimeString() }]);
  }

  addAssistantMessage(text: string) {
    this.messages.update(msgs => [...msgs, { from: 'assistant', text, time: new Date().toLocaleTimeString() }]);
  }

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
      error: (err: any) => {
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
}
