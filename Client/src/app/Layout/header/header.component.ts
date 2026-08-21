import { Component, computed, inject, signal, HostListener, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '../../Core/Services/auth.service';
import { NavigationService } from '../../Core/Services/navigation.service';

interface MarketingNavItem {
  label: string;
  labelKey: string;
  fragment: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, TranslocoModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  auth = inject(AuthService);
  nav = inject(NavigationService);
  private platformId = inject(PLATFORM_ID);

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

  // ── Marketing nav (shown to logged-out visitors, e.g. on the landing page) ──
  readonly marketingNavItems: MarketingNavItem[] = [
    { label: 'Features', labelKey: 'shared.nav.marketingFeatures', fragment: 'features' },
    { label: 'AI Assistant', labelKey: 'shared.nav.marketingAiAssistant', fragment: 'ai-assistant' },
    { label: 'Scheduling', labelKey: 'shared.nav.marketingScheduling', fragment: 'scheduling' },
    { label: 'For Physiotherapists', labelKey: 'shared.nav.marketingForPhysios', fragment: 'for-physios' },
  ];

  // ── Sticky/blurred navbar on scroll ──
  scrolled = signal(false);

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.scrolled.set(window.scrollY > 12);
  }
}