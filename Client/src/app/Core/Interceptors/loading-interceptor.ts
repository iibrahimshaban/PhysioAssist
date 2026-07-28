import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BusyService } from '../Services/busy.service';
import { finalize } from 'rxjs';
import { SKIP_GLOBAL_LOADING } from './skip-global-loading.token';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const busyService = inject(BusyService);

  if (req.context.get(SKIP_GLOBAL_LOADING)) {
    return next(req);
  }

  busyService.busy();

  return next(req).pipe(
    finalize(() => busyService.idle())
  );
};