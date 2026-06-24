import { Injectable, signal } from '@angular/core';
import { Toast, ToastType } from '../../Shared/Models/toast';

@Injectable({
  providedIn: 'root',
})
export class SnackbarService {
  toasts = signal<Toast[]>([]);
  private counter = 0;

  private show(message: string, type: ToastType) {
    const id = this.counter++;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.remove(id), 3000);
  }

  remove(id: number) {
    this.toasts.update(t => t.filter(toast => toast.id !== id));
  }

  success(message: string) { this.show(message, 'success'); }
  error(message: string) { this.show(message, 'error'); }
  info(message: string) { this.show(message, 'info'); }
  warning(message: string) { this.show(message, 'warning'); }
}
