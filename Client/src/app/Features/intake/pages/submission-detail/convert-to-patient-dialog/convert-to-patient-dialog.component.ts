import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';

@Component({
  selector: 'app-convert-to-patient-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule],
  templateUrl: './convert-to-patient-dialog.component.html',
  styleUrl: './convert-to-patient-dialog.component.css'
})
export class ConvertToPatientDialogComponent {
  @Input({ required: true }) visible = false;
  @Input() patientName?: string;
  @Input() patientEmail?: string;
  @Input() patientPhone?: string;
  @Input({ required: true }) updating = false;
  @Input() missingFields: string[] = [];

  @Output() cancelDialog = new EventEmitter<void>();
  @Output() confirmDialog = new EventEmitter<void>();

  get isValid(): boolean {
    return this.missingFields.length === 0;
  }
}