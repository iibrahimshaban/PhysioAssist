import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageModule } from 'primeng/message';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { QrAccessService } from '../../services/qr-access.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import { FormSchemaSummaryResponse, FormSchemaStatus, GenerateIntakeQrLinkRequest, GenerateIntakeQrLinkResponse } from '../../models';

@Component({
  selector: 'app-schema-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TagModule,
    CardModule,
    InputNumberModule,
    DialogModule,
    TooltipModule,
    MessageModule
  ],
  template: `
    <div class="page-container animate-fade-in">
      <!-- Page Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl flex items-center justify-center"
               style="background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);">
            <i class="pi pi-file-edit text-white text-lg"></i>
          </div>
          <div>
            <h1 class="page-title">Form Schemas</h1>
            <p class="page-subtitle">Manage and publish your intake form templates</p>
          </div>
        </div>
        <p-button
          label="Create Schema"
          icon="pi pi-plus"
          (onClick)="createSchema()"
          severity="primary"
          styleClass="shadow-sm">
        </p-button>
      </div>

      <!-- Main Card -->
      <p-card>
        <ng-template pTemplate="content">
          <!-- Search Bar -->
          <div class="mb-5 px-1">
            <div class="relative">
              <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 text-sm"></i>
              <input
                pInputText
                type="text"
                [(ngModel)]="searchTerm"
                (input)="onSearch()"
                placeholder="Search schemas by name or description..."
                class="w-full !pl-10"
                style="padding-left: 2.5rem !important;" />
            </div>
          </div>

          <!-- Loading Skeleton -->
          @if (loading()) {
            <div class="stagger-children">
              @for (i of [1,2,3,4,5]; track i) {
                <div class="skeleton-row">
                  <div class="skeleton skeleton-text" style="width: 160px; height: 14px;"></div>
                  <div class="skeleton skeleton-text" style="width: 200px; height: 12px;"></div>
                  <div class="skeleton skeleton-text" style="width: 40px; height: 12px;"></div>
                  <div class="skeleton" style="width: 70px; height: 24px; border-radius: 9999px;"></div>
                  <div class="skeleton skeleton-text" style="width: 24px; height: 14px;"></div>
                  <div class="skeleton skeleton-text" style="width: 100px; height: 12px;"></div>
                  <div class="flex gap-2">
                    <div class="skeleton" style="width: 32px; height: 32px; border-radius: 8px;"></div>
                    <div class="skeleton" style="width: 32px; height: 32px; border-radius: 8px;"></div>
                  </div>
                </div>
              }
            </div>
          }

          <!-- Error State -->
          @if (!loading() && loadError()) {
            <div class="empty-state py-12 animate-fade-in-up" role="alert">
              <div class="empty-state-icon" style="background: #fef2f2; color: #ef4444; width: 5rem; height: 5rem; font-size: 2rem;">
                <i class="pi pi-exclamation-triangle"></i>
              </div>
              <h3 class="empty-state-title">Failed to load schemas</h3>
              <p class="empty-state-text">{{ loadError() }}</p>
              <p-button label="Try Again" icon="pi pi-refresh" severity="warn" (onClick)="loadSchemas()" />
            </div>
          }

          <!-- Table -->
          @if (!loading() && !loadError()) {
            <p-table
              [value]="filteredSchemas()"
              [paginator]="true"
              [rows]="10"
              [showCurrentPageReport]="true"
              currentPageReportTemplate="Showing {first} to {last} of {totalRecords} schemas"
              [rowHover]="true"
              styleClass="p-datatable-sm cursor-pointer">

              <ng-template pTemplate="header">
                <tr>
                  <th>Name</th>
                  <th class="hide-on-mobile">Description</th>
                  <th>Version</th>
                  <th>Status</th>
                  <th>Default</th>
                  <th class="hide-on-mobile">Published</th>
                  <th class="text-center" style="width: 140px;">Actions</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-schema>
                <tr class="animate-fade-in" (click)="editSchema(schema.id)" style="cursor: pointer;">
                  <td>
                    <div class="flex items-center gap-2.5">
                      <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0"
                           [style.background]="schema.status === FormSchemaStatus.Published ? '#ecfdf5' : '#f1f5f9'">
                        <i class="pi text-xs"
                           [class.pi-check-circle]="schema.status === FormSchemaStatus.Published"
                           [class.pi-file-edit]="schema.status !== FormSchemaStatus.Published"
                           [style.color]="schema.status === FormSchemaStatus.Published ? '#22c55e' : '#94a3b8'"></i>
                      </div>
                      <span class="font-semibold text-surface-800">{{ schema.name }}</span>
                    </div>
                  </td>
                  <td class="hide-on-mobile">
                    <span class="text-surface-500 text-sm">{{ schema.description || '—' }}</span>
                  </td>
                  <td>
                    <span class="inline-flex items-center px-2 py-0.5 rounded-md bg-surface-100 text-surface-600 text-xs font-medium">
                      v{{ schema.version }}
                    </span>
                  </td>
                  <td>
                    <p-tag
                      [value]="getStatusLabel(schema.status)"
                      [severity]="getStatusSeverity(schema.status)">
                    </p-tag>
                  </td>
                  <td>
                    @if (schema.isDefault) {
                      <div class="flex items-center gap-1.5">
                        <i class="pi pi-star-fill text-amber-400 text-sm animate-pulse-slow"></i>
                      </div>
                    } @else {
                      <span class="text-surface-300">—</span>
                    }
                  </td>
                  <td class="hide-on-mobile">
                    <span class="text-sm text-surface-500">
                      {{ schema.publishedAt ? (schema.publishedAt | date: 'MMM d, y') : '—' }}
                    </span>
                  </td>
                  <td>
                    <div class="flex gap-1.5 justify-center">
                        <p-button
                          icon="pi pi-pencil"
                          [rounded]="true"
                          severity="secondary"
                          size="small"
                          pTooltip="Edit"
                          tooltipPosition="top"
                          (onClick)="handleActionClick($event, () => editSchema(schema.id))">
                        </p-button>

                      @if (schema.status === FormSchemaStatus.Draft) {
                        <p-button
                          icon="pi pi-check-circle"
                          [rounded]="true"
                          severity="success"
                          size="small"
                          pTooltip="Publish"
                          tooltipPosition="top"
                          (onClick)="handleActionClick($event, () => publishSchema(schema))"
                          [loading]="publishLoading() === schema.id">
                        </p-button>
                      }

                      @if (schema.status === FormSchemaStatus.Published) {
                        <p-button
                          icon="pi pi-qrcode"
                          [rounded]="true"
                          severity="help"
                          size="small"
                          pTooltip="Generate QR Link"
                          tooltipPosition="top"
                          (onClick)="handleActionClick($event, () => openQrDialog(schema.id))">
                        </p-button>
                      }
                    </div>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td colspan="7">
                    <div class="empty-state py-12">
                      <div class="empty-state-icon" style="width: 5rem; height: 5rem; font-size: 2rem;">
                        <i class="pi pi-inbox"></i>
                      </div>
                      <h3 class="empty-state-title text-lg">No schemas found</h3>
                      <p class="empty-state-text">
                        @if (searchTerm) {
                          No schemas match your search. Try a different term.
                        } @else {
                          Get started by creating your first intake form schema.
                        }
                      </p>
                      @if (!searchTerm) {
                        <p-button
                          label="Create Your First Schema"
                          icon="pi pi-plus"
                          (onClick)="createSchema()">
                        </p-button>
                      }
                    </div>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          }
        </ng-template>
      </p-card>

      <!-- QR Link Dialog -->
      <p-dialog
        [(visible)]="qrDialogVisible"
        [modal]="true"
        [closable]="true"
        [dismissableMask]="true"
        [focusOnShow]="false"
        [style]="{ width: '480px' }">

        <ng-template pTemplate="header">
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 rounded-lg flex items-center justify-center"
                 style="background: linear-gradient(135deg, #a855f7 0%, #7c3aed 100%);">
              <i class="pi pi-qrcode text-white"></i>
            </div>
            <div>
              <h3 class="font-bold text-base m-0">Generate QR Link</h3>
              <p class="text-xs text-surface-500 m-0">Create a public intake URL</p>
            </div>
          </div>
        </ng-template>

        @if (!qrResult()) {
          <div class="space-y-5">
            <div>
              <label class="block text-sm font-semibold text-surface-700 mb-2">
                <i class="pi pi-clock text-xs mr-1.5 text-surface-400"></i>
                Expiry Time (hours)
              </label>
              <p-inputNumber
                [(ngModel)]="expiryHours"
                [min]="1"
                [max]="8760"
                [showButtons]="true"
                class="w-full">
              </p-inputNumber>
              <p class="text-xs text-surface-400 mt-1.5">
                Link will expire after this period. Max: 8760 hours (1 year)
              </p>
            </div>
          </div>
          <ng-template pTemplate="footer">
            <div class="flex justify-end gap-2">
              <p-button
                label="Cancel"
                severity="secondary"
                [outlined]="true"
                (onClick)="closeQrDialog()">
              </p-button>
              <p-button
                label="Generate Link"
                icon="pi pi-link"
                (onClick)="generateQr()"
                [loading]="qrLoading()">
              </p-button>
            </div>
          </ng-template>
        } @else {
          <div class="space-y-5 animate-fade-in-up">
            <!-- Success indicator -->
            <div class="flex items-center gap-3 p-3 rounded-xl" style="background: #f0fdf4;">
              <div class="w-8 h-8 rounded-full flex items-center justify-center" style="background: #22c55e;">
                <i class="pi pi-check text-white text-sm"></i>
              </div>
              <div>
                <p class="text-sm font-semibold text-green-800 m-0">Link generated successfully</p>
                <p class="text-xs text-green-600 m-0">Share this URL with patients</p>
              </div>
            </div>

            @if (qrImageUrl()) {
              <div class="flex flex-col items-center gap-3 rounded-2xl border border-slate-200 bg-white p-4">
                <img [src]="qrImageUrl()" alt="QR code for the public intake form" class="w-48 h-48 rounded-lg border border-slate-200 bg-white" />
                <p class="text-xs text-surface-500 m-0 text-center">Scan this code to open the intake form on a phone.</p>
              </div>
            }

            <div>
              <label class="block text-xs font-semibold text-surface-500 uppercase tracking-wider mb-2">Public URL</label>
              <div class="flex gap-2">
                <input
                  pInputText
                  [ngModel]="qrPublicUrl()"
                  readonly
                  class="flex-1 !bg-surface-50 font-mono text-sm" />
                <p-button
                  icon="pi pi-copy"
                  [outlined]="true"
                  severity="secondary"
                  (onClick)="copyToClipboard(qrPublicUrl())"
                  pTooltip="Copy URL">
                </p-button>
                <p-button
                  icon="pi pi-external-link"
                  [outlined]="true"
                  severity="primary"
                  (onClick)="openUrl(qrPublicUrl())"
                  pTooltip="Open in new tab">
                </p-button>
              </div>
            </div>

            <div class="flex items-center gap-2 p-3 rounded-lg bg-surface-50">
              <i class="pi pi-calendar text-surface-400 text-sm"></i>
              <div>
                <p class="text-xs text-surface-500 m-0">Expires</p>
                <p class="text-sm font-medium text-surface-700 m-0">{{ qrResult()?.expiresAt | date:'medium' }}</p>
              </div>
            </div>
          </div>
          <ng-template pTemplate="footer">
            <div class="flex justify-end">
              <p-button
                label="Done"
                icon="pi pi-check"
                (onClick)="closeQrDialog()">
              </p-button>
            </div>
          </ng-template>
        }
      </p-dialog>
    </div>
  `
})
export class SchemaListComponent implements OnInit {
  private readonly apiService = inject(IntakeApiService);
  protected readonly engine = inject(DynamicFormEngineService);
  protected readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly qrAccessService = inject(QrAccessService);

