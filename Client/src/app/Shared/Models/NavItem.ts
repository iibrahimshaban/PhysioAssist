export interface NavItem {
  label: string;
  route: string;
  icon: string;
  permissions?: string[]; // omit = always visible to any authenticated user
  section: 'primary' | 'settings';
}

export const NAV_ITEMS: NavItem[] = [
  { label: 'Home', route: '/app/dashboard', icon: 'pi pi-home', permissions: ['Dashboard:Read'], section: 'primary' },
  { label: 'Today sessions', route: '/app/today-sessions', icon: 'pi pi-clock', permissions: ['Session:ViewToday'], section: 'primary' },
  { label: 'Patients', route: '/app/patients', icon: 'pi pi-users', permissions: ['Patient:Read'], section: 'primary' },
  { label: 'Schedule', route: '/app/schedule', icon: 'pi pi-calendar', permissions: ['Schedule:Read'], section: 'primary' },
  { label: 'Reception', route: '/app/intake/reception', icon: 'pi pi-inbox', permissions: ['Submission:Read'], section: 'primary' },

  { label: 'Working Schedule', route: '/app/working-schedule', icon: 'pi pi-clock', permissions: ['WorkingSchedule:Read'], section: 'settings' },
  { label: 'schedule-preferences', route: '/app/schedule-preferences', icon: 'pi pi-sliders-h', permissions: ['WorkingSchedule:Read'], section: 'settings' },
  { label: 'Submissions', route: '/app/intake/submissions', icon: 'pi pi-inbox', permissions: ['Submission:Read'], section: 'settings' },
  { label: 'Staff', route: '/app/account/staff', icon: 'pi pi-id-card', permissions: ['receptionist:read'], section: 'settings' },
  { label: 'Intake Schemas', route: '/app/intake/schemas', icon: 'pi pi-file-edit', permissions: ['Intake:Read'], section: 'settings' },
  { label: 'Documentation Templates', route: '/app/Documentation-settings', icon: 'pi pi-clipboard', section: 'settings' },
];