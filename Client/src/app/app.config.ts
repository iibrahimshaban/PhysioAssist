import {
  ApplicationConfig,
  importProvidersFrom,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeAr from '@angular/common/locales/ar';
import localeEn from '@angular/common/locales/en';
import { errorInterceptor } from './Core/Interceptors/error-interceptor';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { authInterceptor } from './Core/Interceptors/ath-interceptor';
import { GoogleLoginProvider, SOCIAL_AUTH_CONFIG, SocialAuthServiceConfig, SocialLoginModule } from '@abacritt/angularx-social-login';
import { environment } from '../environments/environment';
import { AuthService } from './Core/Services/auth.service';
import { firstValueFrom } from 'rxjs';
import { MessageService, ConfirmationService } from 'primeng/api';
import { provideTransloco } from '@jsverse/transloco';
import { TranslocoHttpLoader } from './Core/I18N/transloco-loader';
import { AppLanguageService } from './Core/I18N/app-language.service';

registerLocaleData(localeAr);
registerLocaleData(localeEn);

export const appConfig: ApplicationConfig = {
  providers: [
    MessageService, ConfirmationService,
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideZonelessChangeDetection(),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideTransloco({
      config: {
        availableLangs: ['en', 'ar'],
        defaultLang: 'en',
        fallbackLang: 'en',
        reRenderOnLangChange: true,
        prodMode: environment.production,
      },
      loader: TranslocoHttpLoader,
    }),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: 'none'
        }
      },
    }),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      inject(AppLanguageService).init();
      return firstValueFrom(authService.initializeAuth());
    }),
    importProvidersFrom(SocialLoginModule),
    {
      provide: SOCIAL_AUTH_CONFIG,
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: GoogleLoginProvider.PROVIDER_ID,
            provider: new GoogleLoginProvider(environment.googleClientId, {
              oneTapEnabled: false,
            }),
          },
        ],
      } as SocialAuthServiceConfig,
    },
  ],
};