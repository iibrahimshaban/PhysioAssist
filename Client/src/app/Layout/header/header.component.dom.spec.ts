import '@angular/compiler';
import { describe, expect, it } from 'vitest';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import enIntake from '../../../assets/i18n/intake/en.json';
import arIntake from '../../../assets/i18n/intake/ar.json';
import enShared from '../../../assets/i18n/shared/en.json';
import arShared from '../../../assets/i18n/shared/ar.json';
import { HeaderComponent } from './header.component';
import { AuthService } from '../../Core/Services/auth.service';

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
  en: { ...flatten(enIntake, 'intake.'), ...flatten(enShared, 'shared.') },
  ar: { ...flatten(arIntake, 'intake.'), ...flatten(arShared, 'shared.') },
};

const lang$ = new BehaviorSubject<'en' | 'ar'>('en');

const translocoStub = {
  langChanges$: lang$,
  config: { reRenderOnLangChange: true },
  getActiveLang: () => lang$.value,
  translate: (key: string) => translations[lang$.value][key] ?? `[${key}]`,
  _loadDependencies: () => of(null),
};

function makeAuthStub(authenticated: boolean) {
  return {
    isAuthenticated: signal(authenticated),
    currentUser: signal<any>(authenticated ? { firstName: 'Sara', profilePictureUrl: null } : null),
    isDoctor: signal(true),
    hasPermission: () => true,
    logout: () => {},
  };
}

async function mount(authenticated: boolean) {
  await TestBed.configureTestingModule({
    imports: [HeaderComponent],
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: makeAuthStub(authenticated) },
      { provide: TranslocoService, useValue: translocoStub },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(HeaderComponent);
  fixture.detectChanges();
  return fixture;
}

describe('HeaderComponent nav i18n DOM rendering', () => {
  it('renders English primary nav labels under en', async () => {
    lang$.next('en');
    const fixture = await mount(true);
    const links = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('nav a')).map(a => a.textContent!.trim());
    expect(links).toContain('Home');
    expect(links).toContain('Reception');
  });

  it('renders Arabic primary nav and account dropdown under ar', async () => {
    lang$.next('ar');
    const fixture = await mount(true);
    const el: HTMLElement = fixture.nativeElement;

    const navLinks = Array.from(el.querySelectorAll('nav a')).map(a => a.textContent!.trim());    expect(navLinks).toContain(translations.ar['shared.nav.home']);
    expect(navLinks).toContain(translations.ar['shared.nav.reception']);
    expect(navLinks).not.toContain('Home');

    const accountTrigger = el.querySelector('nav .relative > button') as HTMLButtonElement;
    expect(accountTrigger).toBeTruthy();
    accountTrigger.click();
    fixture.detectChanges();

    const dropdownText = el.querySelector('.account-dropdown-panel')!.textContent!;
    expect(dropdownText).toContain(translations.ar['shared.nav.accountSettings']);
    expect(dropdownText).toContain(translations.ar['shared.nav.logout']);
    for (const key of ['workingSchedule', 'schedulePreferences', 'submissions', 'staff', 'intakeSchemas', 'documentationTemplates']) {
      expect(dropdownText).toContain(translations.ar[`shared.nav.${key}`]);
    }
    expect(dropdownText).not.toContain('Account settings');
    expect(dropdownText).not.toContain('Logout');
  });

  it('renders Arabic marketing links under ar when logged out', async () => {
    lang$.next('ar');
    const fixture = await mount(false);
    const el: HTMLElement = fixture.nativeElement;

    const links = Array.from(el.querySelectorAll('nav a')).map(a => a.textContent!.trim());
    expect(links).toEqual([
      translations.ar['shared.nav.marketingFeatures'],
      translations.ar['shared.nav.marketingAiAssistant'],
      translations.ar['shared.nav.marketingScheduling'],
      translations.ar['shared.nav.marketingForPhysios'],
      'Sign in',
      'Start for free',
    ]);
  });
});
