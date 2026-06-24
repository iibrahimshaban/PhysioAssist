import { Component, inject } from '@angular/core';
import { BusyService } from '../../../Core/Services/busy.service';

@Component({
  selector: 'app-loading-bar',
  imports: [],
  templateUrl: './loading-bar.component.html',
  styleUrl: './loading-bar.component.css',
})
export class LoadingBarComponent {
  busyService = inject(BusyService)
}
