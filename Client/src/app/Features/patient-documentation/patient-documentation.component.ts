import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PatientDocumentationService } from '../../Core/Services/patient-documentation.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentationSummaryResponse, PatientDocumentationSession, SLOT_STATUS_META, SummaryAudience, SummaryScope } from '../../Shared/Models/documentation.model';

@Component({
  selector: 'app-patient-documentation',
  imports: [CommonModule, FormsModule, ButtonModule],
  templateUrl: './patient-documentation.component.html',
  styleUrl: './patient-documentation.component.css',
})
export class PatientDocumentationComponent implements OnInit {
  private readonly documentationService = inject(PatientDocumentationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
 
  readonly SummaryAudience = SummaryAudience;
  readonly SummaryScope = SummaryScope;
  readonly slotStatusMeta = SLOT_STATUS_META;
 
  readonly patientId = signal<string | null>(null);
  readonly loadingSessions = signal(true);
  readonly sessions = signal<PatientDocumentationSession[]>([]);
 
  readonly showAdvanced = signal(false);
  readonly selectedAudience = signal<SummaryAudience>(SummaryAudience.Colleague);
  readonly selectedScope = signal<SummaryScope>(SummaryScope.Full);
  readonly focusAreasText = signal('');
 
  readonly generatingSummary = signal(false);
  readonly generatingPdf = signal(false);
  readonly summaryResult = signal<DocumentationSummaryResponse | null>(null);
 
  readonly showScopeOptions = computed(() => this.selectedAudience() === SummaryAudience.Colleague);
  readonly showFocusAreasInput = computed(
    () => this.showScopeOptions() && this.selectedScope() === SummaryScope.Focused
  );
 
  readonly sessionsWithoutSummary = computed(() => this.sessions().filter((s) => !s.hasSummary).length);
 
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('patientId');
    if (!id) return;
 
    this.patientId.set(id);
    this.loadSessions(id);
  }
 
  toggleAdvanced(): void {
    this.showAdvanced.update((v) => !v);
  }
 
  generateSummary(): void {
    const patientId = this.patientId();
    if (!patientId) return;
 
    this.generatingSummary.set(true);
    this.summaryResult.set(null);
 
    this.documentationService
      .generateSummary({
        patientId,
        audience: this.selectedAudience(),
        scope: this.showScopeOptions() ? this.selectedScope() : undefined,
        focusAreas: this.showFocusAreasInput() ? this.parseFocusAreas() : undefined
      })
      .subscribe({
        next: (result) => {
          this.summaryResult.set(result);
          this.generatingSummary.set(false);
        },
        error: () => {
          this.generatingSummary.set(false);
          this.snackbar.error('Generation failed', ['Could not generate the documentation summary.']);
        }
      });
  }
 
  generatePdf(): void {
    const summary = this.summaryResult();
    if (!summary) return;
 
    this.generatingPdf.set(true);
 
    this.documentationService.generatePdf(summary.id).subscribe({
      next: (result) => {
        this.summaryResult.set(result);
        this.generatingPdf.set(false);
      },
      error: () => {
        this.generatingPdf.set(false);
        this.snackbar.error('PDF generation failed', ['Could not generate the PDF.']);
      }
    });
  }
 
  goBack(): void {
    const id = this.patientId();
    if (!id) return;
    this.router.navigate(['/app/patients', id, 'overview']);
  }
 
  private loadSessions(patientId: string): void {
    this.loadingSessions.set(true);
 
    this.documentationService.getSessions(patientId).subscribe({
      next: (sessions) => {
        this.sessions.set(sessions);
        this.loadingSessions.set(false);
      },
      error: () => {
        this.loadingSessions.set(false);
        this.snackbar.error('Failed to load sessions', ['Please try again.']);
      }
    });
  }
 
  private parseFocusAreas(): string[] {
    return this.focusAreasText()
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
  }
}
