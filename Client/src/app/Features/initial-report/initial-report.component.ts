import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { InitialReportService, InitialReportResponse, ReportAttachmentResponse } from '../../Core/Services/initial-report.service';
import { PatientService } from '../Patient/services/patient.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';

interface AttachmentEntry {
  id?: string;
  name: string;
  size: number;
  fileUrl?: string;
  fileType?: string;
}

@Component({
  selector: 'app-initial-report',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  templateUrl: './initial-report.component.html',
  styleUrls: ['./initial-report.component.css'],
})
export class InitialReportComponent implements OnInit {
  patientName = 'Ahmed Mohamed';
  patientBadge = 'Patient #PT-2841';
  chiefComplaint = 'Lower back pain radiating to right leg for 3 weeks.';
  injury = 'Lower back pain';
  dateOfInjury = 'for 3 weeks.';
  examination = '';
  initialReport = '';
  treatmentPlan = '';
  attachments: AttachmentEntry[] = [];
  activeVoiceField: 'examination' | 'initialReport' | 'treatmentPlan' | null = null;

  private readonly REPORT_ID_KEY = 'initialReportId';
  private readonly PATIENT_ID_KEY = 'patientId';

  reportId = '';
  patientId = '';
  treatmentPlanPdfUrl = '';
  reportReady = false;
  listening = false;
  liveTranscript = '';
  private mediaRecorder: MediaRecorder | null = null;
  private recordedChunks: BlobPart[] = [];

  get recordingFieldLabel(): string {
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
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly initialReportService: InitialReportService,
    private readonly snackbar: SnackbarService,
    private readonly patientService: PatientService
  ) {}

  ngOnInit(): void {
    this.initializeReport();
  }

  private initializeReport() {
    const routePatientId = this.route.snapshot.paramMap.get('patientId') ?? '';
    const queryPatientId = this.route.snapshot.queryParamMap.get('patientId') ?? '';
    const storedPatientId = localStorage.getItem(this.PATIENT_ID_KEY) ?? '';

    const patientId = routePatientId || queryPatientId || storedPatientId;
    if (!patientId) {
      this.reportReady = false;
      this.snackbar.error('Missing patient ID', [
        'No patientId found in route or query params. Use /initial-report/:patientId'
      ]);
      return;
    }

    this.patientId = patientId;
    localStorage.setItem(this.PATIENT_ID_KEY, this.patientId);
    this.createReport(this.patientId);
  }

  private getRouteParam(keys: string[]) {
    for (const key of keys) {
      const value = this.route.snapshot.queryParamMap.get(key);
      if (value) {
        return value;
      }
    }
    return '';
  }

  private loadReport() {
    if (!this.reportId) {
      this.snackbar.warning('Missing report ID', ['Cannot load initial report without a report ID.']);
      return;
    }

    this.initialReportService.getReport(this.reportId).subscribe({
      next: res => this.applyReportResponse(res),
      error: err => {
        console.error('Unable to load initial report from backend', err);
        this.snackbar.warning('Unable to load report', [this.getApiErrorDetail(err) || 'Could not load backend data.']);
        if (err?.status === 404) {
          localStorage.removeItem(this.REPORT_ID_KEY);
          this.reportId = '';
          this.reportReady = false;
        }
      }
    });
  }

  private createReport(patientId: string) {
    this.initialReportService.createReport(patientId).subscribe({
      next: res => this.applyReportResponse(res),
      error: err => {
        console.error('Unable to create report', err);
        this.reportReady = false;
        this.snackbar.error('Report creation failed', [
          this.getApiErrorDetail(err) || 'Unable to create initial report for this patient.'
        ]);
      }
    });
  }

