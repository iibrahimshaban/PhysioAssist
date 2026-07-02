import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, PasswordModule],
  templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  email = signal('');
  loading = signal(false);

  form = this.fb.group(
    {
      newPassword:     ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordMatchValidator }
  );

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.email.set(email);

    // Guard: must arrive from verify-otp with both email and stored OTP
    if (!email || !this.auth.getResetOtp()) {
      this.router.navigateByUrl('/auth/forgot-password');
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const otp = this.auth.getResetOtp();
    if (!otp) {
      this.router.navigateByUrl('/auth/forgot-password');
      return;
    }

    this.loading.set(true);

    const { newPassword } = this.form.getRawValue();

    this.auth.resetPassword({ email: this.email(), otp, newPassword: newPassword! })
      .subscribe({
        next: () => {
          this.auth.clearResetOtp(); // clean up sessionStorage
          this.router.navigateByUrl('/auth/login');
        },
        error: () => this.loading.set(false),
      });
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  isPasswordMismatch(): boolean {
    return !!(
      this.form.hasError('passwordMismatch') &&
      this.form.get('confirmPassword')?.touched
    );
  }

  getPasswordError(): string {
    const ctrl = this.form.get('newPassword');
    if (ctrl?.hasError('required'))  return 'Password is required.';
    if (ctrl?.hasError('minlength')) return 'At least 8 characters required.';
    return '';
  }
}

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('newPassword')?.value;
  const confirm  = group.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
}
