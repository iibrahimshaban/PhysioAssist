import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { SlotCandidateDto } from '../../Shared/Models/InitialReport.models';
import { PackageStatus } from './SessionScheduling.model';
import { PatientSchedulingState, PatientSchedulingContextDto } from './SessionScheduling.model';
import { ReceptionistSchedulingService } from './receptionist-scheduling.service';
import { SessionPlanHeaderComponent } from './session-plan-header/session-plan-header.component';
import { PatientFreeTimeEditorComponent } from './patient-free-time-editor/patient-free-time-editor.component';
import { SlotCandidatesGridComponent } from './slot-candidates-grid/slot-candidates-grid.component';
import { PendingPlanSummaryComponent } from './pending-plan-summary/pending-plan-summary.component';
import { Button } from "primeng/button";
import { Router } from '@angular/router';

@Component({
  selector: 'app-receptionist-scheduling',
  imports: [
    SessionPlanHeaderComponent,
    PatientFreeTimeEditorComponent,
    SlotCandidatesGridComponent,
    PendingPlanSummaryComponent,
    Button
],
  templateUrl: './receptionist-scheduling.component.html',
  styleUrl: './receptionist-scheduling.component.css',
})
export class ReceptionistSchedulingComponent {

  private readonly router = inject(Router);

  // Was `packageId` — now bound to the route's actual :patientId param.
  patientId = input.required<string>();

  private readonly schedulingService = inject(ReceptionistSchedulingService);
  protected readonly packageStatusEnum = PackageStatus;
  protected readonly schedulingStateEnum = PatientSchedulingState;

  summary = this.schedulingService.summary;
  round = this.schedulingService.currentRound;

  context = signal<PatientSchedulingContextDto | null>(null);
  isLoadingContext = signal(false);
  isCreatingPackage = signal(false);

  freeTimeText = signal('');
  isConfirming = signal(false);

  isPackageActive = computed(() => this.summary()?.status === PackageStatus.Active);

  constructor() {
    // Reload whenever patientId changes (e.g. navigating between patients).
    effect(() => {
      const id = this.patientId();
      if (id) this.loadContext(id);
    });
  }

  private loadContext(patientId: string): void {
    this.isLoadingContext.set(true);
    this.schedulingService.getSchedulingContext(patientId).subscribe({
      next: ctx => {
        this.context.set(ctx);
        this.isLoadingContext.set(false);

        if (ctx.state === PatientSchedulingState.ActivePackage && ctx.activePackage) {
          this.schedulingService.summary.set(ctx.activePackage);
          this.freeTimeText.set(ctx.activePackage.patientFreeTimeText);
          if (ctx.activePackage.status === PackageStatus.Active) {
            this.schedulingService.loadNextSessionCandidates(ctx.activePackage.packageId);
          }
        }
      },
      error: () => this.isLoadingContext.set(false),
    });
  }

  onCreatePackage(): void {
    const ctx = this.context();
    if (!ctx?.pendingPlan) return;

    this.isCreatingPackage.set(true);
    this.schedulingService.convertPlanToPackage(ctx.pendingPlan.treatmentPlanId, {}).subscribe({
      next: packageSummary => {
        this.isCreatingPackage.set(false);
        this.schedulingService.summary.set(packageSummary);
        this.freeTimeText.set(packageSummary.patientFreeTimeText);
        // Flip straight into the ActivePackage view without re-fetching context.
        this.context.set({ state: PatientSchedulingState.ActivePackage, activePackage: packageSummary });
        this.schedulingService.loadNextSessionCandidates(packageSummary.packageId);
      },
      error: () => this.isCreatingPackage.set(false),
    });
  }

  onRefresh(event: { text: string; persist: boolean }): void {
    const packageId = this.summary()?.packageId;
    if (!packageId) return;
    this.schedulingService.loadNextSessionCandidates(packageId, event.text, event.persist);
  }

  onPickSlot(candidate: SlotCandidateDto): void {
    const packageId = this.summary()?.packageId;
    if (!packageId) return;

    this.isConfirming.set(true);
    this.schedulingService.confirmSlot(packageId, candidate).subscribe({
      next: () => {
        this.isConfirming.set(false);
        // Re-resolve from context again as the source of truth for
        // remaining/status, then decide whether there's a next round to search.
        this.loadContext(this.patientId());
      },
      error: () => this.isConfirming.set(false),
    });
  }

  onManualSchedule(): void {
    const patientId = this.patientId();
    if (!patientId) return;
    this.router.navigate(['/app/schedule'], { queryParams: { patientId } });
  }
    onBack(): void {
    this.router.navigate(['/app/patients']); // or wherever "back" should go
  }
}