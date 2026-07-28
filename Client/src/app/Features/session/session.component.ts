import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SessionService } from '../../Core/Services/session.service';
import { SelectedAttachment } from '../../Shared/Models/selected-attachment';
import { SessionDetailsResponse } from '../../Shared/Models/session-details-response';
import { RecordingModalComponent } from './components/recording-modal/recording-modal.component';
import { SessionActionsComponent } from './components/session-actions/session-actions.component';
import { SessionAttachmentsComponent } from './components/session-attachments/session-attachments.component';
import { SessionHeaderComponent } from './components/session-header/session-header.component';
import { SessionInfoComponent } from './components/session-info/session-info.component';
import { SessionNotesComponent } from './components/session-notes/session-notes.component';
import { TreatmentPlanComponent } from './components/treatment-plan/treatment-plan.component';
import { NextSessionBookingComponent } from './components/next-session-booking/next-session-booking.component';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import { catchError, concatMap, from, map, of, toArray } from 'rxjs';

@Component({
  selector: 'app-session',
  imports: [
    SessionHeaderComponent,
    SessionNotesComponent,
    SessionInfoComponent,
    SessionAttachmentsComponent,
    SessionActionsComponent,
    RecordingModalComponent,
    NextSessionBookingComponent,
    TreatmentPlanComponent
  ],
  templateUrl: './session.component.html',
  styleUrl: './session.component.css',
})
export class SessionComponent implements OnInit {
  private sessionService = inject(SessionService);
  private route = inject(ActivatedRoute);
  private snackbar = inject(SnackbarService);

  sessionDetails = signal<SessionDetailsResponse | null>(null);
  notes = signal('');
  notesOpen = signal(true); // NEW
  treatmentPlan = signal('');

  isSavingDraft = signal(false);
  isCompletingSession = signal(false);

  sessionInfoOpen = signal(true);
  treatmentPlanOpen = signal(true);

  isRecordingModalOpen = signal(false);

  selectedAttachmentFiles = signal<SelectedAttachment[]>([]);

  isRecording = signal(false);
  isPaused = signal(false);
  recordingSeconds = signal(0);
  isUploadingAudio = signal(false);

  private recordingTimer?: ReturnType<typeof setInterval>;
  private mediaRecorder?: MediaRecorder;
  private audioChunks: Blob[] = [];
  private audioStream?: MediaStream;
  private pendingAttachmentDeletions = signal<string[]>([]);

  visibleAttachments = computed(() => {
    const session = this.sessionDetails();
    if (!session) return [];
    const hidden = new Set(this.pendingAttachmentDeletions());
    return session.attachments.filter(a => !hidden.has(a.id));
  });

  stageAttachmentForDeletion(attachmentId: string) {
    this.pendingAttachmentDeletions.update(ids => [...ids, attachmentId]);
  }

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
        this.treatmentPlan.set(response.treatmentPlan ?? '');
      },
      error: (error) => {
        console.error('Failed to load session details', error);
      },
    });
  }

  onNotesChanged(value: string) {
    this.notes.set(value);
  }

  onTreatmentPlanChanged(value: string) {
    this.treatmentPlan.set(value);
  }

  toggleSessionInfo() {
    this.sessionInfoOpen.update((value) => !value);
  }

  toggleTreatmentPlan() {
    this.treatmentPlanOpen.update((value) => !value);
  }
    toggleNotes() { // NEW
    this.notesOpen.update((value) => !value);
  }

  async startRecording() {
    if (!this.sessionDetails()) return;

    try {
      this.audioChunks = [];
      this.audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.mediaRecorder = new MediaRecorder(this.audioStream);

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) this.audioChunks.push(event.data);
      };

      this.mediaRecorder.start();
      this.isRecording.set(true);
      this.isPaused.set(false);
      this.recordingSeconds.set(0);
      this.startTimer();
    } catch (error) {
      console.error('Microphone permission denied or unavailable', error);
      this.snackbar.warning('Microphone unavailable', ['Please allow microphone access to record.']);
    }
  }

pauseRecording() {
    if (!this.mediaRecorder || this.mediaRecorder.state !== 'recording') return;
    this.mediaRecorder.pause();
    this.isPaused.set(true);
    this.stopTimer(); // don't count paused time
  }

