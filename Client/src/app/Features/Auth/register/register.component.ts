import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CheckboxModule,
  ],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

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
      userName,
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
    if (ctrl.hasError('required'))     return `${this.fieldLabel()[field]} is required.`;
    if (ctrl.hasError('email'))        return 'Enter a valid email address.';
    if (ctrl.hasError('minlength'))    return `At least ${ctrl.errors?.['minlength'].requiredLength} characters required.`;
    if (ctrl.hasError('requiredTrue')) return 'You must agree to the Terms and Privacy Policy.';
    return '';
  }

  private fieldLabel(): Record<string, string> {
    return {
      firstName:  'First name',
      lastName:   'Last name',
      clinicName: 'Clinic name',
      email:      'Email',
      password:   'Password',
      agreed:     'Terms',
    };
  }
}