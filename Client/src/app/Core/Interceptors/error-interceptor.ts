import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SnackbarService } from '../Services/snackbar.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 500) {
        router.navigateByUrl('/server-error', { state: { error: err.error } });
      } else {
        const body = err.error;

        // FluentValidation
        if (body?.errors && !Array.isArray(body.errors) && typeof body.errors === 'object') {
          Object.values(body.errors as Record<string, string[]>)
            .flat()
            .forEach(msg => snackbar.error(msg));

        // Result pattern
        } else if (body?.detail) {
          snackbar.error(body.detail);  // "Invalid email/password"

        // Fallback
        } else {
          snackbar.error(body?.title ?? 'Unexpected error');
        }
      }

      return throwError(() => err);
    })
  );
};
