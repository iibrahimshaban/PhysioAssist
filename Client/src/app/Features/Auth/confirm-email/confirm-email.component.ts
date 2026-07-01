import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputOtpModule } from 'primeng/inputotp';
import { AuthService } from '../../../Core/Services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputOtpModule],
  templateUrl: './confirm-email.component.html',
})
export class ConfirmEmailComponent implements OnInit {
  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  email = signal('');
  loading = signal(false);
  resending = signal(false);

  form = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.email.set(email);

    // If no email in query params, they landed here directly — send back to register
    if (!email) this.router.navigateByUrl('/auth/register');
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    this.auth.confirmEmail({ email: this.email(), code: this.form.getRawValue().code! })
      .subscribe({
        next: () => this.router.navigateByUrl('/auth/login'),
        error: () => this.loading.set(false),
      });
  }

  onResend(): void {
    this.resending.set(true);

    this.auth.resendConfirmationEmail({ email: this.email() })
      .subscribe({
        next: () => this.resending.set(false),
        error: () => this.resending.set(false),
      });
  }
}