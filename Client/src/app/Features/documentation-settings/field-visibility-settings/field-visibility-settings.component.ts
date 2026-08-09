import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { CardModule } from 'primeng/card';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageService } from 'primeng/api';
import { DocumentationField } from '../../../Shared/Models/documentation.model';
import { DocumentationTemplateService } from '../../../Core/Services/documentation-template.service';

interface FieldRow {
  field: DocumentationField;
  visible: boolean;
}

@Component({
  selector: 'app-field-visibility-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, CardModule, CheckboxModule, ButtonModule, SkeletonModule],
  templateUrl: './field-visibility-settings.component.html'
})
export class FieldVisibilitySettingsComponent {
  templateId = input.required<string>();

  private readonly templateService = inject(DocumentationTemplateService);
  private readonly messageService = inject(MessageService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly rows = signal<FieldRow[]>([]);
  readonly skeletonRows = [1, 2, 3, 4, 5];

  readonly hiddenCount = computed(() => this.rows().filter(r => !r.visible).length);

  constructor() {
    effect(() => {
      const id = this.templateId();
      if (id) this.loadFields(id);
    });
  }

  toggle(row: FieldRow): void {
    row.visible = !row.visible;
    this.rows.set([...this.rows()]);
  }

  save(): void {
    this.saving.set(true);
    const hiddenFieldIds = this.rows()
      .filter(r => !r.visible)
      .map(r => r.field.id);

    this.templateService.saveHiddenFields(this.templateId(), hiddenFieldIds).subscribe({
      next: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: 'Saved', detail: 'Field visibility updated.' });
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Save failed', detail: 'Could not update field visibility.' });
      }
    });
  }

  private loadFields(templateId: string): void {
    this.loading.set(true);

    forkJoin({
      all: this.templateService.getAllFields(templateId),
      effective: this.templateService.getEffectiveFields(templateId)
    }).subscribe({
      next: ({ all, effective }) => {
        const effectiveIds = new Set(effective.map(f => f.id));
        this.rows.set(all.map(field => ({ field, visible: effectiveIds.has(field.id) })));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}