import { Component, computed, inject, signal, HostListener, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../Core/Services/auth.service';
import { NavigationService } from '../../Core/Services/navigation.service';
import { MyTranslateService } from '../../Core/Services/my-translate.service';
import { TranslatePipe } from '@ngx-translate/core';

interface MarketingNavItem {
  label: string;
  fragment: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  auth = inject(AuthService);
  nav = inject(NavigationService);
  private platformId = inject(PLATFORM_ID);

  menuOpen = signal(false);

  toggleMenu(): void {
    this.menuOpen.update((v) => !v);
  }

  accountMenuOpen = signal(false);

  homeRoute = computed(() => (this.auth.isDoctor() ? '/app/dashboard' : '/'));

  mobileAccountMenuOpen = signal(false);

  toggleMobileAccountMenu() {
    this.mobileAccountMenuOpen.update((v) => !v);
  }

  closeMenu() {
    this.menuOpen.set(false);
    this.mobileAccountMenuOpen.set(false); // reset so it's collapsed next time it opens
  }

  toggleAccountMenu(): void {
    this.accountMenuOpen.update((v) => !v);
  }

  closeAccountMenu(): void {
    this.accountMenuOpen.set(false);
  }

  // ── Marketing nav (shown to logged-out visitors, e.g. on the landing page) ──
  marketingNavItems = [
    {
      label: 'NAVBAR.FOR_PHYSIOTHERAPISTS',
      fragment: 'for-physiotherapists',
    },
    {
      label: 'NAVBAR.SCHEDULING',
      fragment: 'scheduling',
    },
    {
      label: 'NAVBAR.AI_ASSISTANT',
      fragment: 'ai-assistant',
    },
    {
      label: 'NAVBAR.FEATURES',
      fragment: 'features',
    },
  ];

  // ── Sticky/blurred navbar on scroll ──
  scrolled = signal(false);

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.scrolled.set(window.scrollY > 12);
  }

  // translate
  private myTranslateService = inject(MyTranslateService);

  currentLang = signal(localStorage.getItem('lang') ?? 'en');

  changeLanguage(): void {
    const newLang = this.currentLang() === 'en' ? 'ar' : 'en';

    this.myTranslateService.changeLang(newLang);

    this.currentLang.set(newLang);
  }
}
