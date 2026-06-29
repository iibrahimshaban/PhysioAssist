import { Injectable, signal } from '@angular/core';
import { Toast, ToastType } from '../../Shared/Models/toast';

@Injectable({
  providedIn: 'root',
})
export class SnackbarService {
  toasts = signal<Toast[]>([]);
  private counter = 0;

  private show(title: string, messages: string[], type: ToastType) {
    const id = this.counter++;
    this.toasts.update(t => [...t, { id, title, messages, type }]);
    setTimeout(() => this.remove(id), 4000);
  }

  remove(id: number) {
    this.toasts.update(t => t.filter(toast => toast.id !== id));
  }

  success(title: string, messages: string[] = []) { this.show(title, messages, 'success'); }
  error(title: string, messages: string[] = [])   { this.show(title, messages, 'error'); }
  info(title: string, messages: string[] = [])    { this.show(title, messages, 'info'); }
  warning(title: string, messages: string[] = []) { this.show(title, messages, 'warning'); }
}
