import { computed, inject, Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { NAV_ITEMS, NavItem } from '../../Shared/Models/NavItem';

@Injectable({
  providedIn: 'root',
})
export class NavigationService {
  private authService = inject(AuthService);

  private hasAccess = (item: NavItem): boolean => {
    if (!item.permissions?.length) return true;
    return item.permissions.some((p) => this.authService.hasPermission(p));
  };

  primaryItems = computed(() =>
    NAV_ITEMS.filter((item) => item.section === 'primary' && this.hasAccess(item)),
  );

  settingsItems = computed(() =>
    NAV_ITEMS.filter((item) => item.section === 'settings' && this.hasAccess(item)),
  );
}
