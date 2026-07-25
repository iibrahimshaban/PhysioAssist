import { Component, input, output, signal } from '@angular/core';
import { SessionDetailsResponse } from '../../../../Shared/Models/session-details-response';
import { SelectedAttachment } from '../../../../Shared/Models/selected-attachment';

@Component({
  selector: 'app-session-attachments',
  imports: [],
  templateUrl: './session-attachments.component.html',
  styleUrl: './session-attachments.component.css',
})
export class SessionAttachmentsComponent {
  session = input<SessionDetailsResponse | null>(null);

  selectedFiles = input.required<SelectedAttachment[]>();

  selectedFilesChange = output<SelectedAttachment[]>();
  deleteUploaded = output<string>();

  isOpen = signal(true);

  toggleAttachments() {
    this.isOpen.update((value) => !value);
  }

  onFilesSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;

    if (!inputElement.files || inputElement.files.length === 0) {
      return;
    }

    const newFiles: SelectedAttachment[] = Array.from(inputElement.files).map((file) => ({
      file,
      preview: URL.createObjectURL(file),
    }));

    const updatedFiles = [...this.selectedFiles(), ...newFiles];

    this.selectedFilesChange.emit(updatedFiles);

    inputElement.value = '';
  }

  removeSelectedAttachment(index: number) {
    const currentFiles = this.selectedFiles();
    const selectedFile = currentFiles[index];

    if (selectedFile) {
      URL.revokeObjectURL(selectedFile.preview);
    }

    const updatedFiles = currentFiles.filter((_, currentIndex) => currentIndex !== index);

    this.selectedFilesChange.emit(updatedFiles);
  }

  deleteAttachment(attachmentId: string) {
    this.deleteUploaded.emit(attachmentId);
  }
}
