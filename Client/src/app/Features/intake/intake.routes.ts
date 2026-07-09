import { Routes } from '@angular/router';
import { authGuard } from '../../Core/Guards/auth.guard';
import { SchemaListComponent } from './pages/schema-list/schema-list.component';
import { SchemaBuilderComponent } from './pages/schema-builder/schema-builder.component';
import { SubmissionListComponent } from './pages/submission-list/submission-list.component';
import { SubmissionDetailComponent } from './pages/submission-detail/submission-detail.component';
import { PublicIntakeComponent } from './pages/public-intake/public-intake.component';

export const intakeRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'schemas', component: SchemaListComponent },
      { path: 'schemas/new', component: SchemaBuilderComponent },
      { path: 'schemas/edit/:id', component: SchemaBuilderComponent },
      { path: 'submissions', component: SubmissionListComponent },
      { path: 'submissions/:id', component: SubmissionDetailComponent },
      { path: '', redirectTo: 'schemas', pathMatch: 'full' }
    ]
  }
];

export const publicIntakeRoutes: Routes = [
  {
    path: 'intake/:token',
    component: PublicIntakeComponent
  },
  {
    path: ':token',
    component: PublicIntakeComponent
  }
];
