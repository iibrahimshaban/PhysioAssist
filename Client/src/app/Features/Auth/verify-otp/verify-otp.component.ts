import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputOtpModule } from 'primeng/inputotp';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputOtpModule],
  templateUrl: './verify-otp.component.html',
})
export class VerifyOtpComponent implements OnInit {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  email = signal('');
  loading = signal(false);
  resending = signal(false);

  form = this.fb.group({
    otp: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.email.set(email);

    if (!email) this.router.navigateByUrl('/auth/forgot-password');
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    const otp = this.form.getRawValue().otp!;

    this.auth.verifyResetOtp({ email: this.email(), otp }).subscribe({
      next: () => {
        this.auth.saveResetOtp(otp); 
        this.router.navigate(['/auth/reset-password'], {
          queryParams: { email: this.email() },
        });
      },
      error: () => this.loading.set(false),
    });
  }

  onResend(): void {
    this.resending.set(true);
    this.auth.forgetPassword({ email: this.email() }).subscribe({
      next: () => this.resending.set(false),
      error: () => this.resending.set(false),
    });
  }
}