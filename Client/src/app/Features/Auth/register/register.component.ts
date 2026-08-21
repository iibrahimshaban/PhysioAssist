import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslocoModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CheckboxModule,
  ],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly fb        = inject(FormBuilder);
  private readonly auth      = inject(AuthService);
  private readonly router    = inject(Router);
  private readonly transloco = inject(TranslocoService);

  loading = signal(false);
  photoPreview = signal<string | null>(null);
  photoFile = signal<File | null>(null);

  form = this.fb.group({
    firstName:  ['', [Validators.required, Validators.minLength(2)]],
    lastName:   ['', [Validators.required, Validators.minLength(2)]],
    clinicName: ['', Validators.required],
    email:      ['', [Validators.required, Validators.email]],
    password:   ['', [Validators.required, Validators.minLength(8)]],
    agreed:     [false, Validators.requiredTrue],
  });

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.photoFile.set(file);

    const reader = new FileReader();
    reader.onload = () => this.photoPreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.photoFile.set(null);
    this.photoPreview.set(null);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    const { firstName, lastName, clinicName, email, password } = this.form.getRawValue();

    const userName = email!.split('@')[0];

    this.auth.register({
      firstName: firstName!,
      lastName: lastName!,
      clinicName: clinicName!,
      email: email!,
      password: password!,
      profilePhoto: this.photoFile() ?? undefined,
    }).subscribe({
      next: () => this.router.navigate(['/auth/confirm-email'], { queryParams: { email } }),
      error: () => this.loading.set(false),
    });
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control.touched);
  }

  getFieldError(field: string): string {
    const ctrl = this.form.get(field);
    if (!ctrl) return '';
    if (ctrl.hasError('required'))     return this.transloco.translate('auth.validation.required', { field: this.transloco.translate('auth.fields.' + field) });
    if (ctrl.hasError('email'))        return this.transloco.translate('auth.validation.email');
    if (ctrl.hasError('minlength'))    return this.transloco.translate('auth.validation.minLength', { n: ctrl.errors?.['minlength'].requiredLength });
    if (ctrl.hasError('requiredTrue')) return this.transloco.translate('auth.validation.requiredTrue');
    return '';
  }
}