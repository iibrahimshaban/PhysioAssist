import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DashboardService } from '../../Core/Services/dashboard.service';
import { Router } from '@angular/router';
import { DoctorDashboardSummary } from '../../Shared/Models/dashboard.model';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-dashboard',
  imports: [ButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly router = inject(Router);

  summary = signal<DoctorDashboardSummary | null>(null);
  isLoading = signal(true);

  hasActivityToday = computed(() => {
    const s = this.summary();
    if (!s) return false;
    return s.pendingIntakesCount > 0 || s.upcomingSessionsTodayCount > 0;
  });

  ngOnInit(): void {
    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  goToSessions(): void {
    this.router.navigateByUrl('/app/today-sessions');
  }

  goToSchedule(): void {
    this.router.navigateByUrl('/app/schedule');
  }

  goToPatients(): void {
    this.router.navigateByUrl('/app/patients');
  }

  goToSubmissions(): void {
    this.router.navigateByUrl('/app/intake/submissions');
  }

  reviewSubmission(submissionId: string): void {
    this.router.navigate(['/app/intake/submissions', submissionId]);
  }

  timeAgo(isoDate: string): string {
    const minutes = Math.floor((Date.now() - new Date(isoDate).getTime()) / 60000);
    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} hr ago`;
    return `${Math.floor(hours / 24)} day ago`;
  }
}
