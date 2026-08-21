import '@angular/compiler';
import { describe, expect, it, beforeAll, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Provider } from '@angular/core';
import { BehaviorSubject, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { provideRouter, convertToParamMap, ActivatedRoute } from '@angular/router';
import { SocialAuthService } from '@abacritt/angularx-social-login';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import enCatalog from '../../../assets/i18n/auth/en.json';
import arCatalog from '../../../assets/i18n/auth/ar.json';
import sharedEn from '../../../assets/i18n/shared/en.json';
import sharedAr from '../../../assets/i18n/shared/ar.json';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ForgotPasswordComponent } from './forget-password/forget-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { VerifyOtpComponent } from './verify-otp/verify-otp.component';
import { ConfirmEmailComponent } from './confirm-email/confirm-email.component';
import { GoogleOnboardingComponent } from './google-onboarding/google-onboarding.component';
import { AuthService } from '../../Core/Services/auth.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import { errorInterceptor } from '../../Core/Interceptors/error-interceptor';

function flatten(o: unknown, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(o as Record<string, unknown>)) {
    const p = prefix ? `${prefix}${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      Object.assign(out, flatten(v, `${p}.`));
    } else {
      out[p] = String(v);
    }
  }
  return out;
}

const translations: Record<'en' | 'ar', Record<string, string>> = {
  en: { ...flatten(enCatalog, 'auth.'), ...flatten({ shared: sharedEn }) },
  ar: { ...flatten(arCatalog, 'auth.'), ...flatten({ shared: sharedAr }) },
};

const lang$ = new BehaviorSubject<'en' | 'ar'>('en');

const translocoStub = {
  langChanges$: lang$,
  config: { reRenderOnLangChange: true },
  getActiveLang: () => lang$.value,
  translate: (key: string, params?: Record<string, unknown>) => {
    let t = translations[lang$.value][key] ?? `[${key}]`;
    if (params) {
      for (const [k, v] of Object.entries(params)) t = t.split(`{{${k}}}`).join(String(v));
    }
    return t;
  },
  _loadDependencies: () => of(null),
};

const authStub = {
  register: vi.fn(() => of({})),
  login: vi.fn(() => of({})),
  loginWithGoogle: vi.fn(() => of({})),
  handleAuthResponse: vi.fn(),
  forgetPassword: vi.fn(() => of({})),
  verifyResetOtp: vi.fn(() => of({})),
  saveResetOtp: vi.fn(),
  getResetOtp: () => '123456',
  clearResetOtp: vi.fn(),
  confirmEmail: vi.fn(() => of({})),
  resendConfirmationEmail: vi.fn(() => of({})),
  completeGoogleOnboarding: vi.fn(() => of({})),
};

async function mount(component: any, extraProviders: Provider[] = []) {
  await TestBed.configureTestingModule({
    imports: [component],
    providers: [provideRouter([]), ...extraProviders],
  })
    .overrideProvider(TranslocoService, { useValue: translocoStub })
    .overrideProvider(AuthService, { useValue: authStub })
    .overrideProvider(SocialAuthService, { useValue: { authState: of(null), initState: of(true) } })
    .compileComponents();

  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  return fixture;
}

function text(fixture: any): string {
  return (fixture.nativeElement as HTMLElement).textContent ?? '';
}describe('Auth screens i18n DOM rendering', () => {
  beforeAll(() => {
    const w = window as any;
    if (typeof w.matchMedia !== 'function') {
      w.matchMedia = (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => {},
        removeListener: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => false,
      });
    }
  });

  it('LOGIN en: heading, labels, placeholder, button all canonical', async () => {
    lang$.next('en');
    const f = await mount(LoginComponent, [{ provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } }]);
    const el: HTMLElement = f.nativeElement;

    expect(text(f)).toContain(translations.en['auth.login.title']);
    expect(el.querySelector<HTMLInputElement>('input[type="email"]')!.placeholder)
      .toBe(translations.en['auth.placeholder.drEmail']);
    const btnLabels = Array.from(el.querySelectorAll('.p-button-label')).map(b => b.textContent!.trim());
    expect(btnLabels).toContain(translations.en['auth.login.signIn']);
  });

  it('LOGIN ar: everything Arabic, zero English leak', async () => {
    lang$.next('ar');
    const f = await mount(LoginComponent, [{ provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } }]);
    const el: HTMLElement = f.nativeElement;
    const t = text(f);

    expect(t).toContain(translations.ar['auth.marketing.login.title']);
    expect(t).toContain(translations.ar['auth.login.title']);
    expect(t).toContain(translations.ar['auth.login.rememberMe']);
    expect(t).toContain(translations.ar['auth.login.forgotPassword']);
    expect(el.querySelector<HTMLInputElement>('input[type="email"]')!.placeholder)
      .toBe(translations.ar['auth.placeholder.drEmail']);
    expect(Array.from(el.querySelectorAll('.p-button-label')).map(b => b.textContent!.trim()))
      .toContain(translations.ar['auth.login.signIn']);
    expect(t).not.toContain('Welcome back');
    expect(t).not.toContain('Remember me');
    expect(t).not.toContain('Forgot password?');
    expect(t).not.toContain('Sign in');
  });

  it('REGISTER en: placeholders including p-password inner input', async () => {
    lang$.next('en');
    const f = await mount(RegisterComponent);
    const el: HTMLElement = f.nativeElement;

    expect(el.querySelector<HTMLInputElement>('input[formcontrolname="firstName"], input[formControlName="firstName"]')!.placeholder)
      .toBe('Sarah');
    const pwInput = el.querySelector<HTMLInputElement>('p-password input')!;
    expect(pwInput.placeholder).toBe(translations.en['auth.placeholder.passwordMin']);
    expect(text(f)).toContain('I agree to the');
    expect(text(f)).toContain('Terms');
    expect(text(f)).toContain('Privacy Policy');
  });

  it('REGISTER ar: Arabic placeholders, agree-label segments, validation message composes in Arabic', async () => {
    lang$.next('ar');
    const f = await mount(RegisterComponent);
    const el: HTMLElement = f.nativeElement;
    const t = text(f);

    expect(el.querySelector<HTMLInputElement>('input[formcontrolname="firstName"], input[formControlName="firstName"]')!.placeholder)
      .toBe(translations.ar['auth.placeholder.firstName']);
    expect(el.querySelector<HTMLInputElement>('p-password input')!.placeholder)
      .toBe(translations.ar['auth.placeholder.passwordMin']);
    expect(t).toContain(translations.ar['auth.register.agreePrefix']);
    expect(t).toContain(translations.ar['auth.register.termsLink']);
    expect(t).toContain(translations.ar['auth.register.andWord']);
    expect(t).toContain(translations.ar['auth.register.privacyLink']);

    // Validation composition: required-message template with Arabic field name
    const comp = f.componentInstance as any;
    comp.form.get('firstName').setValue('');
    comp.form.get('firstName').markAsTouched();
    expect(comp.getFieldError('firstName'))
      .toBe(translations.ar['auth.validation.required'].replace('{{field}}', translations.ar['auth.fields.firstName']));
    expect(t).not.toContain('Create your account');
    expect(t).not.toContain('Upload photo');
  });

  it('FORGOT en: title, subtitle, button, placeholder', async () => {
    lang$.next('en');
    const fe = await mount(ForgotPasswordComponent);
    expect(text(fe)).toContain(translations.en['auth.forgot.title']);
    expect(text(fe)).toContain(translations.en['auth.forgot.sendCode']);
    expect((fe.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="email"]')!.placeholder)
      .toBe(translations.en['auth.placeholder.email']);
  });

  it('FORGOT ar: everything Arabic, zero English leak', async () => {
    lang$.next('ar');
    const fa = await mount(ForgotPasswordComponent);
    const ta = text(fa);
    expect(ta).toContain(translations.ar['auth.forgot.title']);
    expect(ta).toContain(translations.ar['auth.forgot.subtitle']);
    expect(ta).toContain(translations.ar['auth.forgot.rememberIt']);
    expect(Array.from(fa.nativeElement.querySelectorAll('.p-button-label')).map((b: any) => b.textContent!.trim()))
      .toContain(translations.ar['auth.forgot.sendCode']);
    expect(ta).not.toContain('Forgot your password?');
    expect(ta).not.toContain('Send reset code');
  });

  it('OTP en: sent-code line, info box, buttons', async () => {
    lang$.next('en');
    const fe = await mount(VerifyOtpComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    const te = text(fe);
    expect(te).toContain(translations.en['auth.otp.sentCodeTo']);
    expect(te).toContain('doc@clinic.com');
    expect(te).toContain(translations.en['auth.otp.infoBox']);
    expect(te).toContain(translations.en['auth.otp.verifyCode']);
    expect(te).toContain(translations.en['auth.otp.resendCode']);
  });

  it('OTP ar: Arabic copy incl. inline error, zero English leak', async () => {
    lang$.next('ar');
    const fa = await mount(VerifyOtpComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    // Inline error renders only once the OTP control is touched
    const comp = fa.componentInstance as any;
    comp.form.get('otp').setValue('');
    comp.form.get('otp').markAsTouched();
    fa.detectChanges();

    const ta = text(fa);
    expect(ta).toContain(translations.ar['auth.otp.title']);
    expect(ta).toContain(translations.ar['auth.otp.fullCodeRequired']);
    expect(ta).toContain(translations.ar['auth.otp.wrongAddress']);
    expect(ta).not.toContain('Enter reset code');
    expect(ta).not.toContain('Resend code');
  });

  it('CONFIRM en: activation copy and buttons', async () => {
    lang$.next('en');
    const fe = await mount(ConfirmEmailComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    expect(text(fe)).toContain(translations.en['auth.confirm.infoBox']);
    expect(text(fe)).toContain(translations.en['auth.confirm.resendEmail']);
  });

  it('CONFIRM ar: Arabic copy, zero English leak', async () => {
    lang$.next('ar');
    const fa = await mount(ConfirmEmailComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    const ta = text(fa);
    expect(ta).toContain(translations.ar['auth.confirm.title']);
    expect(ta).toContain(translations.ar['auth.confirm.verifyEmail']);
    expect(ta).not.toContain('Check your inbox');
    expect(ta).not.toContain('Verify email');
  });

  it('RESET en: labels and both p-password placeholders', async () => {
    lang$.next('en');
    const fe = await mount(ResetPasswordComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    const pwInputs = (fe.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>('p-password input');
    expect(pwInputs.length).toBe(2);
    expect(pwInputs[0].placeholder).toBe(translations.en['auth.placeholder.passwordMin']);
    expect(pwInputs[1].placeholder).toBe(translations.en['auth.placeholder.confirmPassword']);
    expect(text(fe)).toContain(translations.en['auth.reset.updatePassword']);
  });

  it('RESET ar: Arabic labels, zero English leak', async () => {
    lang$.next('ar');
    const fa = await mount(ResetPasswordComponent, [
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ email: 'doc@clinic.com' }) } } },
    ]);
    const ta = text(fa);
    expect(ta).toContain(translations.ar['auth.reset.newPassword']);
    expect(ta).toContain(translations.ar['auth.reset.confirmPassword']);
    expect(ta).toContain(translations.ar['auth.reset.backTo']);
    expect(ta).not.toContain('Set a new password');
    expect(ta).not.toContain('Update password');
  });

  it('ONBOARDING en: labels and button', async () => {
    lang$.next('en');
    const fe = await mount(GoogleOnboardingComponent);
    const te = text(fe);
    expect(te).toContain(translations.en['auth.onboarding.clinicName']);
    expect(te).toContain(translations.en['auth.onboarding.createAccount']);
  });

  it('ONBOARDING ar: Arabic labels + placeholders, zero English leak', async () => {
    lang$.next('ar');
    const fa = await mount(GoogleOnboardingComponent);
    const el = fa.nativeElement as HTMLElement;
    const ta = text(fa);
    expect(ta).toContain(translations.ar['auth.onboarding.firstName']);
    expect(el.querySelector<HTMLInputElement>('input[formcontrolname="clinicName"], input[formControlName="clinicName"]')!.placeholder)
      .toBe(translations.ar['auth.placeholder.clinicName']);
    expect(ta).not.toContain('Clinic name is required.');
    expect(ta).not.toContain('Create account');
  });
});

describe('errorInterceptor client-authored messages i18n', () => {
  beforeAll(() => {
    const w = window as any;
    if (typeof w.matchMedia !== 'function') {
      w.matchMedia = (query: string) => ({
        matches: false, media: query, onchange: null,
        addListener: () => {}, removeListener: () => {},
        addEventListener: () => {}, removeEventListener: () => {}, dispatchEvent: () => false,
      });
    }
  });

  function setupInterceptorTest() {
    const snackbarSpy = { error: vi.fn(), success: vi.fn(), warning: vi.fn(), info: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: SnackbarService, useValue: snackbarSpy },
      ],
    }).overrideProvider(TranslocoService, { useValue: translocoStub });
    const http = TestBed.inject(HttpTestingController);
    const client = TestBed.inject(HttpClient);
    return { snackbarSpy, http, client };
  }

  it('translates offline/canT-reach-server message under ar', () => {
    lang$.next('ar');
    const { snackbarSpy, http, client } = setupInterceptorTest();

    client.get('https://api.test/x').subscribe({ error: () => {} });
    const req = http.expectOne('https://api.test/x');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(snackbarSpy.error).toHaveBeenCalledWith(translations.ar['shared.errors.cantReachServer']);
    http.verify();
  });

  it('keeps backend-pushed detail messages untranslated (boundary)', () => {
    lang$.next('ar');
    const { snackbarSpy, http, client } = setupInterceptorTest();

    client.get('https://api.test/y').subscribe({ error: () => {} });
    const req = http.expectOne('https://api.test/y');
    req.flush({ detail: 'Invalid email/password' }, { status: 400, statusText: 'Bad Request' });

    expect(snackbarSpy.error).toHaveBeenCalledWith('Invalid email/password');
    http.verify();
  });
});
