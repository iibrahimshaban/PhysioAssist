import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { GoogleSigninButtonModule, SocialAuthService, GoogleLoginProvider } from '@abacritt/angularx-social-login';
import { AuthService } from '../../../Core/Services/auth.service';
import { requiresOnboarding } from '../../../Shared/Models/Auth.Modules';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslocoModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CheckboxModule,
    GoogleSigninButtonModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly fb          = inject(FormBuilder);
  private readonly auth        = inject(AuthService);
  private readonly router      = inject(Router);
  private readonly socialAuth  = inject(SocialAuthService);
  private readonly transloco   = inject(TranslocoService);

  loading = signal(false);
  googleError = signal('');

  form = this.fb.group({
    email:      ['', [Validators.required, Validators.email]],
    password:   ['', Validators.required],
    rememberMe: [false],
  });

  constructor() {
    this.socialAuth.authState.subscribe(socialUser => {
      if (socialUser?.idToken) {
        this.onGoogleSignIn(socialUser.idToken);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    const { email, password } = this.form.getRawValue();

    this.auth.login({ email: email!, password: password! }).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: () => this.loading.set(false), // error display handled by errorInterceptor
    });
  }

  private onGoogleSignIn(idToken: string): void {
    this.googleError.set('');
    this.loading.set(true);

    this.auth.loginWithGoogle({ idToken }).subscribe({
      next: (res) => {
        if (requiresOnboarding(res)) {
          this.router.navigate(['/auth/google-onboarding'], {
            state: {
              onboardingToken: res.onboardingToken,
              email: res.email,
              suggestedFirstName: res.suggestedFirstName,
              suggestedLastName: res.suggestedLastName,
            },
          });
        } else {
          this.auth.handleAuthResponse(res);
          this.router.navigateByUrl('/app/dashboard');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.googleError.set(
          err.error?.code === 'User.AccountExistsWithPassword'
            ? this.transloco.translate('auth.login.googleAccountExists')
            : this.transloco.translate('auth.login.googleFailed')
        );
      },
    });
  }

  isInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!(control?.invalid && control.touched);
  }

  getEmailError(): string {
    const ctrl = this.form.get('email');
    if (ctrl?.hasError('required')) return this.transloco.translate('auth.validation.required', { field: this.transloco.translate('auth.fields.email') });
    if (ctrl?.hasError('email'))    return this.transloco.translate('auth.validation.email');
    return '';
  }
}