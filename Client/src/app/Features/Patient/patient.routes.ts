import { Routes } from '@angular/router';

export const patientRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./patient-list/patient-list.component').then((m) => m.PatientListComponent),
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./patient-form/patient-form.component').then((m) => m.PatientFormComponent),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./patient-form/patient-form.component').then((m) => m.PatientFormComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./patient-detail/patient-detail.component').then((m) => m.PatientDetailComponent),
  },
  { path: ':id/overview', loadComponent: () => import('./patient-overview/patient-overview.component').then(m => m.PatientOverviewComponent) },
];