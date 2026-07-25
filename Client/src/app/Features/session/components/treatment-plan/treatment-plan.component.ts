import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-treatment-plan',
  imports: [],
  templateUrl: './treatment-plan.component.html',
  styleUrl: './treatment-plan.component.css',
})
export class TreatmentPlanComponent {
  isOpen = input.required<boolean>();
  plan = input.required<string>();

  toggle = output<void>();
  planChange = output<string>();

  onToggle() {
    this.toggle.emit();
  }

  onInput(value: string) {
    this.planChange.emit(value);
  }
}
