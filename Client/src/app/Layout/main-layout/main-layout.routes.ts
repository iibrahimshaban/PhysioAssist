import { Routes } from '@angular/router';
import { permissionGuard } from '../../Core/Guards/permission-guard';

export const MainLayoutRoutes : Routes = [
  {path: '', redirectTo: 'dashboard', pathMatch: 'full'},
  {
    path: 'dashboard',
    loadComponent: () => import('../../Features/dashboard/dashboard.component').then((c) => c.DashboardComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Dashboard:Read'] },
  },
  { path: 'intake', loadChildren: () => import('../../Features/intake/intake.routes').then((m) => m.intakeRoutes) },
  { path: 'account', loadComponent: () => import('../../Features/account/account.component').then((m) => m.AccountComponent) },
  {
    path: 'account/staff',
    loadComponent: () => import('../../Features/staff/staff.component').then((m) => m.StaffComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['receptionist:read'] },
  },
  {
    path: 'patients', 
    loadChildren: () => import('../../Features/Patient/patient.routes').then((m) => m.patientRoutes),
    canActivate: [permissionGuard],
    data: { permissions: ['Patient:Read']}
  },
  {
    path: 'initial-report/:patientId', 
    loadComponent: () => import('../../Features/initial-report/initial-report.component').then((m) => m.InitialReportComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['InitialReport:Read']}
  },
  {
    path: 'schedule', 
    loadComponent: () => import('../../Features/Schedule/schedule-page.component').then((m) => m.SchedulePageComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Schedule:Read']}
  },
  { 
    path: 'working-schedule',
    loadComponent: () => import('../../Features/WorkingSchedule/working-schedule.component').then((m) => m.WorkingScheduleComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['WorkingSchedule:Read']}
  },
  { 
    path: 'session/:id',
    loadComponent: () => import('../../Features/session/session.component').then((component) => component.SessionComponent ),
    canActivate: [permissionGuard],
    data: { permissions: ['Session:Read']}
  },
  { 
    path: 'receptionist-scheduling/:patientId', 
    loadComponent: () => import('../../Features/receptionist-scheduling/receptionist-scheduling.component').then((c) => c.ReceptionistSchedulingComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Schedule:Write']}
  },
  { 
    path: 'today-sessions', 
    loadComponent: () => import('../../Features/today-sessions-dashboard/today-sessions-dashboard.component').then((c) => c.TodaySessionsDashboardComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['Session:ViewToday']}
  },
  { 
    path: 'schedule-preferences', 
    loadComponent: () => import('../../Features/scheduling-preferences/scheduling-preferences.component').then((c) => c.SchedulingPreferencesComponent),
    canActivate: [permissionGuard],
    data: { permissions: ['WorkingSchedule:Read']}
  }
];