resumeRecording() {
    if (!this.mediaRecorder || this.mediaRecorder.state !== 'paused') return;
    this.mediaRecorder.resume();
    this.isPaused.set(false);
    this.startTimer();
  }

  stopRecording() {
    const currentSession = this.sessionDetails();
    if (!currentSession || !this.mediaRecorder) return;

    // Stop counting the instant Stop is pressed — uploading isn't more recording time.
    this.stopTimer();
    this.isRecording.set(false);
    this.isPaused.set(false);
    this.isUploadingAudio.set(true);

    this.mediaRecorder.onstop = () => {
      const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });

      this.sessionService
        .uploadAudioTranscription(currentSession.id, audioBlob, this.recordingSeconds())
        .subscribe({
          next: (transcript) => {
            // Append, don't overwrite — same reasoning as the initial-report fix:
            // multiple short recordings shouldn't destroy each other.
            this.notes.update(current => current?.trim() ? `${current.trim()}\n${transcript}` : transcript);
            this.isUploadingAudio.set(false);
          },
          error: (error) => {
            console.error(error);
            this.isUploadingAudio.set(false);
            this.snackbar.error('Transcription failed', ['Please try recording again.']);
          },
        });

      this.stopMicrophone();
    };

    this.mediaRecorder.stop();
  }
  cancelRecording() {
    this.stopTimer();
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.onstop = null; // suppress upload on cancel
      this.mediaRecorder.stop();
    }
    this.stopMicrophone();
    this.isRecording.set(false);
    this.isPaused.set(false);
  }

  saveDraft() {
  const currentSession = this.sessionDetails();
  if (!currentSession) return;

  this.isSavingDraft.set(true);

  this.flushPendingDeletions(currentSession.id, () => {
    const files = this.selectedAttachmentFiles().map((attachment) => attachment.file);

    this.sessionService
      .saveDraft(currentSession.id, this.notes(), files, this.treatmentPlan())
      .subscribe({
        next: () => {
          this.clearSelectedAttachments();
          this.pendingAttachmentDeletions.set([]);

          this.sessionDetails.update((current) =>
            current ? { ...current, status: 1, treatmentPlan: this.treatmentPlan() } : current,
          );

          this.isSavingDraft.set(false);
          this.snackbar.success('Draft saved');
        },
        error: (error) => {
          console.error(error);
          this.isSavingDraft.set(false);
          this.snackbar.error('Unable to save draft', ['Please try again.']);
        },
      });
  });
}

completeSession() {
  const currentSession = this.sessionDetails();
  if (!currentSession) return;

  this.isCompletingSession.set(true);

  this.flushPendingDeletions(currentSession.id, () => {
    const files = this.selectedAttachmentFiles().map((attachment) => attachment.file);

    this.sessionService
      .completeSession(currentSession.id, this.notes(), files, this.treatmentPlan())
      .subscribe({
        next: () => {
          this.clearSelectedAttachments();
          this.pendingAttachmentDeletions.set([]);

          this.sessionDetails.update((current) =>
            current ? { ...current, status: 2, treatmentPlan: this.treatmentPlan() } : current,
          );

          this.isCompletingSession.set(false);
          this.snackbar.success('Session completed', ['Notes, treatment plan, and attachments saved.']);
        },
        error: (error) => {
          console.error(error);
          this.isCompletingSession.set(false);
          this.snackbar.error('Unable to complete session', ['Please try again.']);
        },
      });
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

  private startTimer() {
    this.recordingTimer = setInterval(() => this.recordingSeconds.update(v => v + 1), 1000);
  }

  private stopTimer() {
    if (this.recordingTimer) {
      clearInterval(this.recordingTimer);
      this.recordingTimer = undefined;
    }
  }

  private stopMicrophone() {
    this.audioStream?.getTracks().forEach(track => track.stop());
    this.audioStream = undefined;
  }

  private flushPendingDeletions(sessionId: string, onDone: () => void) {
  const ids = this.pendingAttachmentDeletions();
  if (ids.length === 0) {
    onDone();
    return;
  }

  from(ids)
    .pipe(
      concatMap(id =>
        this.sessionService.deleteAttachment(id).pipe(
          map(() => ({ id, error: null as any })),
          catchError(error => of({ id, error })),
        ),
      ),
      toArray(),
    )
    .subscribe(results => {
      const failed = results.filter(r => r.error);
      if (failed.length > 0) {
        this.isSavingDraft.set(false);
        this.isCompletingSession.set(false);
        this.snackbar.error('Some attachments failed to remove', [
          `${failed.length} file(s) could not be deleted — try again.`,
        ]);
        return; // don't proceed to save/complete until deletions succeed
      }

      // Remove the now-actually-deleted attachments from local state
      // so a later save doesn't try to delete them again.
      this.sessionDetails.update(current =>
        current
          ? { ...current, attachments: current.attachments.filter(a => !ids.includes(a.id)) }
          : current,
      );

      onDone();
    });
}
}
