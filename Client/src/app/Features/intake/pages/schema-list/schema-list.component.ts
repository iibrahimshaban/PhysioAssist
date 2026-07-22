import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { QrAccessService } from '../../services/qr-access.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { FormSchemaSummaryResponse, FormSchemaStatus, FormSchemaResponse, CreateFormSchemaRequest, GenerateIntakeQrLinkResponse } from '../../models';

type TabFilter = 'all' | 'published' | 'draft' | 'archived';
type SortField = 'name' | 'updated' | 'submissions';

@Component({
  selector: 'app-schema-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    DialogModule,
    ConfirmDialogModule,
    TooltipModule
  ],
  providers: [ConfirmationService],
  templateUrl: './schema-list.component.html',
  styleUrl: './schema-list.component.css'
})
export class SchemaListComponent implements OnInit {
  private readonly apiService = inject(IntakeApiService);
  protected readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly qrAccessService = inject(QrAccessService);
  private readonly confirmationService = inject(ConfirmationService);


  schemas = signal<FormSchemaSummaryResponse[]>([]);
  filteredSchemas = signal<FormSchemaSummaryResponse[]>([]);
  loading = signal(false);
  loadError = signal<string | null>(null);
  searchTerm = '';

  // Default form
  defaultForm = signal<FormSchemaResponse | null>(null);
  defaultFormLoading = signal(false);

  readonly FormSchemaStatus = FormSchemaStatus;
  protected readonly Math = Math;

  // Tabs & Sort
  activeTab = signal<TabFilter>('all');
  sortBy = signal<SortField>('name');

  readonly sortOptions = [
    { value: 'name' as SortField, label: 'Name' },
    { value: 'updated' as SortField, label: 'Last Updated' },
    { value: 'submissions' as SortField, label: 'Submissions' }
  ];

  // Pagination
  currentPage = signal(1);
  pageSize = 8;

