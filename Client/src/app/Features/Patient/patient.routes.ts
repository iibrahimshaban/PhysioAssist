import { Routes } from '@angular/router';
import { permissionGuard } from '../../Core/Guards/permission-guard';

export const patientRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./patient-list/patient-list.component').then((m) => m.PatientListComponent),
  },
  {
    path: 'create',
    loadComponent: () => import('./patient-create/patient-create.component').then((m) => m.PatientCreateComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Patient:Write']}
  },
  {
    path: 'edit/:id',
    loadComponent: () => import('./patient-form/patient-form.component').then((m) => m.PatientFormComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Patient:Write']}
  },
  {
    path: ':id',
    redirectTo: ':id/overview',
    pathMatch: 'full',
  },
  { path: ':id/overview', loadComponent: () => import('./patient-overview/patient-overview.component').then(m => m.PatientOverviewComponent) },
];