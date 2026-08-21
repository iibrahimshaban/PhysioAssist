import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Translation, TranslocoLoader } from '@jsverse/transloco';
import { forkJoin, map } from 'rxjs';

/**
 * Loads the translation catalogs from /assets/i18n/{intake,shared,auth}/{en,ar}.json
 * and nests them under their module keys so template/service references like
 * `intake.common.tryAgain`, `shared.nav.reception` or `auth.login.signIn`
 * resolve against the root translation scope. Each module owns a separate
 * file set and top-level namespace, so they can never collide.
 */
@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(lang: string) {
    return forkJoin({
      intake: this.http.get<Translation>(`./assets/i18n/intake/${lang}.json`),
      shared: this.http.get<Translation>(`./assets/i18n/shared/${lang}.json`),
      auth: this.http.get<Translation>(`./assets/i18n/auth/${lang}.json`),
    }).pipe(
      map(({ intake, shared, auth }) => ({ intake, shared, auth }) as Translation),
    );
  }
}