  schemas = signal<FormSchemaSummaryResponse[]>([]);
  filteredSchemas = signal<FormSchemaSummaryResponse[]>([]);
  loading = signal(false);
  loadError = signal<string | null>(null);
  searchTerm = '';

  readonly FormSchemaStatus = FormSchemaStatus;

  // QR Dialog state
  qrDialogVisible = false;
  qrSchemaId = '';
  expiryHours = 24;
  qrResult = signal<GenerateIntakeQrLinkResponse | null>(null);
  qrPublicUrl = signal('');
  qrImageUrl = signal<string | null>(null);
  qrLoading = signal(false);
  publishLoading = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSchemas();
  }

  loadSchemas(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.apiService.getFormSchemas().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.schemas.set(data);
        this.filteredSchemas.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(err?.error?.detail || err?.error?.title || 'Could not load schemas. Please try again.');
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    const term = this.searchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredSchemas.set(this.schemas());
      return;
    }

    const filtered = this.schemas().filter(schema =>
      schema.name.toLowerCase().includes(term) ||
      schema.description?.toLowerCase().includes(term)
    );
    this.filteredSchemas.set(filtered);
  }

  createSchema(): void {
    this.router.navigate(['app/intake/schemas/new']);
  }

  handleActionClick(event: Event | undefined, callback: () => void): void {
    event?.preventDefault();
    event?.stopPropagation();
    callback();
  }

  editSchema(id: string): void {
    this.router.navigate(['app/intake/schemas/edit', id]);
  }

  publishSchema(schema: FormSchemaSummaryResponse): void {
    this.publishLoading.set(schema.id);
    this.apiService.publishFormSchema(schema.id, { version: schema.version }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.publishLoading.set(null);
        this.snackbar.success('Schema published', ['Form schema is now live']);
        this.loadSchemas();
      },
      error: (_err: any) => {
        this.publishLoading.set(null);
        const msg = _err?.error?.detail || _err?.error?.title || 'Could not publish schema';
        this.snackbar.error('Publish failed', [msg]);
      }
    });
  }

  openQrDialog(id: string): void {
    this.qrSchemaId = id;
    this.expiryHours = 24;
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
    this.apiService.generateIntakeQrLink(this.qrSchemaId, { expiryHours: this.expiryHours }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
          const msg = err?.error?.detail || err?.error?.title || 'Could not generate QR link';
          this.snackbar.error('QR generation failed', [msg]);
        }, 0);
      }
    });
  }

  private normalizePublicUrl(value: string | undefined): string {
    if (!value?.trim()) {
      return this.qrAccessService.generatePublicUrl('');
    }

    if (/^https?:\/\//i.test(value)) {
      return value;
    }

    return this.qrAccessService.generatePublicUrl(value);
  }

  private renderQrCode(url: string): void {
    try {
      const encodedUrl = encodeURIComponent(url);
      const imageUrl = `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodedUrl}`;
      this.qrImageUrl.set(imageUrl);
    } catch {
      this.qrImageUrl.set(null);
    }
  }

  openUrl(url: string): void {
    if (!url) {
      return;
    }
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  copyToClipboard(url: string): void {
    navigator.clipboard.writeText(url).then(() => {
      this.snackbar.success('Copied', ['URL copied to clipboard']);
    }).catch(() => {
      this.snackbar.error('Copy failed', ['Could not copy URL']);
    });
  }

  getStatusLabel(status: FormSchemaStatus): string {
    switch (status) {
      case FormSchemaStatus.Draft: return 'Draft';
      case FormSchemaStatus.Published: return 'Published';
      case FormSchemaStatus.Archived: return 'Archived';
      default: return 'Unknown';
    }
  }

  getStatusSeverity(status: FormSchemaStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case FormSchemaStatus.Published: return 'success';
      case FormSchemaStatus.Archived: return 'secondary';
      default: return 'info';
    }
  }
}
