import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SnackbarService } from '../Services/snackbar.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { LoginRequest } from '../../Shared/Models/Auth.Modules';
import { SKIP_ERROR_SNACKBAR } from './skip-error-interceptor.token';

const EMAIL_NOT_CONFIRMED_CODE = 'User.EmailNotConfirmed';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = err.error;

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

      // FluentValidation
      if (body?.errors && !Array.isArray(body.errors) && typeof body.errors === 'object') {
        Object.values(body.errors as Record<string, string[]>)
          .flat()
          .forEach(msg => snackbar.error(msg));

      // Result pattern
      } else if (body?.detail) {
        snackbar.error(body.detail);  // e.g. "Invalid email/password"

      // Fallback
      } else {
        const responseBody = err.error;

        if (responseBody?.errors && !Array.isArray(responseBody.errors) && typeof responseBody.errors === 'object') {
          Object.values(responseBody.errors as Record<string, string[]>)
            .flat()
            .forEach(msg => snackbar.error(msg));

        } else if (responseBody?.detail) {
          snackbar.error(responseBody.detail);

        } else if (responseBody?.title) {
          snackbar.error(responseBody.title);

        } else if (typeof responseBody === 'string') {
          snackbar.error(responseBody);

        } else if (err.statusText) {
          snackbar.error(`${err.status} ${err.statusText}`);

        } else {
          snackbar.error('Unexpected error');
        }
      }

      return throwError(() => err);
    })
  );
};
