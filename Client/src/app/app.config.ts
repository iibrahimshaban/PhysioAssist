import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { errorInterceptor } from './Core/Interceptors/error-interceptor';
import { loadingInterceptor } from './Core/Interceptors/loading-interceptor';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { authInterceptor } from './Core/Interceptors/ath-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideZonelessChangeDetection(),
    provideHttpClient(withInterceptors([authInterceptor,errorInterceptor, loadingInterceptor])),
    providePrimeNG({
      theme: {
        preset: Aura,
      },
    }),
  ],
};
