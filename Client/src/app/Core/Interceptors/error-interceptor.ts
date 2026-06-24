import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { SnackbarService } from '../Services/snackbar.service';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router)
  const snackbar = inject(SnackbarService)

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if(err.status === 400)
        snackbar.error("error 400 found")
      else if (err.status === 404)
        router.navigateByUrl('/not-found')
      else if (err.status === 500){
        const extraData: NavigationExtras = {state: {error: err.error}}
        router.navigateByUrl('/server-error',extraData)
      }
        

      if(err.status === 401)
        snackbar.error("anuthorized error found")

      return throwError(() => err)
    })
  );
};
