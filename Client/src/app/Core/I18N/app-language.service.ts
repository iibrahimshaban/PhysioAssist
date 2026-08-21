import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

export type AppLang = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class AppLanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly document = inject(DOCUMENT);

  /** Detect browser language on init: 'ar' if the browser language starts
   *  with "ar", otherwise English (default). */
  static detectBrowserLang(): AppLang {
    const langs: readonly string[] =
      typeof navigator !== 'undefined' && navigator.languages?.length
        ? navigator.languages
        : typeof navigator !== 'undefined'
          ? [navigator.language]
          : [];
    return langs.some(l => l?.toLowerCase().startsWith('ar')) ? 'ar' : 'en';
  }

  init(): void {
    const lang = AppLanguageService.detectBrowserLang();
    this.transloco.setDefaultLang('en');
    this.transloco.setActiveLang(lang);
    this.applyDocumentAttributes(lang);
    this.transloco.langChanges$.subscribe(active => this.applyDocumentAttributes(active as AppLang));
  }

  private applyDocumentAttributes(lang: AppLang): void {
    const html = this.document.documentElement;
    html.setAttribute('lang', lang);
    html.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');
  }
}
