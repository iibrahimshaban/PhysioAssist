import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { SessionService } from '../../Core/Services/session.service';
import { SessionDetailsResponse } from '../../Shared/Models/session-details-response';
import { RecordingModalComponent } from './components/recording-modal/recording-modal.component';

type SelectedAttachment = {
  file: File;
  preview: string;
};

@Component({
  selector: 'app-session',
  imports: [DatePipe, RecordingModalComponent],
  templateUrl: './session.component.html',
  styleUrl: './session.component.css',
})
export class SessionComponent implements OnInit {
  private sessionService = inject(SessionService);

  sessionDetails = signal<SessionDetailsResponse | null>(null);
  notes = signal('');
  isSavingDraft = signal(false);
  sessionInfoOpen = signal(true);
  attachmentsOpen = signal(true);
  dictationGuideOpen = signal(false);

  isRecordingModalOpen = signal(false);
  recordingSeconds = signal(0);
  isUploadingAudio = signal(false);

  selectedAttachmentFiles = signal<SelectedAttachment[]>([]);
  isCompletingSession = signal(false);

  private recordingTimer?: ReturnType<typeof setInterval>;
  private mediaRecorder?: MediaRecorder;
  private audioChunks: Blob[] = [];
  private audioStream?: MediaStream;

  ngOnInit(): void {
    const id = '940D33B2-901D-4F13-A983-AB72BD888091';

    this.sessionService.getDetails(id).subscribe({
      next: (res) => {
        this.sessionDetails.set(res);
        this.notes.set(res.editedTranscript ?? '');
        console.log(res);
      },
      error: (err) => console.log(err),
    });
  }

  toggleDictationGuide() {
    this.dictationGuideOpen.update((value) => !value);
  }

  toggleSessionInfo() {
    this.sessionInfoOpen.update((value) => !value);
  }

  toggleAttachments() {
    this.attachmentsOpen.update((value) => !value);
  }

  getStatusText(status?: number): string {
    switch (status) {
      case 0:
        return 'scheduled';
      case 1:
        return 'in-progress';
      case 2:
        return 'completed';
      case 3:
        return 'cancelled';
      default:
        return '';
    }
  }

  async openRecordingModal() {
    const currentSession = this.sessionDetails();

    if (!currentSession) return;

    try {
      this.audioChunks = [];

      this.audioStream = await navigator.mediaDevices.getUserMedia({
        audio: true,
      });

      this.mediaRecorder = new MediaRecorder(this.audioStream);

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.start();

      this.isRecordingModalOpen.set(true);
      this.recordingSeconds.set(0);

      this.recordingTimer = setInterval(() => {
        this.recordingSeconds.update((value) => value + 1);
      }, 1000);
    } catch (error) {
      console.error('Microphone permission denied or unavailable', error);
    }
  }

  stopRecording() {
    const currentSession = this.sessionDetails();

    if (!currentSession || !this.mediaRecorder) return;

    this.isUploadingAudio.set(true);

    this.mediaRecorder.onstop = () => {
      const audioBlob = new Blob(this.audioChunks, {
        type: 'audio/webm',
      });

      this.sessionService
        .uploadAudioTranscription(currentSession.id, audioBlob, this.recordingSeconds())
        .subscribe({
          next: (transcript) => {
            this.notes.set(transcript);
            this.isUploadingAudio.set(false);
            this.closeRecordingModal();
          },
          error: (err) => {
            console.error(err);
            this.isUploadingAudio.set(false);
            this.closeRecordingModal();
          },
        });

      this.stopMicrophone();
    };

    this.mediaRecorder.stop();
  }

  cancelRecording() {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
    }

    this.stopMicrophone();
    this.closeRecordingModal();
  }

  onAttachmentFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) return;

    const files: SelectedAttachment[] = Array.from(input.files).map((file) => ({
      file,
      preview: URL.createObjectURL(file),
    }));

    this.selectedAttachmentFiles.update((current) => [...current, ...files]);

    input.value = '';
  }

  removeSelectedAttachment(index: number) {
    this.selectedAttachmentFiles.update((files) => {
      URL.revokeObjectURL(files[index].preview);
      return files.filter((_, i) => i !== index);
    });
  }

  completeSession() {
    const currentSession = this.sessionDetails();

    if (!currentSession) return;

    const files = this.selectedAttachmentFiles().map((x) => x.file);

    this.isCompletingSession.set(true);

    this.sessionService.completeSession(currentSession.id, this.notes(), files).subscribe({
      next: () => {
        this.selectedAttachmentFiles().forEach((x) => URL.revokeObjectURL(x.preview));
        this.selectedAttachmentFiles.set([]);

        this.sessionDetails.update((current) =>
          current
            ? {
                ...current,
                status: 2,
              }
            : current,
        );

        this.isCompletingSession.set(false);
        console.log('Session completed successfully');
      },
      error: (err) => {
        console.error(err);
        this.isCompletingSession.set(false);
      },
    });
  }

  deleteAttachment(attachmentId: string) {
    this.sessionService.deleteAttachment(attachmentId).subscribe({
      next: () => {
        this.sessionDetails.update((current) =>
          current
            ? {
                ...current,
                attachments: current.attachments.filter((x) => x.id !== attachmentId),
              }
            : current,
        );
      },
      error: (err) => console.error(err),
    });
  }
  saveDraft() {
    const currentSession = this.sessionDetails();

    if (!currentSession) return;

    const files = this.selectedAttachmentFiles().map((x) => x.file);

    this.isSavingDraft.set(true);

    this.sessionService.saveDraft(currentSession.id, this.notes(), files).subscribe({
      next: () => {
        this.selectedAttachmentFiles().forEach((x) => URL.revokeObjectURL(x.preview));

        this.selectedAttachmentFiles.set([]);

        this.sessionDetails.update((current) =>
          current
            ? {
                ...current,
                status: 1, // InProgress
              }
            : current,
        );

        this.isSavingDraft.set(false);

        console.log('Draft saved successfully');
      },
      error: (err) => {
        console.error(err);
        this.isSavingDraft.set(false);
      },
    });
  }

  private closeRecordingModal() {
    this.isRecordingModalOpen.set(false);

    if (this.recordingTimer) {
      clearInterval(this.recordingTimer);
      this.recordingTimer = undefined;
    }
  }

  private stopMicrophone() {
    this.audioStream?.getTracks().forEach((track) => track.stop());
    this.audioStream = undefined;
  }
}
