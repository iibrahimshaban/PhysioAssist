import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 || err.status === 403) {
        router.navigateByUrl('/not-found');
      } else if (err.status === 500) {
        router.navigateByUrl('/server-error', { state: { error: err.error } });
      }

      return throwError(() => err);
    })
  );
};
