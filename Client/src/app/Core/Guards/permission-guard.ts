import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../Services/auth.service';
import { inject } from '@angular/core';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const required: string[] = route.data['permissions'] ?? [];

  if (required.length === 0) return true;

  const hasAll = required.every(p => auth.hasPermission(p));

  return hasAll ? true : router.createUrlTree(['/unauthorized']);
  
};