  private applyReportResponse(response: InitialReportResponse) {
    this.reportId = response.id;
    this.treatmentPlanPdfUrl = response.treatmentPlanPdfUrl;
    localStorage.setItem(this.REPORT_ID_KEY, this.reportId);
    this.reportReady = true;

    this.parseReportText(response.reportText);
    this.attachments = response.attachments?.map(a => ({
      id: a.id,
      name: a.fileName,
      size: 0,
      fileUrl: a.fileUrl,
      fileType: a.fileType,
    })) ?? [];

    if (!this.patientId && response.patientId) {
      this.patientId = response.patientId;
      localStorage.setItem(this.PATIENT_ID_KEY, this.patientId);
    }

    // fetch patient details if possible to populate UI
    if (this.patientId) {
      this.fetchPatientDetails(this.patientId);
    }
  }

  private fetchPatientDetails(patientId: string) {
    this.patientService.getById(patientId as any).subscribe({
      next: (p: any) => {
        // map likely fields without changing existing UI structure
        const fullName = p.fullName || ((p.firstName || '') + (p.lastName ? ' ' + p.lastName : '')) || p.name;
        if (fullName) this.patientName = fullName;

        const badge = p.patientNumber || p.code || p.id;
        if (badge) this.patientBadge = typeof badge === 'string' ? `Patient #${badge}` : `Patient #${badge}`;

        if (p.chiefComplaint) this.chiefComplaint = p.chiefComplaint;
        if (p.injury) this.injury = p.injury;
        if (p.dateOfInjury) this.dateOfInjury = p.dateOfInjury;
      },
      error: err => {
        console.warn('Unable to load patient details', err);
      }
    });
  }

  private parseReportText(reportText: string) {
    const examinationMatch = reportText.match(/=== Examination ===([\s\S]*?)(?=== Diagnosis ===|$)/);
    const diagnosisMatch = reportText.match(/=== Diagnosis ===([\s\S]*?)(?=== Treatment Plan ===|$)/);
    const treatmentMatch = reportText.match(/=== Treatment Plan ===([\s\S]*?)$/);

    this.examination = examinationMatch?.[1].trim() ?? '';
    this.initialReport = diagnosisMatch?.[1].trim() ?? '';
    this.treatmentPlan = treatmentMatch?.[1].trim() ?? '';
  }

  private getApiErrorDetail(err: any) {
    return err?.error?.detail || err?.error?.message || err?.statusText || err?.message || '';
  }

  startVoice(field: 'examination' | 'initialReport' | 'treatmentPlan') {
    if (!this.reportReady) {
      this.snackbar.warning('Report not ready', ['Please wait until the report is loaded or created before recording audio.']);
      return;
    }

    if (this.listening) {
      this.stopVoice();
      return;
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      this.snackbar.warning('Microphone unavailable', ['Your browser does not support audio recording.']);
      return;
    }

    this.activeVoiceField = field;
    this.liveTranscript = '';
    this.recordedChunks = [];
    this.listening = true;

    navigator.mediaDevices.getUserMedia({ audio: true }).then(stream => {
      this.mediaRecorder = new MediaRecorder(stream);
      this.mediaRecorder.ondataavailable = (event: BlobEvent) => {
        if (event.data && event.data.size > 0) {
          this.recordedChunks.push(event.data);
        }
      };
      this.mediaRecorder.onstop = () => {
        this.listening = false;
        stream.getTracks().forEach(track => track.stop());
        const audioBlob = new Blob(this.recordedChunks, { type: 'audio/webm' });
        this.transcribeVoice(audioBlob, field);
      };
      this.mediaRecorder.start();
    }).catch(err => {
      this.listening = false;
      this.activeVoiceField = null;
      console.error('Microphone access denied', err);
      this.snackbar.warning('Microphone access denied', [this.getApiErrorDetail(err) || 'Cannot start recording.']);
    });
  }

  stopVoice() {
    if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
      this.listening = false;
      this.activeVoiceField = null;
      return;
    }

