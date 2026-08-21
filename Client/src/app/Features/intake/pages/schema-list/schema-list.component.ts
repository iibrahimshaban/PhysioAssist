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
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { IntakeApiService } from '../../services/intake-api.service';
import { QrAccessService } from '../../services/qr-access.service';
import { IntakePageContainerComponent } from '../../shared/intake-page-container.component';
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
    TooltipModule,
    IntakePageContainerComponent,
    TranslocoModule
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
  private readonly transloco = inject(TranslocoService);


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
    { value: 'name' as SortField, labelKey: 'intake.schemaList.toolbar.sortName' },
    { value: 'updated' as SortField, labelKey: 'intake.schemaList.toolbar.sortUpdated' },
    { value: 'submissions' as SortField, labelKey: 'intake.schemaList.toolbar.sortSubmissions' }
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
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.defaultCreated'));
        this.loadSchemas();
        this.loadDefaultForm();
      },
      error: (err: any) => {
        this.defaultFormLoading.set(false);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.defaultFailed'));
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
        this.loadError.set(err?.error?.detail || err?.error?.title || this.transloco.translate('intake.schemaList.snackbar.loadFailedFallback'));
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

  openWizard(): void {
    this.router.navigate(['/app/intake/schemas/wizard']);
  }

  editSchema(id: string): void {
    this.router.navigate(['/app/intake/schemas/edit', id]);
  }

  getFriendlyFormId(schema: FormSchemaSummaryResponse | FormSchemaResponse): string {
    const code = schema.shortCode?.trim();
    return this.transloco.translate('intake.schemaList.formId', { id: code || schema.id });
  }

  duplicateSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.setLoading(schema.id, 'duplicate');
    this.apiService.duplicateFormSchema(schema.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(schema.id);
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.duplicated'));
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.duplicateFailed'));
      }
    });
  }

  previewSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.router.navigate(['/app/intake/schemas/edit', schema.id], { queryParams: { preview: true } });
  }

  confirmDelete(schema: FormSchemaSummaryResponse): void {
    const count = schema.submissionCount ?? 0;
    this.confirmationService.confirm({
      message: this.transloco.translate('intake.schemaList.confirm.deleteMessage', {
        name: schema.name,
        submissions: count > 0 ? this.transloco.translate('intake.schemaList.confirm.deleteSubmissions', { count }) : ''
      }),
      header: this.transloco.translate('intake.schemaList.confirm.deleteHeader'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.transloco.translate('intake.schemaList.confirm.acceptDelete'),
      rejectLabel: this.transloco.translate('intake.common.cancel'),
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.deleteSchema(schema.id)
    });
  }

  private deleteSchema(id: string): void {
    this.setLoading(id, 'delete');
    this.apiService.deleteFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.deleted'));
        if (this.paginatedSchemas().length <= 1 && this.currentPage() > 1) {
          this.goToPage(this.currentPage() - 1);
        }
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.deleteFailed'));
      }
    });
  }

  confirmArchive(schema: FormSchemaSummaryResponse): void {
    this.confirmationService.confirm({
      message: this.transloco.translate('intake.schemaList.confirm.archiveMessage', { name: schema.name }),
      header: this.transloco.translate('intake.schemaList.confirm.archiveHeader'),
      icon: 'pi pi-archive',
      acceptLabel: this.transloco.translate('intake.schemaList.confirm.acceptArchive'),
      rejectLabel: this.transloco.translate('intake.common.cancel'),
      acceptButtonStyleClass: 'p-button-primary',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => this.archiveSchema(schema.id)
    });
  }

  private archiveSchema(id: string): void {
    this.setLoading(id, 'archive');
    this.apiService.archiveFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.archived'));
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.archiveFailed'));
      }
    });
  }

  confirmUnarchive(schema: FormSchemaSummaryResponse): void {
    this.confirmationService.confirm({
      message: this.transloco.translate('intake.schemaList.confirm.unarchiveMessage', { name: schema.name }),
      header: this.transloco.translate('intake.schemaList.confirm.unarchiveHeader'),
      icon: 'pi pi-archive',
      acceptLabel: this.transloco.translate('intake.schemaList.confirm.acceptUnarchive'),
      rejectLabel: this.transloco.translate('intake.common.cancel'),
      acceptButtonStyleClass: 'p-button-primary',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => this.unarchiveSchema(schema.id)
    });
  }

  private unarchiveSchema(id: string): void {
    this.setLoading(id, 'unarchive');
    this.apiService.unarchiveFormSchema(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(id);
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.unarchived'));
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.unarchiveFailed'));
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
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.exported'));
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.exportFailed'));
      }
    });
  }

  quickShare(schema: FormSchemaSummaryResponse): void {
    if (schema.status !== FormSchemaStatus.Published) {
      const [t, m] = this.msg('intake.schemaList.snackbar.notPublished');
      this.snackbar.warning(t, [...m, this.transloco.translate('intake.schemaList.snackbar.notPublishedShare')]);
      return;
    }
    this.setLoading(schema.id, 'share');
    this.apiService.generateIntakeQrLink(schema.id, { expiryMonths: 12 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.clearLoading(schema.id);
        const publicUrl = this.normalizePublicUrl(result.publicUrl || result.token);
        navigator.clipboard.writeText(publicUrl).then(() => {
          this.snackbar.success(...this.msg('intake.schemaList.snackbar.linkCopied'));
        }).catch(() => {
          const [t, m] = this.msg('intake.schemaList.snackbar.linkReady');
          this.snackbar.success(t, [...m, publicUrl]);
        });
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.shareFailed'));
      }
    });
  }

  publishSchema(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    this.setLoading(schema.id, 'publish');
    this.apiService.publishFormSchema(schema.id, { version: schema.version }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.clearLoading(schema.id);
        this.snackbar.success(...this.msg('intake.schemaList.snackbar.published'));
        this.loadSchemas();
      },
      error: (err: any) => {
        this.clearLoading(schema.id);
        this.snackbar.error(...this.msg('intake.schemaList.snackbar.publishFailed'));
      }
    });
  }

  openQrDialog(schema: FormSchemaSummaryResponse | FormSchemaResponse): void {
    if (schema.status !== FormSchemaStatus.Published) {
      const [t, m] = this.msg('intake.schemaList.snackbar.notPublished');
      this.snackbar.warning(t, [...m, this.transloco.translate('intake.schemaList.snackbar.notPublishedQr')]);
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
          this.snackbar.error(...this.msg('intake.schemaList.snackbar.qrFailed'));
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
      this.snackbar.error(...this.msg('intake.schemaList.snackbar.printBlocked'));
      return;
    }

    const html = `<!DOCTYPE html>
<html lang="en">
<head><meta charset="UTF-8"/><title>${this.transloco.translate('intake.schemaList.print.title')}</title><style>
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 32px; box-sizing: border-box; background: #fff; color: #0f172a; }
  h1 { font-size: 1.25rem; font-weight: 700; margin: 0 0 4px; text-align: center; }
  p.subtitle { font-size: 0.8rem; color: #64748b; margin: 0 0 24px; text-align: center; }
  img { width: 240px; height: 240px; border: 1px solid #e2e8f0; border-radius: 12px; display: block; }
  .url-label { font-size: 0.65rem; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; color: #94a3b8; margin-top: 20px; text-align: center; }
  .url-box { margin-top: 6px; padding: 10px 16px; border: 1px solid #e2e8f0; border-radius: 8px; background: #f8fafc; font-family: 'Courier New', monospace; font-size: 0.75rem; color: #334155; word-break: break-all; max-width: 320px; text-align: center; }
  @media print { body { padding: 16px; } }
</style></head>
<body><h1>${this.transloco.translate('intake.schemaList.print.formName')}</h1><p class="subtitle">${this.transloco.translate('intake.schemaList.print.instructions')}</p><img src="${imageUrl}" alt="QR Code" /><p class="url-label">${this.transloco.translate('intake.schemaList.print.publicUrl')}</p><div class="url-box">${publicUrl}</div><script>window.onload = function() { window.print(); window.close(); };<\/script></body></html>`;

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
    navigator.clipboard.writeText(url).then(() => this.snackbar.success(...this.msg('intake.schemaList.snackbar.copied')))
      .catch(() => this.snackbar.error(...this.msg('intake.schemaList.snackbar.copyFailed')));
  }

  private msg(key: string): [string, string[]] {
    const value = this.transloco.translate(key);
    const parts: string[] = Array.isArray(value) ? value.map(String) : [String(value)];
    return [parts[0], parts.slice(1)];
  }

  relativeTime(dateStr: string | undefined | null): string {
    if (!dateStr) return '';

    // Ensure the string is parsed as UTC — backend sends DateTime.UtcNow,
    // but if the serialized string lacks a timezone suffix, JS parses it
    // as local time instead, throwing calculations off by the local UTC offset.
    const utcStr = /Z$|[+-]\d{2}:\d{2}$/.test(dateStr) ? dateStr : `${dateStr}Z`;
    const parsed = new Date(utcStr);
    if (isNaN(parsed.getTime())) return '';

    const diffMs = Date.now() - parsed.getTime();
    if (diffMs < 0) return this.transloco.translate('intake.schemaList.relativeTime.justNow');

    const diffSec = Math.floor(diffMs / 1000);
    if (diffSec < 60) return this.transloco.translate('intake.schemaList.relativeTime.justNow');
    const diffMin = Math.floor(diffSec / 60);
    if (diffMin < 60) return this.transloco.translate('intake.schemaList.relativeTime.minAgo', { n: diffMin });
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return this.transloco.translate('intake.schemaList.relativeTime.hourAgo', { n: diffHrs });
    const diffDays = Math.floor(diffHrs / 24);
    if (diffDays === 1) return this.transloco.translate('intake.schemaList.relativeTime.yesterday');
    if (diffDays < 7) return this.transloco.translate('intake.schemaList.relativeTime.dayAgo', { n: diffDays });

    return parsed.toLocaleDateString(this.transloco.getActiveLang() === 'ar' ? 'ar-EG' : undefined, {
      month: 'short',
      day: 'numeric',
      timeZone: 'Africa/Cairo' // for the absolute-date fallback, display in Cairo local time
    });
  }

  getStatusLabel(status: FormSchemaStatus): string {
    switch (status) {
      case FormSchemaStatus.Draft: return this.transloco.translate('intake.schemaList.schemaStatus.draft');
      case FormSchemaStatus.Published: return this.transloco.translate('intake.schemaList.schemaStatus.published');
      case FormSchemaStatus.Archived: return this.transloco.translate('intake.schemaList.schemaStatus.archived');
      default: return this.transloco.translate('intake.schemaList.schemaStatus.unknown');
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
    return body?.title || this.transloco.translate('intake.schemaList.errors.unexpected');
  }

  trackById(_index: number, item: FormSchemaSummaryResponse): string {
    return item.id;
  }

}
