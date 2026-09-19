import { Component, inject } from '@angular/core';
import { SnackbarService } from '../../../Core/Services/snackbar.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-toast-container',
  imports: [TranslatePipe],
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.css',
})
export class ToastContainerComponent {
  snackbar = inject(SnackbarService);
}
