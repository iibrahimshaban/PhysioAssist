import { Component, input, output, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { Attachment } from '../../../../Shared/Models/session-details-response';
import { SelectedAttachment } from '../../../../Shared/Models/selected-attachment';

@Component({
  selector: 'app-session-attachments',
  imports: [ButtonModule],
  templateUrl: './session-attachments.component.html',
  styleUrl: './session-attachments.component.css',
})
export class SessionAttachmentsComponent {
  attachments = input<Attachment[]>([]);
  selectedFiles = input.required<SelectedAttachment[]>();

  selectedFilesChange = output<SelectedAttachment[]>();
  deleteUploaded = output<string>();

  isOpen = signal(true);

  toggleAttachments() {
    this.isOpen.update((value) => !value);
  }

  onFilesSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (!inputElement.files || inputElement.files.length === 0) return;

    const newFiles: SelectedAttachment[] = Array.from(inputElement.files).map((file) => ({
      file,
      preview: URL.createObjectURL(file),
    }));

    this.selectedFilesChange.emit([...this.selectedFiles(), ...newFiles]);
    inputElement.value = '';
  }

  removeSelectedAttachment(index: number) {
    const currentFiles = this.selectedFiles();
    const selectedFile = currentFiles[index];
    if (selectedFile) URL.revokeObjectURL(selectedFile.preview);

    this.selectedFilesChange.emit(currentFiles.filter((_, i) => i !== index));
  }

  deleteAttachment(attachmentId: string) {
    this.deleteUploaded.emit(attachmentId);
  }
}