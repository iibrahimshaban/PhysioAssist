import { Component, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';

@Component({
  selector: 'app-patient-free-time-editor',
  imports: [FormsModule, ButtonModule, CheckboxModule],
  templateUrl: './patient-free-time-editor.component.html',
  styleUrl: './patient-free-time-editor.component.css',
})
export class PatientFreeTimeEditorComponent {
  freeTimeText = model<string>('');
  persistOverride = model<boolean>(false);

  refresh = output<{ text: string; persist: boolean }>();
}