  paginatedSchemas = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredSchemas().slice(start, start + this.pageSize);
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredSchemas().length / this.pageSize)));
  pages = computed(() => Array.from({ length: this.totalPages() }, (_, i) => i + 1));

  defaultFormSummary = computed(() => {
    const defaultId = this.defaultForm()?.id;
    return defaultId ? this.schemas().find(s => s.id === defaultId) : undefined;
  });

  // QR Dialog state
  qrDialogVisible = false;
  qrSchemaId = '';
  expiryMonths = 12;
  qrResult = signal<GenerateIntakeQrLinkResponse | null>(null);
  qrPublicUrl = signal('');
  qrImageUrl = signal<string | null>(null);
  qrLoading = signal(false);
  loadingStates = signal<Record<string, string>>({});



  ngOnInit(): void {
    this.loadSchemas();
    this.loadDefaultForm();
  }

  private loadDefaultForm(): void {
    this.defaultFormLoading.set(true);
    this.apiService.getDefaultFormSchema().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (form) => {
        this.defaultForm.set(form);
        this.defaultFormLoading.set(false);
      },
      error: () => {
        this.defaultForm.set(null);
        this.defaultFormLoading.set(false);
      }
    });
  }

  getFieldsCount(schemaJson: string): number {
    try {
      const parsed = JSON.parse(schemaJson);
      const sections = parsed.sections ?? parsed.Sections ?? [];
      let count = 0;
      for (const section of sections) {
        const groups = section.groups ?? section.Groups ?? [];
        for (const group of groups) {
          const questions = group.questions ?? group.Questions ?? [];
          count += questions.length;
        }
      }
      return count;
    } catch {
      return 0;
    }
  }

  generateDefaultForm(): void {
    this.defaultFormLoading.set(true);
    this.apiService.generateDefaultFormSchema().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snackbar.success('Default template created', ['The default intake form has been generated']);
        this.loadSchemas();
        this.loadDefaultForm();
      },
      error: (err: any) => {
        this.defaultFormLoading.set(false);
        this.snackbar.error('Failed to create default', [this.extractError(err)]);
      }
    });
  }

  private setLoading(id: string, key: string): void {
    this.loadingStates.update(s => ({ ...s, [id]: key }));
  }

  private clearLoading(id: string): void {
    this.loadingStates.update(s => {
      const next = { ...s };
      delete next[id];
      return next;
    });
  }

  isLoading(id: string, key: string): boolean {
    return this.loadingStates()[id] === key;
  }

  loadSchemas(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.apiService.getFormSchemas().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.schemas.set(data);
        this.applyFilters();
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(err?.error?.detail || err?.error?.title || 'Could not load schemas. Please try again.');
        this.loading.set(false);
      }
    });
  }

  setTab(tab: TabFilter): void {
    this.activeTab.set(tab);
    this.currentPage.set(1);
    this.applyFilters();
  }

  onSearch(): void {
    this.currentPage.set(1);
    this.applyFilters();
  }

  setSort(field: SortField): void {
    this.sortBy.set(field);
    this.applyFilters();
  }

  private applyFilters(): void {
    let result = this.schemas();

    const tab = this.activeTab();
    if (tab !== 'all') {
      const statusMap: Record<TabFilter, FormSchemaStatus> = {
        all: undefined!,
        published: FormSchemaStatus.Published,
        draft: FormSchemaStatus.Draft,
        archived: FormSchemaStatus.Archived
      };
      result = result.filter(s => s.status === statusMap[tab]);
    }

    const term = this.searchTerm.toLowerCase().trim();
    if (term) {
      result = result.filter(s =>
        s.name.toLowerCase().includes(term) ||
        s.description?.toLowerCase().includes(term) ||
        s.shortCode.toLowerCase().includes(term)
      );
    }

    const sort = this.sortBy();
    if (sort === 'name') {
      result = [...result].sort((a, b) => a.name.localeCompare(b.name));
    } else if (sort === 'updated') {
      result = [...result].sort((a, b) => {
        const dateA = new Date(b.publishedAt || b.createdAt).getTime();
        const dateB = new Date(a.publishedAt || a.createdAt).getTime();
        return dateA - dateB;
      });
    } else if (sort === 'submissions') {
      result = [...result].sort((a, b) => (b.submissionCount ?? 0) - (a.submissionCount ?? 0));
    }

    this.filteredSchemas.set(result);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  createSchema(): void {
    this.router.navigate(['/app/intake/schemas/new']);
  }

  editSchema(id: string): void {
    this.router.navigate(['/app/intake/schemas/edit', id]);
  }

  getFriendlyFormId(schema: FormSchemaSummaryResponse | FormSchemaResponse): string {
    return `Form ID: ${schema.shortCode}`;
  }

  duplicateSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.setLoading(schema.id, 'duplicate');
    this.apiService.duplicateFormSchema(schema.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(schema.id);
        this.snackbar.success('Schema duplicated', ['A copy has been created']);
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error('Duplicate failed', [this.extractError(err)]);
      }
    });
  }

  previewSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.router.navigate(['/app/intake/schemas/edit', schema.id], { queryParams: { preview: true } });
  }

  confirmDelete(schema: FormSchemaSummaryResponse): void {
    this.confirmationService.confirm({
      message: `Delete "${schema.name}"?${(schema.submissionCount ?? 0) > 0 ? ` It has ${schema.submissionCount} submission${(schema.submissionCount ?? 0) !== 1 ? 's' : ''} that will be orphaned.` : ''} This action cannot be undone.`,
      header: 'Delete Schema',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.deleteSchema(schema.id)
    });
  }

  private deleteSchema(id: string): void {
    this.setLoading(id, 'delete');
    this.apiService.deleteFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success('Schema deleted', ['The form template has been removed']);
        if (this.paginatedSchemas().length <= 1 && this.currentPage() > 1) {
          this.goToPage(this.currentPage() - 1);
        }
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error('Delete failed', [this.extractError(err)]);
      }
    });
  }

  confirmArchive(schema: FormSchemaSummaryResponse): void {
    this.confirmationService.confirm({
      message: `Archive "${schema.name}"? It will be hidden from active views but can be unarchived later.`,
      header: 'Archive Schema',
      icon: 'pi pi-archive',
      acceptLabel: 'Archive',
      rejectLabel: 'Cancel',
      accept: () => this.archiveSchema(schema.id)
    });
  }

  private archiveSchema(id: string): void {
    this.setLoading(id, 'archive');
    this.apiService.archiveFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success('Schema archived', ['The form template has been archived']);
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error('Archive failed', [this.extractError(err)]);
      }
    });
  }

  confirmUnarchive(schema: FormSchemaSummaryResponse): void {
    this.confirmationService.confirm({
      message: `Unarchive "${schema.name}"? It will be published and available again.`,
      header: 'Unarchive Schema',
      icon: 'pi pi-archive',
      acceptLabel: 'Unarchive',
      rejectLabel: 'Cancel',
      accept: () => this.unarchiveSchema(schema.id)
    });
  }

  private unarchiveSchema(id: string): void {
    this.setLoading(id, 'unarchive');
    this.apiService.unarchiveFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success('Schema unarchived', ['The form template has been published again']);
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error('Unarchive failed', [this.extractError(err)]);
      }
    });
  }

  exportJson(schema: FormSchemaSummaryResponse): void {
    this.setLoading(schema.id, 'export');
    this.apiService.getFormSchemaById(schema.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (full) => {
        this.clearLoading(schema.id);
        const json = JSON.stringify(JSON.parse(full.schemaJson), null, 2);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${schema.name.replace(/[^a-zA-Z0-9]/g, '_')}.json`;
        a.click();
        URL.revokeObjectURL(url);
        this.snackbar.success('Exported', ['Schema JSON downloaded']);
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error('Export failed', [this.extractError(err)]);
      }
    });
  }

  quickShare(schema: FormSchemaSummaryResponse): void {
    if (schema.status !== FormSchemaStatus.Published) {
      this.snackbar.warning('Not published', ['Publish the schema first to generate a shareable link.']);
      return;
    }
    this.setLoading(schema.id, 'share');
    this.apiService.generateIntakeQrLink(schema.id, { expiryMonths: 12 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.clearLoading(schema.id);
        const publicUrl = this.normalizePublicUrl(result.publicUrl || result.token);
        navigator.clipboard.writeText(publicUrl).then(() => {
          this.snackbar.success('Link copied', ['Public URL copied to clipboard']);
        }).catch(() => {
          this.snackbar.success('Link ready', [publicUrl]);
        });
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error('Share failed', [this.extractError(err)]);
      }
    });
  }

  publishSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.setLoading(schema.id, 'publish');
    this.apiService.publishFormSchema(schema.id, { version: schema.version }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(schema.id);
        this.snackbar.success('Schema published', ['Form schema is now live']);
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error('Publish failed', [this.extractError(err)]);
      }
    });
  }

  openQrDialog(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    if (schema.status !== FormSchemaStatus.Published) {
      this.snackbar.warning('Not published', ['Only published schemas can generate QR codes.']);
      return;
    }
    this.qrSchemaId = schema.id;
    this.expiryMonths = 12;
    this.qrResult.set(null);
    this.qrPublicUrl.set('');
    this.qrImageUrl.set(null);
    this.qrDialogVisible = true;
  }

  closeQrDialog(): void {
    this.qrDialogVisible = false;
    this.qrResult.set(null);
    this.qrPublicUrl.set('');
    this.qrImageUrl.set(null);
  }

  generateQr(): void {
    this.qrLoading.set(true);
    this.apiService.generateIntakeQrLink(this.qrSchemaId, { expiryMonths: this.expiryMonths }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        const publicUrl = this.normalizePublicUrl(result.publicUrl || result.token);
        setTimeout(() => {
          this.qrResult.set({ ...result, publicUrl });
          this.qrPublicUrl.set(publicUrl);
          this.qrLoading.set(false);
          void this.renderQrCode(publicUrl);
        }, 0);
      },
      error: (err: any) => {
        setTimeout(() => {
          this.qrLoading.set(false);
          this.qrPublicUrl.set('');
          this.qrImageUrl.set(null);
          this.snackbar.error('QR generation failed', [this.extractError(err)]);
        }, 0);
      }
    });
  }

  printQrCode(): void {
    const imageUrl = this.qrImageUrl();
    const publicUrl = this.qrPublicUrl();
    if (!imageUrl || !publicUrl) return;

    const printWindow = window.open('', '_blank', 'width=500,height=650');
    if (!printWindow) {
      this.snackbar.error('Print blocked', ['Allow pop-ups for this site to print the QR code.']);
      return;
    }

    const html = `<!DOCTYPE html>
<html lang="en">
<head><meta charset="UTF-8"/><title>Patient Intake QR Code</title><style>
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 32px; box-sizing: border-box; background: #fff; color: #0f172a; }
  h1 { font-size: 1.25rem; font-weight: 700; margin: 0 0 4px; text-align: center; }
  p.subtitle { font-size: 0.8rem; color: #64748b; margin: 0 0 24px; text-align: center; }
  img { width: 240px; height: 240px; border: 1px solid #e2e8f0; border-radius: 12px; display: block; }
  .url-label { font-size: 0.65rem; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; color: #94a3b8; margin-top: 20px; text-align: center; }
  .url-box { margin-top: 6px; padding: 10px 16px; border: 1px solid #e2e8f0; border-radius: 8px; background: #f8fafc; font-family: 'Courier New', monospace; font-size: 0.75rem; color: #334155; word-break: break-all; max-width: 320px; text-align: center; }
  @media print { body { padding: 16px; } }
</style></head>
<body><h1>Patient Intake Form</h1><p class="subtitle">Scan or visit the URL below to complete the pre-visit intake</p><img src="${imageUrl}" alt="QR Code" /><p class="url-label">Public URL</p><div class="url-box">${publicUrl}</div><script>window.onload = function() { window.print(); window.close(); };<\/script></body></html>`;

    printWindow.document.write(html);
    printWindow.document.close();
  }

  private normalizePublicUrl(value: string | undefined): string {
    if (!value?.trim()) return this.qrAccessService.generatePublicUrl('');
    if (/^https?:\/\//i.test(value)) return value;
    return this.qrAccessService.generatePublicUrl(value);
  }

  private renderQrCode(url: string): void {
    try {
      this.qrImageUrl.set(`https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(url)}`);
    } catch {
      this.qrImageUrl.set(null);
    }
  }

  openUrl(url: string): void {
    if (url) window.open(url, '_blank', 'noopener,noreferrer');
  }

  copyToClipboard(url: string): void {
    navigator.clipboard.writeText(url).then(() => this.snackbar.success('Copied', ['URL copied to clipboard']))
      .catch(() => this.snackbar.error('Copy failed', ['Could not copy URL']));
  }

  relativeTime(dateStr: string | undefined | null): string {
    if (!dateStr) return '';
    const diffMs = new Date().getTime() - new Date(dateStr).getTime();
    const diffSec = Math.floor(diffMs / 1000);
    if (diffSec < 60) return 'Just now';
    const diffMin = Math.floor(diffSec / 60);
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `${diffHrs}h ago`;
    const diffDays = Math.floor(diffHrs / 24);
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 30) return `${diffDays} days ago`;
    const diffMonths = Math.floor(diffDays / 30);
    if (diffMonths < 12) return `${diffMonths}mo ago`;
    return `${Math.floor(diffMonths / 12)}y ago`;
  }

  getStatusLabel(status: FormSchemaStatus): string {
    switch (status) {
      case FormSchemaStatus.Draft: return 'Draft';
      case FormSchemaStatus.Published: return 'Published';
      case FormSchemaStatus.Archived: return 'Archived';
      default: return 'Unknown';
    }
  }

  getCardBorderClass(status: FormSchemaStatus): string {
    switch (status) {
      case FormSchemaStatus.Published: return 'card-border--published';
      case FormSchemaStatus.Archived: return 'card-border--archived';
      default: return 'card-border--draft';
    }
  }

  private extractError(err: any): string {
    const body = err?.error;
    if (body?.detail) return body.detail;
    if (body?.errors) return Object.values(body.errors as Record<string, string[]>).flat().join('; ');
    return body?.title || 'Unexpected error';
  }

  trackById(_index: number, item: FormSchemaSummaryResponse): string {
    return item.id;
  }

}
