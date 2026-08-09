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

interface FeatureCard {
  icon: string;
  title: string;
  description: string;
}

interface JourneyStep {
  label: string;
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
      title: 'Patient Management',
      description:
        'Keep patient profiles, history, sessions and clinical information organized in one place.',
    },
    {
      icon: 'pi-microphone',
      title: 'Voice Transcription',
      description:
        'Dictate session notes in Egyptian Arabic or English and turn your voice into structured documentation.',
    },
    {
      icon: 'pi-sparkles',
      title: 'AI Documentation',
      description:
        'Use patient history and session information to assist with reports, treatment plans and summaries.',
    },
    {
      icon: 'pi-calendar',
      title: 'Smart Scheduling',
      description:
        'Manage doctor availability, appointments, rescheduling and free time from one intelligent calendar.',
    },
    {
      icon: 'pi-heart',
      title: 'Treatment Sessions',
      description:
        "Keep treatment sessions organized and connect every session to the patient's treatment journey.",
    },
    {
      icon: 'pi-bell',
      title: 'Notifications',
      description:
        'Keep patients informed about appointments, reminders, cancellations and schedule changes.',
    },
  ];

  readonly journey: JourneyStep[] = [
    { label: 'First Visit', icon: 'pi-user-plus' },
    { label: 'Patient Intake', icon: 'pi-file-edit' },
    { label: 'Examination', icon: 'pi-search' },
    { label: 'Treatment Plan', icon: 'pi-clipboard' },
    { label: 'Smart Scheduling', icon: 'pi-calendar' },
    { label: 'Treatment Sessions', icon: 'pi-heart' },
    { label: 'AI Documentation', icon: 'pi-sparkles' },
    { label: 'Patient History', icon: 'pi-history' },
  ];

  private observer?: IntersectionObserver;

  @ViewChildren('revealEl')
  private revealEls?: QueryList<ElementRef<HTMLElement>>;

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.prefersReducedMotion.set(
        window.matchMedia('(prefers-reduced-motion: reduce)').matches
      );

      this.fragmentSub = this.route.fragment.subscribe((fragment) => {
        if (!fragment) return;
        // Wait two frames so the section (and any *ngIf/*ngFor content above it)
        // has actually painted before we measure and scroll to it.
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
      { threshold: 0.2 }
    );

    this.revealEls.forEach((ref) => this.observer?.observe(ref.nativeElement));

    // Elements added after the initial view init (e.g. inside *ngIf) fall
    // back to visible via the .reveal CSS default — no-JS-needed safety net.
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