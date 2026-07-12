import { Routes } from '@angular/router';

export const MainLayoutRoutes : Routes = [
  { path: 'intake', loadChildren: () => import('../../Features/intake/intake.routes').then((m) => m.intakeRoutes) },
  { path: 'account', loadComponent: () => import('../../Features/account/account.component').then((m) => m.AccountComponent) },
  { path: 'patients', loadChildren: () => import('../../Features/Patient/patient.routes').then((m) => m.patientRoutes) },
  { path: 'initial-report', loadComponent: () => import('../../Features/initial-report/initial-report.component').then((m) => m.InitialReportComponent) },
  { path: 'schedule', loadComponent: () => import('../../Features/Schedule/schedule-page.component').then((m) => m.SchedulePageComponent) },
  { path: 'session', loadComponent: () => import('../../Features/session/session.component').then((m) => m.SessionComponent) },
];