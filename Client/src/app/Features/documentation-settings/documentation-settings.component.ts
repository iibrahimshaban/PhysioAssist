import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { InputTextModule } from 'primeng/inputtext';
import { DocumentationTemplateService } from '../../Core/Services/documentation-template.service';
import {
  DocumentationField,
  DocumentationTemplateSummary,
  PatientCategory,
  PATIENT_CATEGORY_LABELS,
} from '../../Shared/Models/documentation.model';
import { SnackbarService } from '../../Core/Services/snackbar.service';

export interface FieldRow {
  field: DocumentationField;
  visible: boolean;
  required: boolean;
}

const CATEGORY_ICONS: Record<PatientCategory, string> = {
  [PatientCategory.Orthopedic]: 'pi-sitemap',
  [PatientCategory.Neurological]: 'pi-bolt',
  [PatientCategory.Pediatric]: 'pi-heart',
  [PatientCategory.GeneralOther]: 'pi-file',
};

const CATEGORY_ICON_STYLE: Record<PatientCategory, string> = {
  [PatientCategory.Orthopedic]: 'icon-bg-blue',
  [PatientCategory.Neurological]: 'icon-bg-teal',
  [PatientCategory.Pediatric]: 'icon-bg-amber',
  [PatientCategory.GeneralOther]: 'icon-bg-neutral',
};

@Component({
  selector: 'app-documentation-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    ButtonModule,
    SkeletonModule,
    ToggleSwitchModule,
    InputTextModule,
  ],
  templateUrl: './documentation-settings.component.html',
})
export class DocumentationSettingsComponent {
  private readonly templateService = inject(DocumentationTemplateService);
  private readonly snackbar = inject(SnackbarService);

  readonly categoryLabels = PATIENT_CATEGORY_LABELS;
  readonly skeletonRows = [1, 2, 3, 4, 5];

  readonly templates = signal<DocumentationTemplateSummary[]>([]);
  readonly loadingTemplates = signal(true);
  readonly loadingFields = signal(true);
  readonly activeTemplateId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly searchTerm = signal('');

  /** Last-saved state per template id — the source of truth for dirty checks and Reset. */
  private savedRows: Record<string, FieldRow[]> = {};

  /** Editable working copy per template id. */
  readonly rowsByTemplate = signal<Record<string, FieldRow[]>>({});

  readonly activeCategory = computed<PatientCategory | undefined>(
    () => this.templates().find((t) => t.id === this.activeTemplateId())?.category
  );

  readonly activeRows = computed(() => this.rowsByTemplate()[this.activeTemplateId() ?? ''] ?? []);

