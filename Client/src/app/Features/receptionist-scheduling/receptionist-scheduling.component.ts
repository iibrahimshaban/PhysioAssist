import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { SlotCandidateDto } from '../../Shared/Models/InitialReport.models';
import { PackageStatus } from './SessionScheduling.model';
import { PatientSchedulingState, PatientSchedulingContextDto } from './SessionScheduling.model';
import { ReceptionistSchedulingService } from './receptionist-scheduling.service';
import { SessionPlanHeaderComponent } from './session-plan-header/session-plan-header.component';
import { PatientFreeTimeEditorComponent } from './patient-free-time-editor/patient-free-time-editor.component';
import { SlotCandidatesGridComponent } from './slot-candidates-grid/slot-candidates-grid.component';
import { PendingPlanSummaryComponent } from './pending-plan-summary/pending-plan-summary.component';
import { Button } from 'primeng/button';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { ConfirmationService } from 'primeng/api';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-receptionist-scheduling',
  imports: [
    SessionPlanHeaderComponent,
    PatientFreeTimeEditorComponent,
    SlotCandidatesGridComponent,
    PendingPlanSummaryComponent,
    Button,
    ConfirmDialog,
    Dialog,
    InputNumber,
    FormsModule,
    TranslatePipe,
  ],
  templateUrl: './receptionist-scheduling.component.html',
  styleUrl: './receptionist-scheduling.component.css',
})
export class ReceptionistSchedulingComponent {
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);

  patientId = input.required<string>();

  protected readonly schedulingService = inject(ReceptionistSchedulingService);
  protected readonly packageStatusEnum = PackageStatus;
  protected readonly schedulingStateEnum = PatientSchedulingState;

  summary = this.schedulingService.summary;
  round = this.schedulingService.currentRound;

  context = signal<PatientSchedulingContextDto | null>(null);
  isLoadingContext = signal(false);
  isCreatingPackage = signal(false);

  freeTimeText = signal('');
  isConfirming = signal(false);

  showExtendDialog = signal(false);
  additionalSessions = signal<number | null>(null);
  isExtending = signal(false);
  isStopping = signal(false);

  isPackageActive = computed(() => this.summary()?.status === PackageStatus.Active);

  constructor() {
    effect(() => {
      const id = this.patientId();
      if (id) this.loadContext(id);
    });
  }

  private loadContext(patientId: string): void {
    this.isLoadingContext.set(true);
    this.schedulingService.getSchedulingContext(patientId).subscribe({
      next: (ctx) => {
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
      next: (packageSummary) => {
        this.isCreatingPackage.set(false);
        this.schedulingService.summary.set(packageSummary);
        this.freeTimeText.set(packageSummary.patientFreeTimeText);
        this.context.set({
          state: PatientSchedulingState.ActivePackage,
          activePackage: packageSummary,
        });
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
    this.router.navigate(['/app/patients']);
  }

  // --- Extend package ---

  openExtendDialog(): void {
    this.additionalSessions.set(null);
    this.showExtendDialog.set(true);
  }

  confirmExtend(): void {
    const packageId = this.summary()?.packageId;
    const sessions = this.additionalSessions();
    if (!packageId || !sessions || sessions <= 0) return;

    this.isExtending.set(true);
    this.schedulingService.extendPackage(packageId, { additionalSessions: sessions }).subscribe({
      next: () => {
        this.isExtending.set(false);
        this.showExtendDialog.set(false);
        this.loadContext(this.patientId());
      },
      error: () => this.isExtending.set(false),
    });
  }

  // --- Stop package ---

  confirmStopPackage(): void {
    const packageId = this.summary()?.packageId;
    if (!packageId) return;

    this.confirmationService.confirm({
      header: 'Stop package',
      message: 'Are you sure you want to stop this package? This cannot be undone.',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Stop package', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', outlined: true },
      accept: () => this.stopPackage(packageId),
    });
  }

  private stopPackage(packageId: string): void {
    this.isStopping.set(true);
    this.schedulingService.stopPackage(packageId, {}).subscribe({
      next: () => {
        this.isStopping.set(false);
        this.loadContext(this.patientId());
      },
      error: () => this.isStopping.set(false),
    });
  }
}
