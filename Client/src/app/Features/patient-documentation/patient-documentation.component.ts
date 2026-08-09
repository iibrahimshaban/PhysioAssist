import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PatientDocumentationService } from '../../Core/Services/patient-documentation.service';
import { SnackbarService } from '../../Core/Services/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentationSummaryResponse, PatientDocumentationSession, SessionProgressNote, SLOT_STATUS_META, SlotStatus, SummaryAudience, SummaryScope } from '../../Shared/Models/documentation.model';


interface ObjectiveFindingRow {
  label: string;
  value: string;
}

interface ObjectiveFindingField {
  label: string;
  kind: 'value' | 'list';
  value?: string;
  items?: ObjectiveFindingRow[][];
}

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
  readonly SlotStatus = SlotStatus;

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

  // Per-session UI state, keyed by sessionId
  readonly expandedSessionId = signal<string | null>(null);
  readonly progressNotes = signal<Record<string, SessionProgressNote | 'not-found' | 'error'>>({});
  readonly loadingNoteFor = signal<string | null>(null);
  readonly generatingSessionSummaryFor = signal<string | null>(null);

  readonly showScopeOptions = computed(() => this.selectedAudience() === SummaryAudience.Colleague);
  readonly showFocusAreasInput = computed(
    () => this.showScopeOptions() && this.selectedScope() === SummaryScope.Focused
  );

  readonly sessionsWithoutSummary = computed(() => this.sessions().filter((s) => !s.hasSummary).length);

  readonly scopeOptions = [
    { value: SummaryScope.Full, label: 'Full', icon: 'pi-list' },
    { value: SummaryScope.Partial, label: 'Partial', icon: 'pi-minus-circle' },
    { value: SummaryScope.Focused, label: 'Focused', icon: 'pi-bullseye' }
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('patientId');
    if (!id) return;

    this.patientId.set(id);
    this.loadSessions(id);
  }

  toggleAdvanced(): void {
    this.showAdvanced.update((v) => !v);
  }

  toggleExpand(sessionId: string): void {
    if (this.expandedSessionId() === sessionId) {
      this.expandedSessionId.set(null);
      return;
    }

    this.expandedSessionId.set(sessionId);

    // Only fetch once per session per page load
    if (this.progressNotes()[sessionId] !== undefined) return;

    this.loadingNoteFor.set(sessionId);

    this.documentationService.getProgressNote(sessionId).subscribe({
      next: (note) => {
        this.progressNotes.update((m) => ({ ...m, [sessionId]: note }));
        this.loadingNoteFor.set(null);
      },
      error: (err) => {
        const status = err?.status;
        this.progressNotes.update((m) => ({
          ...m,
          [sessionId]: status === 404 ? 'not-found' : 'error'
        }));
        this.loadingNoteFor.set(null);
      }
    });
  }

  generateSessionSummary(sessionId: string): void {
    this.generatingSessionSummaryFor.set(sessionId);

    this.documentationService.generateSessionSummary(sessionId).subscribe({
      next: () => {
        this.generatingSessionSummaryFor.set(null);
        const patientId = this.patientId();
        if (patientId) this.loadSessions(patientId, { silent: true });
      },
      error: () => {
        this.generatingSessionSummaryFor.set(null);
        this.snackbar.error('Generation failed', ['Could not generate a summary for this session. Make sure the progress note is complete.']);
      }
    });
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

  private loadSessions(patientId: string, opts: { silent?: boolean } = {}): void {
    if (!opts.silent) this.loadingSessions.set(true);

    this.documentationService.getSessions(patientId).subscribe({
      next: (sessions) => {
        this.sessions.set(sessions);
        this.loadingSessions.set(false);
      },
      error: () => {
        this.loadingSessions.set(false);
        if (!opts.silent) this.snackbar.error('Failed to load sessions', ['Please try again.']);
      }
    });
  }

  private parseFocusAreas(): string[] {
    return this.focusAreasText()
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
  }

  parseObjectiveFindings(raw: string | null): ObjectiveFindingField[] | null {
    if (!raw) return null;

    try {
      const parsed = JSON.parse(raw);
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return null;

      return Object.entries(parsed).map(([key, val]) => {
        const label = this.humanizeKey(key);

        if (Array.isArray(val)) {
          const items = val.map((item) =>
            item && typeof item === 'object'
              ? Object.entries(item).map(([k, v]) => ({ label: this.humanizeKey(k), value: String(v) }))
              : [{ label: '', value: String(item) }]
          );
          return { label, kind: 'list' as const, items };
        }

        return {
          label,
          kind: 'value' as const,
          value: val === null || val === undefined || val === '' ? 'Not recorded' : String(val)
        };
      });
    } catch {
      return null; // fall back to raw text in the template
    }
  }

  private humanizeKey(key: string): string {
    return key.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}