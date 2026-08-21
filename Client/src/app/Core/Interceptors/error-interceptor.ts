import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { SnackbarService } from '../Services/snackbar.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { LoginRequest } from '../../Shared/Models/Auth.Modules';
import { SKIP_ERROR_SNACKBAR } from './skip-error-interceptor.token';

const EMAIL_NOT_CONFIRMED_CODE = 'User.EmailNotConfirmed';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);
  const transloco = inject(TranslocoService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = err.error;

      if (err.status === 0) {
        const message = navigator.onLine
          ? transloco.translate('shared.errors.cantReachServer')
          : transloco.translate('shared.errors.noInternet');

        snackbar.error(message);
        return throwError(() => err);
      }

      // Caller opted out of the snackbar for an expected 404 (e.g. "no schedule yet").
      // Other statuses on the same request still go through normal handling below.
      if (err.status === 404 && req.context.get(SKIP_ERROR_SNACKBAR)) {
        return throwError(() => err);
      }

      if (err.status === 500) {
        router.navigateByUrl('/server-error', { state: { error: body } });
        return throwError(() => err);
      }

      // Special case: email not confirmed -> redirect instead of showing a snackbar
      if (err.status === 401 && body?.title === EMAIL_NOT_CONFIRMED_CODE) {
        const email = (req.body as LoginRequest | null)?.email;

        router.navigate(['/auth/confirm-email'], {
          queryParams: email ? { email } : undefined,
        });

        return throwError(() => err);
      }

      if (err.status === 403) {
        window.open('/unauthorized', '_blank');
      }

      // NOTE: Backend-pushed messages below (FluentValidation `body.errors`,
      // Result-pattern `body.detail`, ProblemDetails `body.title`) are authored
      // server-side and intentionally shown as-is — translating them is out of
      // scope for the client i18n effort. Only client-authored strings above
      // are localized.
      // FluentValidation
      if (body?.errors && !Array.isArray(body.errors) && typeof body.errors === 'object') {
        Object.values(body.errors as Record<string, string[]>)
          .flat()
          .forEach(msg => snackbar.error(msg));

      // Result pattern
      } else if (body?.detail) {
        snackbar.error(body.detail);  // e.g. "Invalid email/password"

      // Fallback
      } else if (body?.title) {
        snackbar.error(body.title);

      } else if (typeof body === 'string') {
        snackbar.error(body);

      } else if (err.statusText) {
        snackbar.error(`${err.status} ${err.statusText}`);

      } else {
        snackbar.error(transloco.translate('shared.errors.unexpectedError'));
      }

      return throwError(() => err);
    })
  );
};