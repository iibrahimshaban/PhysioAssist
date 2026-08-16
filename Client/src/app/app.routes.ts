import { Routes } from '@angular/router';
import { noAuthGuard } from './Core/Guards/no-auth-guard';
import { authGuard } from './Core/Guards/auth-guard';
import { MainLayoutComponent } from './Layout/main-layout/main-layout.component';
import { homeRedirectGuard } from './Core/Guards/home-redirect-guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [homeRedirectGuard],
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
    path: 'terms',
    loadComponent: () =>
      import('./Features/Legal/terms/terms.component').then((m) => m.TermsComponent),
  },
  {
    path: 'privacy-policy',
    loadComponent: () =>
      import('./Features/Legal/privacy-policy/privacy-policy.component').then(
        (m) => m.PrivacyPolicyComponent,
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
    path: 'public',
    loadChildren: () => import('./Features/intake/intake.routes').then((m) => m.publicIntakeRoutes),
  },
  { path: '**', redirectTo: 'not-found' },
];