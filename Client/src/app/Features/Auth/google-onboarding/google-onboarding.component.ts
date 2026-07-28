import {  Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-google-onboarding',
  imports: [ReactiveFormsModule, InputTextModule, ButtonModule],
  templateUrl: './google-onboarding.component.html',
  styleUrl: './google-onboarding.component.css',
})
export class GoogleOnboardingComponent {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  photoPreviewUrl = signal<string | null>(null);

  loading = signal(false);
  email = signal('');
  profilePhoto = signal<File | null>(null);
  private onboardingToken = '';

  form = this.fb.group({
    firstName:  ['', Validators.required],
    lastName:   ['', Validators.required],
    clinicName: ['', Validators.required],
  });

  constructor() {
    const state = history.state;
    if (!state?.onboardingToken) {
      this.router.navigateByUrl('/auth/login');
      return;
    }
    this.onboardingToken = state.onboardingToken;
    this.email.set(state.email ?? '');
    this.form.patchValue({
      firstName: state.suggestedFirstName ?? '',
      lastName: state.suggestedLastName ?? '',
    });
  }

  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    this.profilePhoto.set(file ?? null);

    if (file) {
      const reader = new FileReader();
      reader.onload = () => this.photoPreviewUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    } else {
      this.photoPreviewUrl.set(null);
    }
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control.touched);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const { firstName, lastName, clinicName } = this.form.getRawValue();

    this.auth.completeGoogleOnboarding({
      onboardingToken: this.onboardingToken,
      firstName: firstName!,
      lastName: lastName!,
      clinicName: clinicName!,
      profilePhoto: this.profilePhoto() ?? undefined,
    }).subscribe({
      next: () => this.router.navigateByUrl('/app/dashboard'),
      error: (err) => {
        console.error('Onboarding navigation failed:', err);
        this.loading.set(false);
      },
    });
  }
}