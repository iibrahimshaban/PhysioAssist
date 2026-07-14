import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SessionService } from '../../Core/Services/session.service';
import { SelectedAttachment } from '../../Shared/Models/selected-attachment';
import { SessionDetailsResponse } from '../../Shared/Models/session-details-response';

import { DictationGuideComponent } from './components/dictation-guide/dictation-guide.component';
import { RecordingModalComponent } from './components/recording-modal/recording-modal.component';
import { SessionActionsComponent } from './components/session-actions/session-actions.component';
import { SessionAttachmentsComponent } from './components/session-attachments/session-attachments.component';
import { SessionHeaderComponent } from './components/session-header/session-header.component';
import { SessionInfoComponent } from './components/session-info/session-info.component';
import { SessionNotesComponent } from './components/session-notes/session-notes.component';

@Component({
  selector: 'app-session',
  imports: [
    SessionHeaderComponent,
    SessionNotesComponent,
    SessionInfoComponent,
    SessionAttachmentsComponent,
    SessionActionsComponent,
    DictationGuideComponent,
    RecordingModalComponent,
  ],
  templateUrl: './session.component.html',
  styleUrl: './session.component.css',
})
export class SessionComponent implements OnInit {
  private sessionService = inject(SessionService);
  private route = inject(ActivatedRoute);
  sessionDetails = signal<SessionDetailsResponse | null>(null);
  notes = signal('');

  isSavingDraft = signal(false);
  isCompletingSession = signal(false);

  sessionInfoOpen = signal(true);
  dictationGuideOpen = signal(false);

  isRecordingModalOpen = signal(false);
  recordingSeconds = signal(0);
  isUploadingAudio = signal(false);

  selectedAttachmentFiles = signal<SelectedAttachment[]>([]);

  private recordingTimer?: ReturnType<typeof setInterval>;
  private mediaRecorder?: MediaRecorder;
  private audioChunks: Blob[] = [];
  private audioStream?: MediaStream;

  ngOnInit(): void {
    //const id = '940D33B2-901D-4F13-A983-AB72BD888091';
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      console.error('Session id was not found in the route');
      return;
    }

    this.loadSessionDetails(id);
  }
  private loadSessionDetails(id: string) {
    this.sessionService.getDetails(id).subscribe({
      next: (response) => {
        this.sessionDetails.set(response);
        this.notes.set(response.editedTranscript ?? '');
      },
      error: (error) => {
        console.error('Failed to load session details', error);
      },
    });
  }

  onNotesChanged(value: string) {
    this.notes.set(value);
  }

  toggleSessionInfo() {
    this.sessionInfoOpen.update((value) => !value);
  }

  toggleDictationGuide() {
    this.dictationGuideOpen.update((value) => !value);
  }

  async openRecordingModal() {
    if (!this.sessionDetails()) {
      return;
    }

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

    if (!currentSession || !this.mediaRecorder) {
      return;
    }

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
          error: (error) => {
            console.error(error);
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

  saveDraft() {
    const currentSession = this.sessionDetails();

    if (!currentSession) {
      return;
    }

    const files = this.selectedAttachmentFiles().map((attachment) => attachment.file);

    this.isSavingDraft.set(true);

    this.sessionService.saveDraft(currentSession.id, this.notes(), files).subscribe({
      next: () => {
        this.clearSelectedAttachments();

        this.sessionDetails.update((current) =>
          current
            ? {
                ...current,
                status: 1,
              }
            : current,
        );

        this.isSavingDraft.set(false);

        console.log('Draft saved successfully');
      },
      error: (error) => {
        console.error(error);
        this.isSavingDraft.set(false);
      },
    });
  }

  completeSession() {
    const currentSession = this.sessionDetails();

    if (!currentSession) {
      return;
    }

    const files = this.selectedAttachmentFiles().map((attachment) => attachment.file);

    this.isCompletingSession.set(true);

    this.sessionService.completeSession(currentSession.id, this.notes(), files).subscribe({
      next: () => {
        this.clearSelectedAttachments();

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
      error: (error) => {
        console.error(error);
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
                attachments: current.attachments.filter(
                  (attachment) => attachment.id !== attachmentId,
                ),
              }
            : current,
        );
      },
      error: (error) => {
        console.error(error);
      },
    });
  }

  private clearSelectedAttachments() {
    this.selectedAttachmentFiles().forEach((attachment) => {
      URL.revokeObjectURL(attachment.preview);
    });

    this.selectedAttachmentFiles.set([]);
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
