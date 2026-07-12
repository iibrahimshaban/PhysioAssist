import { Component, computed, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { BodySvgComponent, MuscleRegionClick } from '../body-svg/body-svg.component';
import { PainPointService } from '../../services/pain-point.service';
import { PainPointDto } from '../../models';

@Component({
  selector: 'app-body-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, SliderModule, BodySvgComponent],
  template: `
    <div class="flex flex-col lg:flex-row gap-4" role="group" aria-label="Body pain point selector">
      <div class="w-full lg:w-48 shrink-0">
        <app-body-svg
          [view]="currentView()"
          [points]="pointsForCurrentView()"
          [selectedIndex]="selectedIndex()"
          (areaClick)="onBodyClick($event)"
          (regionClick)="onRegionClick($event)"
          (viewChange)="currentView.set($event)" />
      </div>

      <div class="flex-1 min-w-0" role="list" aria-label="Marked pain points">
        @if (painPointService.painPoints().length === 0) {
          <div class="text-center py-8 lg:py-12 text-surface-400 border-2 border-dashed border-surface-200 rounded-lg" role="status">
            <i class="pi pi-hand-pointer text-2xl mb-2 block"></i>
            <p class="text-sm">Tap the body diagram to mark where it hurts</p>
          </div>
        }

        @if (painPointService.painPoints().length > 0) {
          <div>
            <div class="flex items-center justify-between mb-3">
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
              <div class="flex flex-col gap-2 mb-3 p-3 border border-surface-200 rounded-lg bg-white"
                   role="listitem"
                   [class.ring-2]="$index === selectedIndex()"
                   [class.ring-primary-400]="$index === selectedIndex()"
                   (mouseenter)="selectedIndex.set($index)"
                   (mouseleave)="selectedIndex.set(null)"
                   (focusin)="selectedIndex.set($index)"
                   (focusout)="selectedIndex.set(null)"
                   tabindex="0">

                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2 min-w-0">
                    <span class="text-[10px] font-bold uppercase tracking-wider text-surface-400 shrink-0">
                      {{ point.bodyPart === 'front' ? 'Front' : 'Back' }}
                    </span>
                    @if (point.anatomicalRegion) {
                      <span class="font-semibold text-sm text-surface-800 truncate">{{ point.anatomicalRegion }}</span>
                    } @else {
                      <input
                        type="text"
                        pInputText
                        [ngModel]="point.specificLocation || ''"
                        (ngModelChange)="updatePointDetail($index, 'specificLocation', $event)"
                        placeholder="Add a label (optional)"
                        class="text-sm font-medium w-full" />
                    }
                  </div>
                  <p-button
                    icon="pi pi-times"
                    [text]="true"
                    severity="danger"
                    size="small"
                    (onClick)="removePoint($index)"
                    [attr.aria-label]="'Remove pain point ' + ($index + 1)" />
                </div>

                <div class="flex items-center gap-3">
                  <span class="inline-block w-2.5 h-2.5 rounded-full shrink-0"
                        [style.background-color]="getIntensityColor(point.intensity)"
                        [attr.aria-hidden]="true"></span>
                  <p-slider
                    [ngModel]="point.intensity"
                    (ngModelChange)="updateIntensity($index, $event)"
                    [min]="0"
                    [max]="10"
                    styleClass="flex-1"
                    [attr.aria-label]="'Intensity for ' + (point.anatomicalRegion || 'pain point ' + ($index + 1)) + ', current value ' + point.intensity" />
                  <span class="text-sm font-semibold w-10 text-right shrink-0" [style.color]="getIntensityColor(point.intensity)">
                    {{ point.intensity }}/10
                  </span>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`:host { display: block; }`]
})
export class BodySelectorComponent {
  protected readonly painPointService = inject(PainPointService);
  readonly painPointsChange = output<PainPointDto[]>();

  readonly currentView = signal<'front' | 'back'>('front');
  readonly selectedIndex = signal<number | null>(null);

  readonly pointsForCurrentView = computed(() =>
    this.painPointService.painPoints().filter(p => p.bodyPart === this.currentView())
  );

  onBodyClick(coords: { x: number; y: number }): void {
    this.painPointService.addPoint({
      x: coords.x,
      y: coords.y,
      intensity: 5,
      bodyPart: this.currentView()
      // no anatomicalRegion — free tap, patient can optionally label it
    });
    this.selectedIndex.set(this.pointsForCurrentView().length - 1);
    this.emitChange();
  }

  onRegionClick(region: MuscleRegionClick): void {
    const view = this.currentView();
    this.painPointService.addPoint({
      x: region.x,
      y: region.y,
      intensity: 5,
      anatomicalRegion: region.name,
      side: region.id.endsWith('_l') ? 'left' : region.id.endsWith('_r') ? 'right' : undefined,
      specificLocation: region.id,
      bodyPart: view
    });
    this.selectedIndex.set(this.pointsForCurrentView().length - 1);
    this.emitChange();
  }

  removePoint(viewIndex: number): void {
    const point = this.pointsForCurrentView()[viewIndex];
    const globalIndex = this.painPointService.painPoints().indexOf(point);
    if (globalIndex === -1) return;
    this.painPointService.removePoint(globalIndex);
    this.selectedIndex.set(null);
    this.emitChange();
  }

  updateIntensity(viewIndex: number, intensity: number): void {
    const point = this.pointsForCurrentView()[viewIndex];
    const globalIndex = this.painPointService.painPoints().indexOf(point);
    if (globalIndex === -1) return;
    this.painPointService.updateIntensity(globalIndex, intensity);
    this.emitChange();
  }

  clearPoints(): void {
    this.painPointService.clear();
    this.selectedIndex.set(null);
    this.emitChange();
  }

  updatePointDetail(viewIndex: number, field: 'anatomicalRegion' | 'specificLocation', value: string): void {
    const current = this.pointsForCurrentView()[viewIndex];
    if (!current) return;
    const globalIndex = this.painPointService.painPoints().indexOf(current);
    if (globalIndex === -1) return;
    this.painPointService.updatePoint(globalIndex, { ...current, [field]: value });
    this.emitChange();
  }

  private emitChange(): void {
    this.painPointsChange.emit(this.painPointService.painPoints());
  }

  getIntensityColor(intensity: number): string {
    if (intensity <= 3) return '#22c55e';
    if (intensity <= 6) return '#eab308';
    return '#ef4444';
  }
}