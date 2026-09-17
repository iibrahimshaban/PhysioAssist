export interface NavItem {
  label: string;
  route: string;
  icon: string;
  permissions?: string[]; // omit = always visible to any authenticated user
  section: 'primary' | 'settings';
}

export const NAV_ITEMS: NavItem[] = [
  {
    label: 'NAVBAR.HOME',
    route: '/app/dashboard',
    icon: 'pi pi-home',
    permissions: ['Dashboard:Read'],
    section: 'primary',
  },
  {
    label: 'NAVBAR.TODAY_SESSIONS',
    route: '/app/today-sessions',
    icon: 'pi pi-clock',
    permissions: ['Session:ViewToday'],
    section: 'primary',
  },
  {
    label: 'NAVBAR.PATIENTS',
    route: '/app/patients',
    icon: 'pi pi-users',
    permissions: ['Patient:Read'],
    section: 'primary',
  },
  {
    label: 'NAVBAR.SCHEDULE',
    route: '/app/schedule',
    icon: 'pi pi-calendar',
    permissions: ['Schedule:Read'],
    section: 'primary',
  },
  {
    label: 'NAVBAR.RECEPTION',
    route: '/app/intake/reception',
    icon: 'pi pi-inbox',
    permissions: ['Submission:Read'],
    section: 'primary',
  },

  {
    label: 'NAVBAR.WORKING_SCHEDULE',
    route: '/app/working-schedule',
    icon: 'pi pi-clock',
    permissions: ['WorkingSchedule:Read'],
    section: 'settings',
  },
  {
    label: 'NAVBAR.SCHEDULE_PREFERENCES',
    route: '/app/schedule-preferences',
    icon: 'pi pi-sliders-h',
    permissions: ['WorkingSchedule:Read'],
    section: 'settings',
  },
  {
    label: 'NAVBAR.SUBMISSIONS',
    route: '/app/intake/submissions',
    icon: 'pi pi-inbox',
    permissions: ['Submission:Read'],
    section: 'settings',
  },
  {
    label: 'NAVBAR.STAFF',
    route: '/app/account/staff',
    icon: 'pi pi-id-card',
    permissions: ['receptionist:read'],
    section: 'settings',
  },
  {
    label: 'NAVBAR.INTAKE_SCHEMAS',
    route: '/app/intake/schemas',
    icon: 'pi pi-file-edit',
    permissions: ['Intake:Read'],
    section: 'settings',
  },
  {
    label: 'NAVBAR.DOCUMENTATION_TEMPLATES',
    route: '/app/Documentation-settings',
    icon: 'pi pi-clipboard',
    section: 'settings',
  },
];
