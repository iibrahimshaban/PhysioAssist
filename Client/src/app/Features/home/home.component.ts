import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  AfterViewInit,
  QueryList,
  ViewChildren,
  inject,
  signal,
  WritableSignal,
  PLATFORM_ID,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { HeaderComponent } from '../../Layout/header/header.component';
import { AskAsiPanelComponent } from '../../Shared/Components/ask-asi-panel/ask-asi-panel.component';
import { AskAsiButtonComponent } from '../../Shared/Components/ask-asi-button/ask-asi-button.component';
import { AuthService } from '../../Core/Services/auth.service';
import { HasPermissionDirective } from '../../Shared/Directives/has-permission-directive';
import { TranslatePipe } from '@ngx-translate/core';

interface FeatureCard {
  icon: string;
  titleKey: string;
  descKey: string;
}

interface JourneyStep {
  labelKey: string;
  icon: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    HeaderComponent,
    AskAsiPanelComponent,
    AskAsiButtonComponent,
    HasPermissionDirective,
    TranslatePipe,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  auth = inject(AuthService);
  private platformId = inject(PLATFORM_ID);
  private route = inject(ActivatedRoute);
  private fragmentSub?: Subscription;

  // Accessibility
  prefersReducedMotion = signal(false);

  // Back-to-top button
  showBackToTop = signal(false);

  // Clinic overview stats (static demo data, animated on view)
  patientsCount: WritableSignal<number> = signal(0);
  appointmentsCount: WritableSignal<number> = signal(0);
  completedCount: WritableSignal<number> = signal(0);
  upcomingCount: WritableSignal<number> = signal(0);
  private statsAnimated = false;

  // Smart scheduling "best match" reveal
  matchRevealed = signal(false);

  readonly features: FeatureCard[] = [
    {
      icon: 'pi-users',
      titleKey: 'LANDING.FEATURES.PATIENT_MANAGEMENT.TITLE',
      descKey: 'LANDING.FEATURES.PATIENT_MANAGEMENT.DESC',
    },
    {
      icon: 'pi-microphone',
      titleKey: 'LANDING.FEATURES.VOICE_TRANSCRIPTION.TITLE',
      descKey: 'LANDING.FEATURES.VOICE_TRANSCRIPTION.DESC',
    },
    {
      icon: 'pi-sparkles',
      titleKey: 'LANDING.FEATURES.AI_DOCUMENTATION.TITLE',
      descKey: 'LANDING.FEATURES.AI_DOCUMENTATION.DESC',
    },
    {
      icon: 'pi-calendar',
      titleKey: 'LANDING.FEATURES.SMART_SCHEDULING.TITLE',
      descKey: 'LANDING.FEATURES.SMART_SCHEDULING.DESC',
    },
    {
      icon: 'pi-heart',
      titleKey: 'LANDING.FEATURES.TREATMENT_SESSIONS.TITLE',
      descKey: 'LANDING.FEATURES.TREATMENT_SESSIONS.DESC',
    },
    {
      icon: 'pi-bell',
      titleKey: 'LANDING.FEATURES.NOTIFICATIONS.TITLE',
      descKey: 'LANDING.FEATURES.NOTIFICATIONS.DESC',
    },
  ];

  readonly journey: JourneyStep[] = [
    { labelKey: 'LANDING.JOURNEY.STEPS.FIRST_VISIT', icon: 'pi-user-plus' },
    { labelKey: 'LANDING.JOURNEY.STEPS.PATIENT_INTAKE', icon: 'pi-file-edit' },
    { labelKey: 'LANDING.JOURNEY.STEPS.EXAMINATION', icon: 'pi-search' },
    { labelKey: 'LANDING.JOURNEY.STEPS.TREATMENT_PLAN', icon: 'pi-clipboard' },
    { labelKey: 'LANDING.JOURNEY.STEPS.SMART_SCHEDULING', icon: 'pi-calendar' },
    { labelKey: 'LANDING.JOURNEY.STEPS.TREATMENT_SESSIONS', icon: 'pi-heart' },
    { labelKey: 'LANDING.JOURNEY.STEPS.AI_DOCUMENTATION', icon: 'pi-sparkles' },
    { labelKey: 'LANDING.JOURNEY.STEPS.PATIENT_HISTORY', icon: 'pi-history' },
  ];

  private observer?: IntersectionObserver;

  @ViewChildren('revealEl')
  private revealEls?: QueryList<ElementRef<HTMLElement>>;

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.prefersReducedMotion.set(window.matchMedia('(prefers-reduced-motion: reduce)').matches);

      this.fragmentSub = this.route.fragment.subscribe((fragment) => {
        if (!fragment) return;
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            document.getElementById(fragment)?.scrollIntoView({
              behavior: this.prefersReducedMotion() ? 'auto' : 'smooth',
              block: 'start',
            });
          });
        });
      });
    }
  }

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId) || !this.revealEls) return;

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const el = entry.target as HTMLElement;
          el.classList.add('is-visible');

          if (el.hasAttribute('data-stats')) this.animateStats();
          if (el.hasAttribute('data-match')) this.matchRevealed.set(true);

          this.observer?.unobserve(el);
        }
      },
      { threshold: 0.2 },
    );

    this.revealEls.forEach((ref) => this.observer?.observe(ref.nativeElement));
  }

  private animateStats(): void {
    if (this.statsAnimated) return;
    this.statsAnimated = true;
    this.animateValue(this.patientsCount, 24);
    this.animateValue(this.appointmentsCount, 18);
    this.animateValue(this.completedCount, 12);
    this.animateValue(this.upcomingCount, 6);
  }

  private animateValue(target: WritableSignal<number>, end: number): void {
    if (this.prefersReducedMotion()) {
      target.set(end);
      return;
    }
    const duration = 900;
    const start = performance.now();
    const step = (now: number) => {
      const progress = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      target.set(Math.round(end * eased));
      if (progress < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.showBackToTop.set(window.scrollY > 480);
  }

  scrollToTop(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    window.scrollTo({
      top: 0,
      behavior: this.prefersReducedMotion() ? 'auto' : 'smooth',
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.fragmentSub?.unsubscribe();
  }
}
