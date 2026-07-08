import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { Doctor } from '../schedule.models';


@Component({
  selector: 'app-doctor-selector',
  standalone: true,
  templateUrl: './doctor-selector.component.html',
  styleUrl: './doctor-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DoctorSelectorComponent {
  doctors = input.required<Doctor[]>();
  selectedDoctorId = input<string | null>(null);
  doctorSelected = output<string>();

  protected onSelect(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    if (id) this.doctorSelected.emit(id);
  }
}