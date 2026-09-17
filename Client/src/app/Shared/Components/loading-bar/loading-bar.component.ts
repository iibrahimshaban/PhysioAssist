import { Component, inject } from '@angular/core';
import { BusyService } from '../../../Core/Services/busy.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-loading-bar',
  imports: [TranslatePipe],
  templateUrl: './loading-bar.component.html',
  styleUrl: './loading-bar.component.css',
})
export class LoadingBarComponent {
  busyService = inject(BusyService);
}
