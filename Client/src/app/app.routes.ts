import { Routes } from '@angular/router';
import { NotFoundComponent } from './Shared/Components/not-found/not-found.component';
import { ServerErrorComponent } from './Shared/Components/server-error/server-error.component';
import { TestErrorComponent } from './Features/test-error/test-error.component';
import { TestprimengComponent } from './Features/testprimeng/testprimeng.component';
import { noAuthGuard } from './Core/Guards/no-auth-guard';
import { authGuard } from './Core/Guards/auth-guard';
import { permissionGuard } from './Core/Guards/permission-guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./Features/home/home.component').then(m => m.HomeComponent) },

  { path: 'test-error', component: TestErrorComponent },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'prime', component: TestprimengComponent },
  { path: 'server-error', component: ServerErrorComponent },
  { path: 'unauthorized', loadComponent: () => import('./Shared/Components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent) },

  {
    path: 'auth',
    canActivate: [noAuthGuard],
    children: [
      { path: 'login',           loadComponent: () => import('./Features/Auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register',        loadComponent: () => import('./Features/Auth/register/register.component').then(m => m.RegisterComponent) },
      { path: 'confirm-email',   loadComponent: () => import('./Features/Auth/confirm-email/confirm-email.component').then(m => m.ConfirmEmailComponent) },
      { path: 'forgot-password', loadComponent: () => import('./Features/Auth/forget-password/forget-password.component').then(m => m.ForgotPasswordComponent) },
      { path: 'reset-password',  loadComponent: () => import('./Features/Auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
      { path: 'verify-otp',      loadComponent: () => import('./Features/Auth/verify-otp/verify-otp.component').then(m => m.VerifyOtpComponent) },
    ],
  },

  {
    path: 'app',          // ← protected routes now live under /app
    canActivate: [authGuard],
     children: [
    {
      path: 'account',
      loadComponent: () =>
        import('./Features/account/account.component')
          .then(m => m.AccountComponent)
    },

    {
      path: 'patients',
      canActivate: [permissionGuard],
      data: { permissions: ['User:create'] },
      loadComponent: () =>
        import('./Features/weather/weather.component')
          .then(m => m.WeatherComponent),
    },

    {
      path: 'schedule',
      loadComponent: () =>
        import('./Features/Schedule/schedule-page.component')
          .then(m => m.SchedulePageComponent),
    },
  ],
  },

  { path: '**', redirectTo: 'not-found' },
];