    this.mediaRecorder.stop();
    this.activeVoiceField = null;
  }

  private transcribeVoice(audioBlob: Blob, field: 'examination' | 'initialReport' | 'treatmentPlan') {
    if (!this.reportId) {
      this.snackbar.error('Cannot transcribe audio', ['Report ID is missing.']);
      return;
    }

    this.initialReportService.transcribeAudio(this.reportId, audioBlob).subscribe({
      next: res => {
        const trimmedText = res.reportText?.trim() ?? '';
        if (!trimmedText) {
          this.snackbar.warning('No transcription result', ['The audio did not return any text.']);
          return;
        }

        this.liveTranscript = trimmedText;
        if (field === 'examination') {
          this.examination = trimmedText;
        } else if (field === 'initialReport') {
          this.initialReport = trimmedText;
        } else {
          this.treatmentPlan = trimmedText;
        }
      },
      error: err => {
        console.error('Audio transcription failed', err);
        this.snackbar.error('Transcription failed', [this.getApiErrorDetail(err) || 'Unable to transcribe audio.']);
      }
    });
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.uploadFiles(input.files);
    input.value = '';
  }

  addFiles(fileList: FileList) {
    this.uploadFiles(fileList);
  }

  private uploadFiles(fileList: FileList) {
    Array.from(fileList).forEach(file => this.uploadAttachmentFile(file));
  }

  private uploadAttachmentFile(file: File) {
    if (!this.reportId) {
      this.snackbar.warning('Cannot upload attachment', ['Report ID is missing.']);
      return;
    }

    const isImage = file.type.startsWith('image/');
    const isPdf = file.type === 'application/pdf';
    if (!isImage && !isPdf) {
      this.snackbar.error('Invalid attachment', ['Only images and PDFs are allowed.']);
      return;
    }

    this.initialReportService.uploadAttachment(this.reportId, file).subscribe({
      next: res => {
        this.attachments.push({
          id: res.id,
          name: res.fileName,
          size: file.size,
          fileUrl: res.fileUrl,
          fileType: res.fileType,
        });
        this.snackbar.success('Attachment uploaded', [res.fileName]);
      },
      error: err => {
        console.error('Attachment upload failed', err);
        const detail = this.getApiErrorDetail(err);
        const message = err?.status === 400 ? 'Only images and PDFs are allowed.' : detail || 'Unable to upload attachment.';
        this.snackbar.error('Upload failed', [message]);
      }
    });
  }

  removeAttachment(index: number) {
    const attachment = this.attachments[index];
    if (!attachment) {
      return;
    }

    if (attachment.id && this.reportId) {
      this.initialReportService.deleteAttachment(this.reportId, attachment.id).subscribe({
        next: () => {
          this.attachments.splice(index, 1);
          this.snackbar.success('Attachment removed');
        },
        error: err => {
          console.error('Attachment delete failed', err);
          this.snackbar.error('Delete failed', [this.getApiErrorDetail(err) || 'Unable to remove attachment.']);
        }
      });
      return;
    }

    this.attachments.splice(index, 1);
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

  saveDraft() {
    if (!this.reportId) {
      this.snackbar.error('Unable to save draft', ['Report ID is missing.']);
      return;
    }

    const reportText = `=== Examination ===${this.examination}=== Diagnosis ===${this.initialReport}=== Treatment Plan ===${this.treatmentPlan}`;
    this.initialReportService.updateReportText(this.reportId, reportText).subscribe({
      next: res => {
        this.applyReportResponse(res);
        this.snackbar.success('Draft saved', ['Report text was updated successfully.']);
      },
      error: err => {
        console.error('Draft save failed', err);
        this.snackbar.error('Save failed', [this.getApiErrorDetail(err) || 'Unable to save report draft.']);
      }
    });
  }

  submitAndSend() {
    if (!this.reportId) {
      this.snackbar.error('Unable to submit report', ['Report ID is missing.']);
      return;
    }

    this.initialReportService.submitReport(this.reportId).subscribe({
      next: res => {
        this.applyReportResponse(res);
        const linkMessage = res.treatmentPlanPdfUrl ? `PDF: ${res.treatmentPlanPdfUrl}` : 'Report submitted successfully.';
        this.snackbar.success('Submitted and sent', [linkMessage]);
      },
      error: err => {
        console.error('Submit failed', err);
        this.snackbar.error('Submit failed', [this.getApiErrorDetail(err) || 'Unable to send report to backend.']);
      }
    });
  }
}
