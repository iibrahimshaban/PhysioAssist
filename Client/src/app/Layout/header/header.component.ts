import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Core/Services/auth.service';
import { NavigationService } from '../../Core/Services/navigation.service';

@Component({
  selector: 'app-header',
  imports: [ RouterLink,RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  auth = inject(AuthService);
  nav = inject(NavigationService);

  menuOpen = signal(false);
 
  toggleMenu(): void { this.menuOpen.update(v => !v); }

  accountMenuOpen = signal(false);

  homeRoute = computed(() => (this.auth.isDoctor() ? '/app/dashboard' : '/'));

  mobileAccountMenuOpen = signal(false);

  toggleMobileAccountMenu() {
    this.mobileAccountMenuOpen.update(v => !v);
  }

  closeMenu() {
    this.menuOpen.set(false);
    this.mobileAccountMenuOpen.set(false); // reset so it's collapsed next time it opens
  }

  toggleAccountMenu(): void {
    this.accountMenuOpen.update(v => !v);
  }

  closeAccountMenu(): void {
    this.accountMenuOpen.set(false);
  }
}
