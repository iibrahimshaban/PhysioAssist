import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../Services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const platformId = inject(PLATFORM_ID);

  if (isAuthEndpoint(req.url)) {
    return next(addLangHeader(req, platformId));
  }

  const token = authService.getToken();

  if (token && authService.isTokenExpiringSoon(token)) {
    return refreshAndRetry(req, next, authService, platformId);
  }

  const outgoing = addLangHeader(token ? withBearer(req, token) : req, platformId);

  return next(outgoing).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return refreshAndRetry(req, next, authService, platformId);
      }
      return throwError(() => error);
    }),
  );
};

function refreshAndRetry(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  platformId: object,
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
    switchMap((newAuth) => next(addLangHeader(withBearer(req, newAuth.token), platformId))),
    catchError((err) => {
      authService.logout();
      return throwError(() => err);
    }),
  );
}

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthEndpoint(url: string): boolean {
  return url.includes('/api/auth/');
}

function addLangHeader(req: HttpRequest<unknown>, platformId: object): HttpRequest<unknown> {
  // localStorage is not available during SSR, so we fall back to 'en' on the server.
  const lang = isPlatformBrowser(platformId) ? (localStorage.getItem('lang') ?? 'en') : 'en';

  return req.clone({ setHeaders: { lang: lang } });
}
