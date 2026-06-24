import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BusyService } from '../Services/busy.service';
import { defer, delay, finalize } from 'rxjs';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
   const busyService = inject(BusyService)

    busyService.busy();

    return next(req).pipe(
      delay(500),
      finalize(() => busyService.idle())
    );
};
