import { Component, inject } from '@angular/core';
import { SnackbarService } from '../../../Core/Services/snackbar.service';

@Component({
  selector: 'app-toast-container',
  imports: [],
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.css',
})
export class ToastContainerComponent {
  snackbar = inject(SnackbarService)
}
