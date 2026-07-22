import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-dictation-guide',
  imports: [],
  templateUrl: './dictation-guide.component.html',
  styleUrl: './dictation-guide.component.css',
})
export class DictationGuideComponent {
  isOpen = input.required<boolean>();

  toggle = output<void>();

  onToggle() {
    this.toggle.emit();
  }
}
