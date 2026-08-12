import { Routes } from '@angular/router';
import { authGuard } from '../../Core/Guards/auth-guard';
import { SchemaListComponent } from './pages/schema-list/schema-list.component';
import { SchemaBuilderComponent } from './pages/schema-builder/schema-builder.component';
import { SchemaWizardComponent } from './pages/schema-wizard/schema-wizard.component';
import { SubmissionListComponent } from './pages/submission-list/submission-list.component';
import { SubmissionDetailComponent } from './pages/submission-detail/submission-detail.component';
import { PublicIntakeComponent } from './pages/public-intake/public-intake.component';
import { ReceptionComponent } from './pages/reception/reception.component';
import { permissionGuard } from '../../Core/Guards/permission-guard';

export const intakeRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'reception',
        component: ReceptionComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Submission:Read'] }
      },
      {
        path: 'schemas',
        component: SchemaListComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Intake:Read'] }
      },
      {
        path: 'schemas/new',
        component: SchemaBuilderComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Intake:ManageForms'] }
      },
      {
        path: 'schemas/wizard',
        component: SchemaWizardComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Intake:ManageForms'] }
      },
      {
        path: 'schemas/edit/:id',
        component: SchemaBuilderComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Intake:Read', 'Intake:ManageForms'] }
      },
      {
        path: 'submissions',
        component: SubmissionListComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Submission:Read'] }
      },
      {
        path: 'submissions/:id',
        component: SubmissionDetailComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Submission:Read'] }
      },
      { path: '', redirectTo: 'reception', pathMatch: 'full' }
    ]
  }
];

export const publicIntakeRoutes: Routes = [
  { path: 'intake', component: PublicIntakeComponent }
];