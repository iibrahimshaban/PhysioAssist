import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../Services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  const token = authService.getToken();

  if (token && authService.isTokenExpiringSoon(token)) {
    return refreshAndRetry(req, next, authService);
  }

  const outgoing = token ? withBearer(req, token) : req;

  return next(outgoing).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return refreshAndRetry(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

function refreshAndRetry(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
) {
  const refresh$ = authService.refreshToken();

  if (!refresh$) {
    authService.logout();
    return throwError(() => new Error('No refresh token'));
  }

  // Multiple concurrent callers all land on the SAME refresh$ (see AuthService.refreshToken),
  // so only one HTTP call to /new-refresh is ever made no matter how many requests
  // triggered a refresh at the same moment.
  return refresh$.pipe(
    switchMap(newAuth => next(withBearer(req, newAuth.token))),
    catchError(err => {
      authService.logout();
      return throwError(() => err);
    })
  );
}

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthEndpoint(url: string): boolean {
  return url.includes('/api/auth/');
}