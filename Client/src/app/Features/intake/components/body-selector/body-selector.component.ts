import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectButtonModule } from 'primeng/selectbutton';
import { BodySvgComponent } from '../body-svg/body-svg.component';
import { PainPointService } from '../../services/pain-point.service';
import { PainPointDto } from '../../models';

@Component({
  selector: 'app-body-selector',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputNumberModule,
    SelectButtonModule,
    BodySvgComponent
  ],
  template: `
    <div class="flex flex-col lg:flex-row gap-4" role="group" aria-label="Body pain point selector">
      <div class="w-full lg:w-48 shrink-0">
        <app-body-svg
          [view]="currentView()"
          [points]="painPointService.painPoints()"
          [selectedIndex]="selectedIndex()"
          (areaClick)="onBodyClick($event)"
          (regionClick)="onRegionClick($event)"
          (viewChange)="currentView.set($event)" />
      </div>

      <div class="flex-1 min-w-0" role="list" aria-label="Marked pain points">
        @if (painPointService.painPoints().length === 0) {
          <div class="text-center py-8 lg:py-12 text-surface-400 border-2 border-dashed border-surface-200 rounded-lg" role="status">
            <i class="pi pi-hand-pointer text-2xl mb-2 block"></i>
            <p class="text-sm">Click on the body diagram to mark pain areas</p>
          </div>
        }

        @if (painPointService.painPoints().length > 0) {
          <div>
            <div class="flex items-center justify-between mb-2">
              <h4 class="font-semibold text-sm m-0" id="pain-points-heading">
                Pain Points ({{ painPointService.painPoints().length }})
              </h4>
              <p-button
                label="Clear All"
                icon="pi pi-trash"
                [text]="true"
                severity="danger"
                size="small"
                (onClick)="clearPoints()"
                [attr.aria-label]="'Clear all ' + painPointService.painPoints().length + ' pain points'" />
            </div>

            @for (point of painPointService.painPoints(); track $index) {
              <div class="flex flex-wrap items-center gap-2 mb-2 p-2 border border-surface-200 rounded-md bg-white"
                   role="listitem"
                   [class.ring-2]="$index === selectedIndex()"
                   [class.ring-primary-400]="$index === selectedIndex()"
                   (mouseenter)="selectedIndex.set($index)"
                   (mouseleave)="selectedIndex.set(null)"
                   (focusin)="selectedIndex.set($index)"
                   (focusout)="selectedIndex.set(null)"
                   tabindex="0">
                <span class="inline-block w-3 h-3 rounded-full shrink-0"
                      [style.background-color]="getIntensityColor(point.intensity)"
                      [attr.aria-hidden]="true">
                </span>
                <span class="text-xs text-surface-600 w-28 shrink-0" aria-label="Position">
                  {{ getPointSummary(point) }} ({{ point.x }}, {{ point.y }})
                </span>
                <label class="text-xs text-surface-500 shrink-0" [attr.for]="'intensity-' + $index">Intensity:</label>
                <p-inputNumber
                  [inputId]="'intensity-' + $index"
                  [(ngModel)]="point.intensity"
                  [min]="1"
                  [max]="10"
                  [showButtons]="true"
                  styleClass="!w-28"
                  (onInput)="updateIntensity($index, $event.value ?? 5)"
                  [attr.aria-label]="'Intensity for pain point ' + ($index + 1) + ', current value ' + point.intensity" />
                <div class="flex flex-wrap items-center gap-2 flex-1 min-w-[220px]">
                  <input
                    type="text"
                    pInputText
                    [ngModel]="point.anatomicalRegion || point.bodyPart || ''"
                    (ngModelChange)="updatePointDetail($index, 'anatomicalRegion', $event)"
                    placeholder="Anatomical region"
                    class="w-40 text-xs" />
                  <p-selectButton
                    [options]="sideOptions"
                    [ngModel]="point.side || 'bilateral'"
                    (ngModelChange)="updatePointDetail($index, 'side', $event)"
                    styleClass="p-selectbutton-sm" />
                  <input
                    type="text"
                    pInputText
                    [ngModel]="point.specificLocation || ''"
                    (ngModelChange)="updatePointDetail($index, 'specificLocation', $event)"
                    placeholder="Location detail"
                    class="w-36 text-xs" />
                </div>
                <p-button
                  icon="pi pi-times"
                  [text]="true"
                  severity="danger"
                  size="small"
                  (onClick)="removePoint($index)"
                  [attr.aria-label]="'Remove pain point ' + ($index + 1)" />
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
})
export class BodySelectorComponent {
  protected readonly painPointService = inject(PainPointService);
  readonly painPointsChange = output<PainPointDto[]>();

  readonly currentView = signal<'front' | 'back'>('front');
  readonly selectedIndex = signal<number | null>(null);
  readonly sideOptions = [
    { label: 'Left', value: 'left' },
    { label: 'Right', value: 'right' },
    { label: 'Both', value: 'bilateral' },
    { label: 'Midline', value: 'midline' }
  ];

  onBodyClick(coords: { x: number; y: number }): void {
    this.painPointService.addPoint({
      x: coords.x,
      y: coords.y,
      intensity: 5,
      anatomicalRegion: this.currentView() === 'front' ? 'Anterior' : 'Posterior',
      side: 'bilateral',
      specificLocation: 'Marked on body map',
      bodyPart: this.currentView()
    });
    this.selectedIndex.set(this.painPointService.painPoints().length - 1);
    this.emitChange();
  }

  onRegionClick(muscleId: string): void {
    const view = this.currentView();
    const regionName = muscleId.replace(/_l$|_r$/i, '').replace(/_/g, ' ');
    this.painPointService.addPoint({
      x: view === 'front' ? 100 : 100,
      y: view === 'front' ? 160 : 160,
      intensity: 5,
      anatomicalRegion: regionName.charAt(0).toUpperCase() + regionName.slice(1),
      side: muscleId.endsWith('_l') ? 'left' : muscleId.endsWith('_r') ? 'right' : 'bilateral',
      specificLocation: muscleId,
      bodyPart: view
    });
    this.selectedIndex.set(this.painPointService.painPoints().length - 1);
    this.emitChange();
  }

  removePoint(index: number): void {
    this.painPointService.removePoint(index);
    this.selectedIndex.set(null);
    this.emitChange();
  }

  updateIntensity(index: number, intensity: number): void {
    this.painPointService.updateIntensity(index, intensity);
    this.emitChange();
  }

  clearPoints(): void {
    this.painPointService.clear();
    this.selectedIndex.set(null);
    this.emitChange();
  }

  updatePointDetail(index: number, field: 'anatomicalRegion' | 'side' | 'specificLocation', value: string): void {
    const current = this.painPointService.painPoints()[index];
    if (!current) return;
    this.painPointService.updatePoint(index, { ...current, [field]: value });
    this.emitChange();
  }

  private emitChange(): void {
    this.painPointsChange.emit(this.painPointService.painPoints());
  }

  getPointSummary(point: PainPointDto): string {
    const region = point.anatomicalRegion || point.bodyPart || 'Anatomical region';
    const side = point.side ? ` • ${point.side}` : '';
    const location = point.specificLocation ? ` • ${point.specificLocation}` : '';
    return `${region}${side}${location}`;
  }

  getIntensityColor(intensity: number): string {
    if (intensity <= 3) return '#22c55e';
    if (intensity <= 6) return '#eab308';
    return '#ef4444';
  }
}
