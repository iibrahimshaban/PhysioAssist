import { Routes } from '@angular/router';
import { noAuthGuard } from './Core/Guards/no-auth-guard';
import { authGuard } from './Core/Guards/auth-guard';
import { MainLayoutComponent } from './Layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./Features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'not-found',
    loadComponent: () =>
      import('./Shared/Components/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },
  {
    path: 'server-error',
    loadComponent: () =>
      import('./Shared/Components/server-error/server-error.component').then(
        (m) => m.ServerErrorComponent,
      ),
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./Shared/Components/unauthorized/unauthorized.component').then(
        (m) => m.UnauthorizedComponent,
      ),
  },
  {
    path: 'auth',
    canActivate: [noAuthGuard],
    loadChildren: () => import('./Features/Auth/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'app',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    loadChildren: () =>
      import('./Layout/main-layout/main-layout.routes').then((m) => m.MainLayoutRoutes),
  },
  {
    path: 'working-schedule',
    loadComponent: () =>
      import('./Features/WorkingSchedule/working-schedule.component').then(
        (m) => m.WorkingScheduleComponent,
      ),
  },
  {
    path: 'schedule',
    loadComponent: () =>
      import('./Features/Schedule/schedule-page.component').then((m) => m.SchedulePageComponent),
  },
  {
    path: 'public',
    loadChildren: () => import('./Features/intake/intake.routes').then((m) => m.publicIntakeRoutes),
  },
  { path: '**', redirectTo: 'not-found' },
];