  readonly filteredRows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const rows = this.activeRows();
    if (!term) return rows;
    return rows.filter(
      (r) =>
        r.field.label?.toLowerCase().includes(term) ||
        this.fieldHelpText(r.field)?.toLowerCase().includes(term) ||
        r.field.id.toLowerCase().includes(term)
    );
  });

  readonly isDirty = computed(() => Object.keys(this.rowsByTemplate()).some((id) => this.hasChanges(id)));

  constructor() {
    this.templateService.getTemplates().subscribe({
      next: (templates) => {
        this.templates.set(templates);
        this.loadingTemplates.set(false);
        if (templates.length) {
          this.activeTemplateId.set(templates[0].id);
          this.preloadAll(templates);
        } else {
          this.loadingFields.set(false);
        }
      },
      error: () => {
        this.loadingTemplates.set(false);
        this.loadingFields.set(false);
      },
    });
  }

  categoryIcon(category: PatientCategory | undefined): string {
    return category !== undefined ? CATEGORY_ICONS[category] ?? 'pi-file' : 'pi-file';
  }

  iconStyle(category: PatientCategory | undefined): string {
    return category !== undefined ? CATEGORY_ICON_STYLE[category] ?? 'icon-bg-blue' : 'icon-bg-blue';
  }

  categoryLabel(category: PatientCategory | undefined): string {
    return category !== undefined ? PATIENT_CATEGORY_LABELS[category] : '';
  }

  /** DocumentationField is schema-driven ([key: string]: unknown), so read optional props defensively. */
  fieldHelpText(field: DocumentationField): string | undefined {
    const value = field['helpText'];
    return typeof value === 'string' ? value : undefined;
  }

  isFieldRequired(field: DocumentationField): boolean {
    const value = field['required'];
    return typeof value === 'boolean' ? value : false;
  }

  visibleCount(templateId: string): number {
    return (this.rowsByTemplate()[templateId] ?? []).filter((r) => r.visible).length;
  }

  totalCount(templateId: string): number {
    return (this.rowsByTemplate()[templateId] ?? []).length;
  }

  selectTemplate(templateId: string): void {
    this.activeTemplateId.set(templateId);
    this.searchTerm.set('');
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
  }

  toggle(row: FieldRow): void {
    if (row.required) return;
    const templateId = this.activeTemplateId();
    if (!templateId) return;

    const rows = (this.rowsByTemplate()[templateId] ?? []).map((r) =>
      r.field.id === row.field.id ? { ...r, visible: !r.visible } : r
    );
    this.rowsByTemplate.set({ ...this.rowsByTemplate(), [templateId]: rows });
  }

  reset(): void {
    const restored: Record<string, FieldRow[]> = {};
    for (const id of Object.keys(this.savedRows)) {
      restored[id] = this.savedRows[id].map((r) => ({ ...r }));
    }
    this.rowsByTemplate.set(restored);
    this.snackbar.info('Changes reverted', ['Your edits were discarded.']);
  }

  save(): void {
    const dirtyIds = Object.keys(this.rowsByTemplate()).filter((id) => this.hasChanges(id));
    if (!dirtyIds.length) return;

    this.saving.set(true);
    const requests: Record<string, Observable<unknown>> = {};
    for (const id of dirtyIds) {
      const hiddenFieldIds = (this.rowsByTemplate()[id] ?? []).filter((r) => !r.visible).map((r) => r.field.id);
      requests[id] = this.templateService.saveHiddenFields(id, hiddenFieldIds);
    }

    forkJoin(requests).subscribe({
      next: () => {
        this.saving.set(false);
        for (const id of dirtyIds) {
          this.savedRows[id] = (this.rowsByTemplate()[id] ?? []).map((r) => ({ ...r }));
        }
        this.snackbar.success('Documentation settings saved', ['Your changes are now live in the report editor.']);
      },
      error: () => {
        this.saving.set(false);
        this.snackbar.error('Save failed', ['Could not save documentation settings. Please try again.']);
      },
    });
  }

  private hasChanges(templateId: string): boolean {
    const saved = this.savedRows[templateId] ?? [];
    const working = this.rowsByTemplate()[templateId] ?? [];
    if (saved.length !== working.length) return false;
    return saved.some((r, i) => r.visible !== working[i]?.visible);
  }

  private preloadAll(templates: DocumentationTemplateSummary[]): void {
    this.loadingFields.set(true);

    const requests: Record<string, Observable<{ all: DocumentationField[]; effective: DocumentationField[] }>> = {};
    for (const t of templates) {
      requests[t.id] = forkJoin({
        all: this.templateService.getAllFields(t.id),
        effective: this.templateService.getEffectiveFields(t.id),
      });
    }

    forkJoin(requests).subscribe({
      next: (result) => {
        const rows: Record<string, FieldRow[]> = {};
        for (const id of Object.keys(result)) {
          const { all, effective } = result[id];
          const effectiveIds = new Set(effective.map((f) => f.id));
          rows[id] = all.map((field) => ({
            field,
            visible: effectiveIds.has(field.id),
            required: this.isFieldRequired(field),
          }));
        }
        this.savedRows = rows;
        this.rowsByTemplate.set(
          Object.fromEntries(Object.entries(rows).map(([id, list]) => [id, list.map((r) => ({ ...r }))]))
        );
        this.loadingFields.set(false);
      },
      error: () => this.loadingFields.set(false),
    });
  }
}