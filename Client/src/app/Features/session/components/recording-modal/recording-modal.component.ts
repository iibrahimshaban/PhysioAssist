import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-recording-modal',
  imports: [TranslatePipe],
  templateUrl: './recording-modal.component.html',
  styleUrl: './recording-modal.component.css',
})
export class RecordingModalComponent {
  seconds = input.required<number>();
  isUploading = input(false);

  stop = output<void>();
  cancel = output<void>();

  formatRecordingTime(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;

    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  }
}
