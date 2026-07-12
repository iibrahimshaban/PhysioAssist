import { Component, EventEmitter, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type BodyRegionShape = 'ellipse' | 'rect';

export interface BodyRegionDef {
  id: string;
  labelEn: string;
  labelAr: string;
  shape: BodyRegionShape;
  // ellipse
  cx?: number;
  cy?: number;
  rx?: number;
  ry?: number;
  // rect
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  radius?: number;
}

export interface PainRegionSelection {
  id: string;
  labelEn: string;
  labelAr: string;
  severity: number; // 1-10
}

export interface BodyPainMapPayload {
  regions: PainRegionSelection[];
  chiefComplaint: string;
}

const FRONT_REGIONS: BodyRegionDef[] = [
  { id: 'f-head', labelEn: 'Head', labelAr: 'الرأس', shape: 'ellipse', cx: 65, cy: 22, rx: 16, ry: 19 },
  { id: 'f-neck', labelEn: 'Neck', labelAr: 'الرقبة', shape: 'rect', x: 57, y: 40, width: 16, height: 12, radius: 3 },
  { id: 'f-r-shoulder', labelEn: 'Right shoulder', labelAr: 'كتف أيمن', shape: 'ellipse', cx: 35, cy: 60, rx: 13, ry: 10 },
  { id: 'f-l-shoulder', labelEn: 'Left shoulder', labelAr: 'كتف أيسر', shape: 'ellipse', cx: 95, cy: 60, rx: 13, ry: 10 },
  { id: 'f-chest', labelEn: 'Chest', labelAr: 'الصدر', shape: 'rect', x: 44, y: 52, width: 42, height: 32, radius: 4 },
  { id: 'f-r-arm', labelEn: 'Right upper arm', labelAr: 'ذراع أيمن', shape: 'rect', x: 22, y: 68, width: 14, height: 36, radius: 5 },
  { id: 'f-l-arm', labelEn: 'Left upper arm', labelAr: 'ذراع أيسر', shape: 'rect', x: 94, y: 68, width: 14, height: 36, radius: 5 },
  { id: 'f-r-elbow', labelEn: 'Right elbow', labelAr: 'كوع أيمن', shape: 'ellipse', cx: 29, cy: 110, rx: 9, ry: 8 },
  { id: 'f-l-elbow', labelEn: 'Left elbow', labelAr: 'كوع أيسر', shape: 'ellipse', cx: 101, cy: 110, rx: 9, ry: 8 },
  { id: 'f-abdomen', labelEn: 'Abdomen', labelAr: 'البطن', shape: 'rect', x: 44, y: 84, width: 42, height: 30, radius: 4 },
  { id: 'f-r-forearm', labelEn: 'Right forearm', labelAr: 'ساعد أيمن', shape: 'rect', x: 23, y: 116, width: 12, height: 28, radius: 4 },
  { id: 'f-l-forearm', labelEn: 'Left forearm', labelAr: 'ساعد أيسر', shape: 'rect', x: 95, y: 116, width: 12, height: 28, radius: 4 },
  { id: 'f-pelvis', labelEn: 'Pelvis', labelAr: 'الحوض', shape: 'rect', x: 44, y: 114, width: 42, height: 22, radius: 4 },
  { id: 'f-r-hand', labelEn: 'Right hand', labelAr: 'يد يمنى', shape: 'ellipse', cx: 29, cy: 152, rx: 9, ry: 7 },
  { id: 'f-l-hand', labelEn: 'Left hand', labelAr: 'يد يسرى', shape: 'ellipse', cx: 101, cy: 152, rx: 9, ry: 7 },
  { id: 'f-r-thigh', labelEn: 'Right thigh', labelAr: 'فخذ أيمن', shape: 'rect', x: 44, y: 136, width: 20, height: 50, radius: 6 },
  { id: 'f-l-thigh', labelEn: 'Left thigh', labelAr: 'فخذ أيسر', shape: 'rect', x: 66, y: 136, width: 20, height: 50, radius: 6 },
  { id: 'f-r-knee', labelEn: 'Right knee', labelAr: 'ركبة يمنى', shape: 'ellipse', cx: 54, cy: 192, rx: 10, ry: 9 },
  { id: 'f-l-knee', labelEn: 'Left knee', labelAr: 'ركبة يسرى', shape: 'ellipse', cx: 76, cy: 192, rx: 10, ry: 9 },
  { id: 'f-r-shin', labelEn: 'Right shin', labelAr: 'ساق أيمن', shape: 'rect', x: 45, y: 200, width: 18, height: 44, radius: 5 },
  { id: 'f-l-shin', labelEn: 'Left shin', labelAr: 'ساق أيسر', shape: 'rect', x: 67, y: 200, width: 18, height: 44, radius: 5 },
  { id: 'f-r-foot', labelEn: 'Right foot', labelAr: 'قدم يمنى', shape: 'ellipse', cx: 54, cy: 252, rx: 11, ry: 7 },
  { id: 'f-l-foot', labelEn: 'Left foot', labelAr: 'قدم يسرى', shape: 'ellipse', cx: 76, cy: 252, rx: 11, ry: 7 },
];

const BACK_REGIONS: BodyRegionDef[] = [
  { id: 'b-head', labelEn: 'Back of head', labelAr: 'مؤخرة الرأس', shape: 'ellipse', cx: 65, cy: 22, rx: 16, ry: 19 },
  { id: 'b-neck', labelEn: 'Back of neck', labelAr: 'رقبة خلفية', shape: 'rect', x: 57, y: 40, width: 16, height: 12, radius: 3 },
  { id: 'b-r-shoulder', labelEn: 'Right shoulder (back)', labelAr: 'كتف أيمن خلفي', shape: 'ellipse', cx: 35, cy: 60, rx: 13, ry: 10 },
  { id: 'b-l-shoulder', labelEn: 'Left shoulder (back)', labelAr: 'كتف أيسر خلفي', shape: 'ellipse', cx: 95, cy: 60, rx: 13, ry: 10 },
  { id: 'b-upper-back', labelEn: 'Upper back', labelAr: 'أعلى الظهر', shape: 'rect', x: 44, y: 52, width: 42, height: 32, radius: 4 },
  { id: 'b-r-arm', labelEn: 'Right upper arm (back)', labelAr: 'ذراع أيمن خلفي', shape: 'rect', x: 22, y: 68, width: 14, height: 36, radius: 5 },
  { id: 'b-l-arm', labelEn: 'Left upper arm (back)', labelAr: 'ذراع أيسر خلفي', shape: 'rect', x: 94, y: 68, width: 14, height: 36, radius: 5 },
  { id: 'b-r-elbow', labelEn: 'Right elbow (back)', labelAr: 'كوع أيمن خلفي', shape: 'ellipse', cx: 29, cy: 110, rx: 9, ry: 8 },
  { id: 'b-l-elbow', labelEn: 'Left elbow (back)', labelAr: 'كوع أيسر خلفي', shape: 'ellipse', cx: 101, cy: 110, rx: 9, ry: 8 },
  { id: 'b-lower-back', labelEn: 'Lower back', labelAr: 'أسفل الظهر', shape: 'rect', x: 44, y: 84, width: 42, height: 30, radius: 4 },
  { id: 'b-r-forearm', labelEn: 'Right forearm (back)', labelAr: 'ساعد أيمن خلفي', shape: 'rect', x: 23, y: 116, width: 12, height: 28, radius: 4 },
  { id: 'b-l-forearm', labelEn: 'Left forearm (back)', labelAr: 'ساعد أيسر خلفي', shape: 'rect', x: 95, y: 116, width: 12, height: 28, radius: 4 },
  { id: 'b-glutes', labelEn: 'Glutes', labelAr: 'الأرداف', shape: 'rect', x: 44, y: 114, width: 42, height: 22, radius: 4 },
  { id: 'b-r-hand', labelEn: 'Right hand (back)', labelAr: 'يد يمنى خلفية', shape: 'ellipse', cx: 29, cy: 152, rx: 9, ry: 7 },
  { id: 'b-l-hand', labelEn: 'Left hand (back)', labelAr: 'يد يسرى خلفية', shape: 'ellipse', cx: 101, cy: 152, rx: 9, ry: 7 },
  { id: 'b-r-thigh', labelEn: 'Right thigh (back)', labelAr: 'فخذ أيمن خلفي', shape: 'rect', x: 44, y: 136, width: 20, height: 50, radius: 6 },
  { id: 'b-l-thigh', labelEn: 'Left thigh (back)', labelAr: 'فخذ أيسر خلفي', shape: 'rect', x: 66, y: 136, width: 20, height: 50, radius: 6 },
  { id: 'b-r-knee', labelEn: 'Right knee (back)', labelAr: 'ركبة يمنى خلفية', shape: 'ellipse', cx: 54, cy: 192, rx: 10, ry: 9 },
  { id: 'b-l-knee', labelEn: 'Left knee (back)', labelAr: 'ركبة يسرى خلفية', shape: 'ellipse', cx: 76, cy: 192, rx: 10, ry: 9 },
  { id: 'b-r-calf', labelEn: 'Right calf', labelAr: 'بطة ساق يمنى', shape: 'rect', x: 45, y: 200, width: 18, height: 44, radius: 5 },
  { id: 'b-l-calf', labelEn: 'Left calf', labelAr: 'بطة ساق يسرى', shape: 'rect', x: 67, y: 200, width: 18, height: 44, radius: 5 },
  { id: 'b-r-heel', labelEn: 'Right heel', labelAr: 'كعب يمنى', shape: 'ellipse', cx: 54, cy: 252, rx: 11, ry: 7 },
  { id: 'b-l-heel', labelEn: 'Left heel', labelAr: 'كعب يسرى', shape: 'ellipse', cx: 76, cy: 252, rx: 11, ry: 7 },
];

@Component({
  selector: 'app-body-pain-map',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './body-pain-map.component.html',
})
export class BodyPainMapComponent {
  /** Fires on every change so the parent form can keep a live copy of the payload. */
  @Output() mapChange = new EventEmitter<BodyPainMapPayload>();

  readonly frontRegions = FRONT_REGIONS;
  readonly backRegions = BACK_REGIONS;

  private readonly selectionsMap = signal<Map<string, PainRegionSelection>>(new Map());
  readonly chiefComplaint = signal('');
  readonly showDebug = signal(false);

  readonly selectedList = computed(() => Array.from(this.selectionsMap().values()));
  readonly hasSelections = computed(() => this.selectionsMap().size > 0);
  readonly payload = computed<BodyPainMapPayload>(() => ({
    regions: this.selectedList(),
    chiefComplaint: this.chiefComplaint(),
  }));

  isSelected(id: string): boolean {
    return this.selectionsMap().has(id);
  }

  toggleRegion(region: BodyRegionDef): void {
    const next = new Map(this.selectionsMap());
    if (next.has(region.id)) {
      next.delete(region.id);
    } else {
      next.set(region.id, {
        id: region.id,
        labelEn: region.labelEn,
        labelAr: region.labelAr,
        severity: 5,
      });
    }
    this.selectionsMap.set(next);
    this.emitChange();
  }

  updateSeverity(id: string, rawValue: string): void {
    const next = new Map(this.selectionsMap());
    const existing = next.get(id);
    if (!existing) return;
    next.set(id, { ...existing, severity: Number(rawValue) });
    this.selectionsMap.set(next);
    this.emitChange();
  }

  removeRegion(id: string): void {
    const next = new Map(this.selectionsMap());
    next.delete(id);
    this.selectionsMap.set(next);
    this.emitChange();
  }

  onChiefComplaintInput(value: string): void {
    this.chiefComplaint.set(value);
    this.emitChange();
  }

  toggleDebug(): void {
    this.showDebug.update((v) => !v);
  }

  severityClass(v: number): string {
    if (v <= 3) return 'text-emerald-600';
    if (v <= 6) return 'text-amber-600';
    return 'text-rose-600';
  }

  private emitChange(): void {
    this.mapChange.emit(this.payload());
  }
}
