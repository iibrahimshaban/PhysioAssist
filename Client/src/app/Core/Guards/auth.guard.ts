import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const token = localStorage.getItem('physioassist_token');
  if (!token) {
    router.navigate(['/not-found']);
    return false;
  }
  return true;
};
