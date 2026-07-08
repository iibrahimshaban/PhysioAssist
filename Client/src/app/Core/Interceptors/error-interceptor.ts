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

        if (body?.errors && !Array.isArray(body.errors) && typeof body.errors === 'object') {
          Object.values(body.errors as Record<string, string[]>)
            .flat()
            .forEach(msg => snackbar.error(msg));

        } else if (body?.detail) {
          snackbar.error(body.detail);

        } else if (body?.title) {
          snackbar.error(body.title);

        } else if (typeof body === 'string') {
          snackbar.error(body);

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
