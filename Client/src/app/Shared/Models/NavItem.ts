export interface NavItem {
  label: string;
  labelKey: string;
  route: string;
  icon: string;
  permissions?: string[]; // omit = always visible to any authenticated user
  section: 'primary' | 'settings';
}

export const NAV_ITEMS: NavItem[] = [
  { label: 'Home', labelKey: 'shared.nav.home', route: '/app/dashboard', icon: 'pi pi-home', permissions: ['Dashboard:Read'], section: 'primary' },
  { label: 'Today sessions', labelKey: 'shared.nav.todaySessions', route: '/app/today-sessions', icon: 'pi pi-clock', permissions: ['Session:ViewToday'], section: 'primary' },
  { label: 'Patients', labelKey: 'shared.nav.patients', route: '/app/patients', icon: 'pi pi-users', permissions: ['Patient:Read'], section: 'primary' },
  { label: 'Schedule', labelKey: 'shared.nav.schedule', route: '/app/schedule', icon: 'pi pi-calendar', permissions: ['Schedule:Read'], section: 'primary' },
  { label: 'Reception', labelKey: 'shared.nav.reception', route: '/app/intake/reception', icon: 'pi pi-inbox', permissions: ['Submission:Read'], section: 'primary' },

  { label: 'Working Schedule', labelKey: 'shared.nav.workingSchedule', route: '/app/working-schedule', icon: 'pi pi-clock', permissions: ['WorkingSchedule:Read'], section: 'settings' },
  { label: 'schedule-preferences', labelKey: 'shared.nav.schedulePreferences', route: '/app/schedule-preferences', icon: 'pi pi-sliders-h', permissions: ['WorkingSchedule:Read'], section: 'settings' },
  { label: 'Submissions', labelKey: 'shared.nav.submissions', route: '/app/intake/submissions', icon: 'pi pi-inbox', permissions: ['Submission:Read'], section: 'settings' },
  { label: 'Staff', labelKey: 'shared.nav.staff', route: '/app/account/staff', icon: 'pi pi-id-card', permissions: ['receptionist:read'], section: 'settings' },
  { label: 'Intake Schemas', labelKey: 'shared.nav.intakeSchemas', route: '/app/intake/schemas', icon: 'pi pi-file-edit', permissions: ['Intake:Read'], section: 'settings' },
  { label: 'Documentation Templates', labelKey: 'shared.nav.documentationTemplates', route: '/app/Documentation-settings', icon: 'pi pi-clipboard', section: 'settings' },
];