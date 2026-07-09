import { Routes } from '@angular/router';
import { NotFoundComponent } from './Shared/Components/not-found/not-found.component';
import { ServerErrorComponent } from './Shared/Components/server-error/server-error.component';

export const routes: Routes = [
  { path: '', redirectTo: 'intake/schemas', pathMatch: 'full' },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'server-error', component: ServerErrorComponent },
  {
    path: 'intake',
    loadChildren: () => import('./Features/intake/intake.routes').then(m => m.intakeRoutes)
  },
  {
    path: 'public',
    loadChildren: () => import('./Features/intake/intake.routes').then(m => m.publicIntakeRoutes)
  }
];
