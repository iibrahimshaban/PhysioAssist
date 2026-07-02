import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../Services/auth.service';


let isRefreshing = false;
const newToken$ = new BehaviorSubject<string | null>(null);


export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  const token = authService.getToken();
  const outgoing = token ? withBearer(req, token) : req;

  return next(outgoing).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return handle401(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

function handle401(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
) {
  if (isRefreshing) {
    return newToken$.pipe(
      filter((token): token is string => token !== null),
      take(1),
      switchMap(token => next(withBearer(req, token)))
    );
  }

  isRefreshing = true;
  newToken$.next(null);

  const refresh$ = authService.refreshToken();

  if (!refresh$) {
    isRefreshing = false;
    authService.logout();
    return throwError(() => new Error('No refresh token'));
  }

  return refresh$.pipe(
    switchMap(newAuth => {
      isRefreshing = false;
      newToken$.next(newAuth.token);
      return next(withBearer(req, newAuth.token));
    }),
    catchError(err => {
      isRefreshing = false;
      authService.logout();
      return throwError(() => err);
    })
  );
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthEndpoint(url: string): boolean {
  return url.includes('/api/auth/');
}
