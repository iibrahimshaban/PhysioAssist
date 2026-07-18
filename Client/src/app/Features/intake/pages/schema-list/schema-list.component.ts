import { Component, inject, signal, OnInit, DestroyRef, HostListener } from '@angular/core';
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
import { FormSchemaSummaryResponse, FormSchemaStatus, GenerateIntakeQrLinkResponse } from '../../models';


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
  templateUrl: './schema-list.component.html',
  styleUrl: './schema-list.component.css'
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

  /** True when the viewport is narrower than 640 px (sm breakpoint). */
  isMobile = signal(typeof window !== 'undefined' ? window.innerWidth < 640 : false);

  @HostListener('window:resize')
  onResize(): void {
    if (typeof window !== 'undefined') {
      this.isMobile.set(window.innerWidth < 640);
    }
  }

  readonly FormSchemaStatus = FormSchemaStatus;

  // QR Dialog state
  qrDialogVisible = false;
  qrSchemaId = '';
  /** Duration in months. Max 24 months (2 years). */
  expiryMonths = 12;
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
    this.router.navigate(['/app/intake/schemas/new']);
  }

  handleActionClick(event: Event | undefined, callback: () => void): void {
    event?.preventDefault();
    event?.stopPropagation();
    callback();
  }

  editSchema(id: string): void {
    this.router.navigate(['/app/intake/schemas/edit', id]);
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
    const expiryMonths = this.expiryMonths;
    this.apiService.generateIntakeQrLink(this.qrSchemaId, { expiryMonths }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  /**
   * Opens a minimal browser print window containing the QR code image and
   * the public URL underneath — zero-dependency, uses window.print().
   */
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
<head>
  <meta charset="UTF-8"/>
  <title>Patient Intake QR Code</title>
  <style>
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      min-height: 100vh; margin: 0; padding: 32px;
      box-sizing: border-box; background: #fff; color: #0f172a;
    }
    h1 { font-size: 1.25rem; font-weight: 700; margin: 0 0 4px; text-align: center; }
    p.subtitle { font-size: 0.8rem; color: #64748b; margin: 0 0 24px; text-align: center; }
    img { width: 240px; height: 240px; border: 1px solid #e2e8f0; border-radius: 12px; display: block; }
    .url-label {
      font-size: 0.65rem; font-weight: 700; letter-spacing: 0.06em;
      text-transform: uppercase; color: #94a3b8; margin-top: 20px; text-align: center;
    }
    .url-box {
      margin-top: 6px; padding: 10px 16px; border: 1px solid #e2e8f0;
      border-radius: 8px; background: #f8fafc;
      font-family: 'Courier New', monospace; font-size: 0.75rem;
      color: #334155; word-break: break-all; max-width: 320px; text-align: center;
    }
    @media print { body { padding: 16px; } }
  </style>
</head>
<body>
  <h1>Patient Intake Form</h1>
  <p class="subtitle">Scan or visit the URL below to complete the pre-visit intake</p>
  <img src="${imageUrl}" alt="QR Code" />
  <p class="url-label">Public URL</p>
  <div class="url-box">${publicUrl}</div>
  <script>window.onload = function() { window.print(); window.close(); };<` + `/script>
</body>
</html>`;

    printWindow.document.write(html);
    printWindow.document.close();
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
    if (!url) return;
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

  getStatusBadgeClass(status: FormSchemaStatus): string {
    switch (status) {
      case FormSchemaStatus.Published: return 'status-badge-success';
      case FormSchemaStatus.Archived: return 'status-badge-neutral';
      default: return 'status-badge-warning'; // Draft
    }
  }
}
