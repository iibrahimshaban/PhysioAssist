import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
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
  patientName = 'Ahmed Mohamed';
  patientBadge = 'Patient #PT-2841';
  chiefComplaint = 'Lower back pain radiating to right leg for 3 weeks.';
  injury = 'Lower back pain';
  dateOfInjury = 'for 3 weeks.';
  examination = '';
  initialReport = '';
  treatmentPlan = '';
  attachments: File[] = [];
  chatText = '';
  activeVoiceField: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan' | null = null;

  // chat state
  messages: Array<{ from: 'user' | 'assistant'; text: string; time?: string }> = [];
  sending = false;
  recognition: any = null;
  listening = false;
  liveTranscript = '';

  get recordingFieldLabel(): string {
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
  }

  constructor(
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

        this.liveTranscript = transcript;

        switch (this.activeVoiceField) {
          case 'chatText':
            this.chatText = transcript;
            break;
          case 'examination':
            this.examination = transcript;
            break;
          case 'initialReport':
            this.initialReport = transcript;
            break;
          case 'treatmentPlan':
            this.treatmentPlan = transcript;
            break;
          default:
            this.chatText = transcript;
        }
      };
      this.recognition.onend = () => {
        this.listening = false;
        this.activeVoiceField = null;
      };
      this.recognition.onerror = () => {
        this.listening = false;
        this.activeVoiceField = null;
      };
    }
  }

  ngOnInit(): void {
    this.loadInitialReport();
  }

  private loadInitialReport() {
    const patientId = 1;
    this.initialReportService.getInitialReport(patientId).subscribe({
      next: res => {
        this.patientName = res.patientName ?? this.patientName;
        this.patientBadge = res.patientBadge ?? this.patientBadge;
        this.chiefComplaint = res.chiefComplaint ?? this.chiefComplaint;
        this.injury = res.injury ?? this.injury;
        this.dateOfInjury = res.dateOfInjury ?? this.dateOfInjury;
        this.examination = res.examination ?? this.examination;
        this.initialReport = res.initialReport ?? this.initialReport;
        this.treatmentPlan = res.treatmentPlan ?? this.treatmentPlan;
      },
      error: err => {
        console.warn('Unable to load initial report from backend', err);
        this.snackbar.warning('Unable to load report', ['Could not load backend data.']);
      }
    });
  }

  addUserMessage(text: string) {
    this.messages.push({ from: 'user', text, time: new Date().toLocaleTimeString() });
  }

  addAssistantMessage(text: string) {
    this.messages.push({ from: 'assistant', text, time: new Date().toLocaleTimeString() });
  }

  startVoice(field: 'chatText' | 'examination' | 'initialReport' | 'treatmentPlan') {
    if (!this.recognition) {
      this.snackbar.warning('Speech recognition unavailable', ['Your browser does not support audio transcription.']);
      return;
    }
    if (this.listening) {
      this.recognition.stop();
      return;
    }
    this.liveTranscript = '';
    this.activeVoiceField = field;
    this.listening = true;
    this.recognition.start();
  }

  stopVoice() {
    if (!this.recognition) return;
    this.recognition.stop();
    this.listening = false;
    this.activeVoiceField = null;
  }

  sendToAi(text: string) {
    if (!text || this.sending) return;
    this.sending = true;
    const payload = { patientId: 1, text };
    this.initialReportService.sendChatMessage(payload).subscribe({
      next: res => {
        const reply = res?.reply || 'No response from AI.';
        this.addAssistantMessage(reply);
        this.sending = false;
      },
      error: err => {
        console.error('AI call failed', err);
        this.addAssistantMessage('AI service error.');
        this.sending = false;
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
    const combined = this.messages.map(m => `${m.from}: ${m.text}`).join('\n');
    this.sendToAi(`Compile draft from conversation:\n${combined}`);
  }

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
    this.addFiles(input.files);
    input.value = '';
  }

  addFiles(fileList: FileList) {
    for (let i = 0; i < fileList.length; i++) {
      const file = fileList.item(i);
      if (file) {
        this.attachments.push(file);
      }
    }
  }

  removeAttachment(index: number) {
    this.attachments.splice(index, 1);
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

  saveDraft() {
    const body = {
      examination: this.examination,
      initialReport: this.initialReport,
      treatmentPlan: this.treatmentPlan,
      chat: this.messages,
      attachments: this.attachments.map(file => file.name),
    };
    this.initialReportService.saveDraft(body).subscribe({
      next: () => this.snackbar.success('Draft saved'),
      error: err => {
        console.error('Draft save failed', err);
        const message = err?.error?.message || err?.statusText || err?.message || 'Unable to save report draft.';
        this.snackbar.error('Save failed', [message]);
      }
    });
  }

  submitAndSend() {
    const body = {
      examination: this.examination,
      initialReport: this.initialReport,
      treatmentPlan: this.treatmentPlan,
      chat: this.messages,
      attachments: this.attachments.map(file => file.name),
    };
    this.initialReportService.submit(body).subscribe({
      next: () => this.snackbar.success('Submitted and sent'),
      error: err => {
        console.error('Submit failed', err);
        const message = err?.error?.message || err?.statusText || err?.message || 'Unable to send report to backend.';
        this.snackbar.error('Submit failed', [message]);
      }
    });
  }
}
