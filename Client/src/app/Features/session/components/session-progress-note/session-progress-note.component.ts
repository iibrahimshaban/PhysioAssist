import { Component, computed, effect, ElementRef, inject, input, output, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { SessionProgressNoteService } from '../../../../Core/Services/session-progress-note.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { SessionProgressNote } from '../../../../Shared/Models/documentation.model';
import { ConfirmDialog } from "primeng/confirmdialog";
import { ConfirmationService } from 'primeng/api';

interface ObjectiveFindingRow {
  label: string;
  value: string;
}

interface ObjectiveGroupRow {
  label: string;
  columns: string[];
  items: Record<string, string>[];
}

@Component({
  selector: 'app-session-progress-note',
  standalone: true,
  imports: [CommonModule, FormsModule, CardModule, ButtonModule, SkeletonModule, ConfirmDialog],
  providers: [ConfirmationService],
  templateUrl: './session-progress-note.component.html'
})
export class SessionProgressNoteComponent {
  sessionId = input.required<string>();
  isOpen = input.required<boolean>();
  toggle = output<void>();

  onToggle(): void {
    this.toggle.emit();
  }

  @ViewChild('subjectiveEl') private subjectiveEl?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('assessmentEl') private assessmentEl?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('planEl') private planEl?: ElementRef<HTMLTextAreaElement>;

  private readonly noteService = inject(SessionProgressNoteService);
  private readonly snackbar = inject(SnackbarService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly loading = signal(true);
  readonly generating = signal(false);
  readonly saving = signal(false);
  readonly note = signal<SessionProgressNote | null>(null);
  readonly noteExists = signal(false);

  // Editable form state, kept separate from `note()` — AI drafts and doctor edits
  // never silently overwrite each other without an explicit action.
  readonly subjective = signal('');
  readonly assessment = signal('');
  readonly plan = signal('');

  // Parsed once — repeatable_group fields (arrays of objects, e.g. tone/strength/sensation)
  // need different rendering than scalar fields (select/number/text), so we split them here
  // rather than JSON.stringify-ing arrays inline like before.
  private readonly parsedObjectiveFindings = computed<Record<string, unknown>>(() => {
    const raw = this.note()?.objectiveFindings;
    if (!raw) return {};

    try {
      return JSON.parse(raw) as Record<string, unknown>;
    } catch {
      return {};
    }
  });

  readonly objectiveSimpleRows = computed<ObjectiveFindingRow[]>(() =>
    Object.entries(this.parsedObjectiveFindings())
      .filter(([, value]) => !Array.isArray(value))
      .map(([key, value]) => ({ label: this.humanize(key), value: String(value) }))
  );

  readonly objectiveGroupRows = computed<ObjectiveGroupRow[]>(() =>
    Object.entries(this.parsedObjectiveFindings())
      .filter((entry): entry is [string, unknown[]] => Array.isArray(entry[1]))
      .map(([key, items]) => {
        const rows = items.map((item) => this.formatGroupItemAsRow(item));
        const columns = rows.length ? Object.keys(rows[0]) : [];
        return { label: this.humanize(key), columns, items: rows };
      })
  );

  private formatGroupItemAsRow(item: unknown): Record<string, string> {
    if (typeof item !== 'object' || item === null) {
      return { Value: String(item) };
    }

    return Object.fromEntries(
      Object.entries(item as Record<string, unknown>).map(
        ([key, value]) => [this.humanize(key), String(value)]
      )
    );
  }

  constructor() {
    effect(() => {
      const id = this.sessionId();
      if (id) this.loadNote(id);
    });
  }

  // Textareas grow to fit content instead of scrolling internally — called on (input)
  // for live typing, and manually after any programmatic fill (AI draft, loaded note).
  autoGrow(event: Event): void {
    this.resize((event.target as HTMLTextAreaElement) ?? undefined);
  }

  private resize(el?: HTMLTextAreaElement): void {
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = `${el.scrollHeight}px`;
  }

  private resizeAllNextTick(): void {
    setTimeout(() => {
      this.resize(this.subjectiveEl?.nativeElement);
      this.resize(this.assessmentEl?.nativeElement);
      this.resize(this.planEl?.nativeElement);
    });
  }

  generateAiSummary(): void {
    if (this.noteExists()) {
      this.confirmationService.confirm({
        header: 'Regenerate AI Summary?',
        message: 'This re-runs AI extraction and refills the draft fields below. Any unsaved edits will be overwritten. Continue?',
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: 'Continue',
        rejectLabel: 'Cancel',
        accept: () => this.runGenerateAiSummary()
      });
      return;
    }

    this.runGenerateAiSummary();
  }

  private runGenerateAiSummary(): void {
    this.generating.set(true);

    this.noteService.generateAiSummary(this.sessionId()).subscribe({
      next: (response) => {
        this.note.set(response.progressNote);
        this.noteExists.set(true);
        this.subjective.set(response.narrativeDraft?.subjective ?? '');
        this.assessment.set(response.narrativeDraft?.assessment ?? '');
        this.plan.set(response.narrativeDraft?.plan ?? '');
        this.generating.set(false);
        this.resizeAllNextTick();

        if (!response.narrativeDraft) {
          this.snackbar.warning('Objective findings saved', [
            'The narrative draft failed to generate — retry it below, or write S/A/P manually.'
          ]);
        }
      },
      error: () => {
        this.generating.set(false);
        this.snackbar.error('Generation failed', [
          'Could not generate the AI summary. Make sure this session has a finalized transcript.'
        ]);
      }
    });
  }

  retryNarrativeDraft(): void {
    this.generating.set(true);

    this.noteService.generateNarrativeDraft(this.sessionId()).subscribe({
      next: (draft) => {
        this.subjective.set(draft.subjective);
        this.assessment.set(draft.assessment);
        this.plan.set(draft.plan);
        this.generating.set(false);
        this.resizeAllNextTick();
      },
      error: () => {
        this.generating.set(false);
        this.snackbar.error('Retry failed', ['Could not generate the narrative draft.']);
      }
    });
  }

  submit(): void {
    this.saving.set(true);

    this.noteService
      .updateNarrative(this.sessionId(), {
        subjective: this.subjective(),
        assessment: this.assessment(),
        plan: this.plan()
      })
      .subscribe({
        next: (updated) => {
          this.note.set(updated);
          this.saving.set(false);
          this.snackbar.success('Progress note submitted');
        },
        error: () => {
          this.saving.set(false);
          this.snackbar.error('Save failed', ['Please try again.']);
        }
      });
  }

  private loadNote(sessionId: string): void {
    this.loading.set(true);

    this.noteService.get(sessionId).subscribe({
      next: (note) => {
        this.note.set(note);
        this.noteExists.set(true);
        this.subjective.set(note.subjective);
        this.assessment.set(note.assessment);
        this.plan.set(note.plan);
        this.loading.set(false);
        this.resizeAllNextTick();
      },
      error: () => {
        // 404 is expected when no note exists yet — not a real error for this screen.
        this.note.set(null);
        this.noteExists.set(false);
        this.loading.set(false);
      }
    });
  }

  private humanize(key: string): string {
    return key.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}