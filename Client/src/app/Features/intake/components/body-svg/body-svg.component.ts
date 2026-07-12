import {
  Component,
  input,
  output,
  signal,
  computed,
  ElementRef,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PainPointDto } from '../../models';

// COLORS is the single source of truth for valid category ids.
// Category is derived FROM this object (not the other way round) so that
// any typo in a region's `category` field is now a compile-time TS error
// instead of a silent runtime "undefined fill" bug.
const COLORS = {
  head:        { base: '#e74c3c', hover: '#c0392b', light: '#fadbd8' },
  neck:        { base: '#f39c12', hover: '#e67e22', light: '#fdebd0' },
  chest:       { base: '#e91e63', hover: '#c2185b', light: '#fce4ec' },
  back:        { base: '#9b59b6', hover: '#8e44ad', light: '#e8daef' },
  shoulders:   { base: '#3498db', hover: '#2980b9', light: '#d6eaf8' },
  arms:        { base: '#1abc9c', hover: '#16a085', light: '#d5f5e3' },
  forearms:    { base: '#2ecc71', hover: '#27ae60', light: '#d5f5e3' },
  abs:         { base: '#f1c40f', hover: '#f39c12', light: '#fef9e7' },
  hips:        { base: '#e67e22', hover: '#d35400', light: '#fdebd0' },
  glutes:      { base: '#d35400', hover: '#a04000', light: '#fdebd0' },
  quads:       { base: '#3498db', hover: '#2471a3', light: '#d6eaf8' },
  hamstrings:  { base: '#2980b9', hover: '#1a5276', light: '#d4e6f1' },
  calves:      { base: '#1abc9c', hover: '#148f77', light: '#d5f5e3' },
  legs:        { base: '#e67e22', hover: '#d35400', light: '#fdebd0' },
  feet:        { base: '#95a5a6', hover: '#7f8c8d', light: '#ecf0f1' },
} as const;

type Category = keyof typeof COLORS;

const CATEGORY_LABELS: Record<Category, { en: string; ar: string }> = {
  head:       { en: 'Head',       ar: 'الرأس' },
  neck:       { en: 'Neck',       ar: 'الرقبة' },
  chest:      { en: 'Chest',      ar: 'الصدر' },
  back:       { en: 'Back',       ar: 'الظهر' },
  shoulders:  { en: 'Shoulders',  ar: 'الكتفان' },
  arms:       { en: 'Arms',       ar: 'الذراعان' },
  forearms:   { en: 'Forearms',   ar: 'الساعدان' },
  abs:        { en: 'Abdomen',    ar: 'البطن' },
  hips:       { en: 'Hips',       ar: 'الوركان' },
  glutes:     { en: 'Glutes',     ar: 'الأرداف' },
  quads:      { en: 'Quadriceps', ar: 'الفخذ الأمامي' },
  hamstrings: { en: 'Hamstrings', ar: 'أوتار الركبة' },
  calves:     { en: 'Calves',     ar: 'الساق' },
  legs:       { en: 'Legs',       ar: 'الساقان' },
  feet:       { en: 'Feet',       ar: 'القدمان' },
};

export interface MuscleRegion {
  id: string;
  name: string;
  nameAr: string;
  category: Category;
  cx: number;
  cy: number;
  rx: number;
  ry: number;
}

export interface JointDot {
  x: number;
  y: number;
  label: string;
  labelAr: string;
}

export interface MuscleRegionClick {
  id: string;
  x: number;
  y: number;
  name: string;
  nameAr: string;
}

// 20 clickable regions, front view.
const FRONT_REGIONS: MuscleRegion[] = [
  { id: 'head',        name: 'Head',              nameAr: 'الرأس',                category: 'head',      cx: 100, cy: 30,  rx: 16, ry: 18 },
  { id: 'neck',        name: 'Neck',              nameAr: 'الرقبة',               category: 'neck',      cx: 100, cy: 58,  rx: 10, ry: 8  },
  { id: 'shoulder_l',  name: 'Left Shoulder',     nameAr: 'الكتف الأيسر',         category: 'shoulders', cx: 60,  cy: 78,  rx: 14, ry: 10 },
  { id: 'shoulder_r',  name: 'Right Shoulder',    nameAr: 'الكتف الأيمن',         category: 'shoulders', cx: 140, cy: 78,  rx: 14, ry: 10 },
  { id: 'chest',       name: 'Chest',             nameAr: 'الصدر',                category: 'chest',     cx: 100, cy: 100, rx: 28, ry: 18 },
  { id: 'arm_l',       name: 'Left Arm',          nameAr: 'الذراع الأيسر',        category: 'arms',      cx: 48,  cy: 145, rx: 11, ry: 28 },
  { id: 'arm_r',       name: 'Right Arm',         nameAr: 'الذراع الأيمن',        category: 'arms',      cx: 152, cy: 145, rx: 11, ry: 28 },
  { id: 'abdomen',     name: 'Abdomen',           nameAr: 'البطن',                category: 'abs',       cx: 100, cy: 155, rx: 22, ry: 22 },
  { id: 'forearm_l',   name: 'Left Forearm',      nameAr: 'الساعد الأيسر',        category: 'forearms',  cx: 43,  cy: 200, rx: 9,  ry: 26 },
  { id: 'forearm_r',   name: 'Right Forearm',     nameAr: 'الساعد الأيمن',        category: 'forearms',  cx: 157, cy: 200, rx: 9,  ry: 26 },
  { id: 'hip_l',       name: 'Left Hip',          nameAr: 'الورك الأيسر',         category: 'hips',      cx: 78,  cy: 215, rx: 14, ry: 14 },
  { id: 'hip_r',       name: 'Right Hip',         nameAr: 'الورك الأيمن',         category: 'hips',      cx: 122, cy: 215, rx: 14, ry: 14 },
  { id: 'thigh_l',     name: 'Left Quadriceps',   nameAr: 'الفخذ الأمامي الأيسر', category: 'quads',     cx: 78,  cy: 290, rx: 16, ry: 40 },
  { id: 'thigh_r',     name: 'Right Quadriceps',  nameAr: 'الفخذ الأمامي الأيمن', category: 'quads',     cx: 122, cy: 290, rx: 16, ry: 40 },
  { id: 'knee_l',      name: 'Left Knee',         nameAr: 'الركبة اليسرى',        category: 'legs',      cx: 78,  cy: 362, rx: 11, ry: 10 },
  { id: 'knee_r',      name: 'Right Knee',        nameAr: 'الركبة اليمنى',        category: 'legs',      cx: 122, cy: 362, rx: 11, ry: 10 },
  { id: 'shin_l',      name: 'Left Shin',         nameAr: 'الساق الأمامية اليسرى', category: 'legs',     cx: 78,  cy: 410, rx: 10, ry: 32 },
  { id: 'shin_r',      name: 'Right Shin',        nameAr: 'الساق الأمامية اليمنى', category: 'legs',     cx: 122, cy: 410, rx: 10, ry: 32 },
  { id: 'foot_l',      name: 'Left Foot',         nameAr: 'القدم اليسرى',         category: 'feet',      cx: 78,  cy: 456, rx: 11, ry: 14 },
  { id: 'foot_r',      name: 'Right Foot',        nameAr: 'القدم اليمنى',         category: 'feet',      cx: 122, cy: 456, rx: 11, ry: 14 },
];

// 20 clickable regions, back view.
const BACK_REGIONS: MuscleRegion[] = [
  { id: 'head_b',       name: 'Head',            nameAr: 'الرأس',                 category: 'head',       cx: 100, cy: 30,  rx: 16, ry: 18 },
  { id: 'neck_b',       name: 'Neck',            nameAr: 'الرقبة',                category: 'neck',       cx: 100, cy: 58,  rx: 10, ry: 8  },
  { id: 'shoulder_l_b', name: 'Left Shoulder',   nameAr: 'الكتف الأيسر',          category: 'shoulders',  cx: 60,  cy: 78,  rx: 14, ry: 10 },
  { id: 'shoulder_r_b', name: 'Right Shoulder',  nameAr: 'الكتف الأيمن',          category: 'shoulders',  cx: 140, cy: 78,  rx: 14, ry: 10 },
  { id: 'back',         name: 'Back',            nameAr: 'الظهر',                 category: 'back',       cx: 100, cy: 130, rx: 26, ry: 42 },
  { id: 'arm_l_b',      name: 'Left Arm',        nameAr: 'الذراع الأيسر',         category: 'arms',       cx: 48,  cy: 145, rx: 11, ry: 28 },
  { id: 'arm_r_b',      name: 'Right Arm',       nameAr: 'الذراع الأيمن',         category: 'arms',       cx: 152, cy: 145, rx: 11, ry: 28 },
  { id: 'forearm_l_b',  name: 'Left Forearm',    nameAr: 'الساعد الأيسر',         category: 'forearms',   cx: 43,  cy: 200, rx: 9,  ry: 26 },
  { id: 'forearm_r_b',  name: 'Right Forearm',   nameAr: 'الساعد الأيمن',         category: 'forearms',   cx: 157, cy: 200, rx: 9,  ry: 26 },
  { id: 'glute_l',      name: 'Left Glute',      nameAr: 'الرداف الأيسر',         category: 'glutes',     cx: 78,  cy: 215, rx: 14, ry: 14 },
  { id: 'glute_r',      name: 'Right Glute',     nameAr: 'الرداف الأيمن',         category: 'glutes',     cx: 122, cy: 215, rx: 14, ry: 14 },
  { id: 'thigh_l_b',    name: 'Left Hamstring',  nameAr: 'وتر الركبة الأيسر',     category: 'hamstrings', cx: 78,  cy: 290, rx: 16, ry: 40 },
  { id: 'thigh_r_b',    name: 'Right Hamstring', nameAr: 'وتر الركبة الأيمن',     category: 'hamstrings', cx: 122, cy: 290, rx: 16, ry: 40 },
  { id: 'knee_l_b',     name: 'Left Knee',       nameAr: 'الركبة اليسرى',         category: 'legs',       cx: 78,  cy: 362, rx: 11, ry: 10 },
  { id: 'knee_r_b',     name: 'Right Knee',      nameAr: 'الركبة اليمنى',         category: 'legs',       cx: 122, cy: 362, rx: 11, ry: 10 },
  { id: 'calf_l',       name: 'Left Calf',       nameAr: 'الساق الخلفية اليسرى',  category: 'calves',     cx: 78,  cy: 410, rx: 10, ry: 32 },
  { id: 'calf_r',       name: 'Right Calf',      nameAr: 'الساق الخلفية اليمنى',  category: 'calves',     cx: 122, cy: 410, rx: 10, ry: 32 },
  { id: 'foot_l_b',     name: 'Left Foot',       nameAr: 'القدم اليسرى',          category: 'feet',       cx: 78,  cy: 456, rx: 11, ry: 14 },
  { id: 'foot_r_b',     name: 'Right Foot',      nameAr: 'القدم اليمنى',          category: 'feet',       cx: 122, cy: 456, rx: 11, ry: 14 },
];

@Component({
  selector: 'app-body-svg',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="body-chart-wrapper" [dir]="lang() === 'ar' ? 'rtl' : 'ltr'">

      <div class="toolbar">
        <button type="button" class="icon-btn" (click)="toggleLang()"
          [attr.aria-label]="lang() === 'en' ? 'Switch to Arabic' : 'التبديل إلى الإنجليزية'"
          title="EN / عربي">{{ lang() === 'en' ? 'ع' : 'EN' }}</button>

        <div class="search-box">
          <input type="text" [(ngModel)]="searchTerm" (ngModelChange)="onSearchChange($event)"
            [placeholder]="lang() === 'en' ? 'Search a muscle…' : 'ابحث عن عضلة…'"
            [attr.aria-label]="lang() === 'en' ? 'Search muscle regions' : 'ابحث عن مناطق العضلات'" />
          @if (searchTerm) {
            <button type="button" class="clear-btn" (click)="clearSearch()" aria-label="Clear search">×</button>
          }
        </div>

        <button type="button" class="view-toggle"
          (click)="viewChange.emit(view() === 'front' ? 'back' : 'front')">
          {{ view() === 'front'
            ? (lang() === 'en' ? '⟲ Back View' : '⟲ الظهر')
            : (lang() === 'en' ? '⟲ Front View' : '⟲ الأمام') }}
        </button>
      </div>

      <div class="svg-shell">
        <svg #svgRef viewBox="0 0 200 500" xmlns="http://www.w3.org/2000/svg"
          [style.transform]="'scale(' + zoom() + ')'"
          (click)="onSvgClick($event)" role="img"
          [attr.aria-label]="view() === 'front'
            ? (lang() === 'en' ? 'Front view body diagram for pain identification' : 'مخطط الجسم الأمامي لتحديد الألم')
            : (lang() === 'en' ? 'Back view body diagram for pain identification' : 'مخطط الجسم الخلفي لتحديد الألم')">
          <defs>
            <linearGradient id="bodyGrad" x1="15%" y1="0%" x2="85%" y2="100%">
              <stop offset="0%" stop-color="#f3e6d4" />
              <stop offset="45%" stop-color="#d9bd9c" />
              <stop offset="100%" stop-color="#a67f5c" />
            </linearGradient>
            <filter id="soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="3.2" />
            </filter>
            <filter id="pulse" x="-80%" y="-80%" width="260%" height="260%">
              <feGaussianBlur stdDeviation="1.6" />
            </filter>
          </defs>

          <ellipse cx="100" cy="480" rx="50" ry="9" fill="#3d2c1c" opacity="0.16" filter="url(#soft)" />

          @for (region of currentMuscles(); track region.id) {
            <ellipse
              [attr.cx]="region.cx" [attr.cy]="region.cy"
              [attr.rx]="region.rx" [attr.ry]="region.ry"
              [attr.fill]="regionFill(region.id)" [attr.fill-opacity]="regionOpacity(region.id)"
              [attr.data-region-id]="region.id" tabindex="0" role="button"
              [attr.aria-label]="lang() === 'en' ? region.name : region.nameAr"
              (mouseenter)="onRegionEnter(region.id)" (mouseleave)="onRegionLeave()"
              (touchstart)="onRegionEnter(region.id)" (touchend)="onRegionLeave()"
              (focus)="onRegionEnter(region.id)" (blur)="onRegionLeave()"
              (click)="onMuscleClick(region.id, $event)"
              (keydown.enter)="onMuscleClick(region.id, $event)"
              (keydown.space)="onMuscleClick(region.id, $event)"
              stroke="#5c4632" stroke-width="0.6" stroke-opacity="0.4" class="muscle-path">
              <title>{{ lang() === 'en' ? region.name : region.nameAr }}</title>
            </ellipse>
          }

          <g class="body-outline" pointer-events="none">
            <ellipse cx="100" cy="30" rx="19" ry="23" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.3" />
            <ellipse cx="80" cy="32" rx="3.5" ry="6" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
            <ellipse cx="120" cy="32" rx="3.5" ry="6" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
            <path d="M91,50 L91,64 Q100,70 109,64 L109,50 Z" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.2" />
            <path d="M91,64 Q70,66 60,78 L54,120 Q52,132 58,140 L61,150 Q56,175 58,200 Q60,215 66,225 Q64,240 60,255 L57,268 Q56,272 60,273 Q64,274 66,270 L72,250 Q76,232 78,215 Q84,222 100,223 Q116,222 122,215 Q124,232 128,250 L134,270 Q136,274 140,273 Q144,272 143,268 L140,255 Q136,240 134,225 Q140,215 142,200 Q144,175 139,150 L142,140 Q148,132 146,120 L140,78 Q130,66 109,64 Q100,70 91,64 Z" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.3" />
            <ellipse cx="58" cy="270" rx="6" ry="8" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
            <ellipse cx="142" cy="270" rx="6" ry="8" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
            <path d="M66,225 Q60,245 62,265 Q64,282 74,290 L74,300 L126,300 L126,290 Q136,282 138,265 Q140,245 134,225 Q116,238 100,238 Q84,238 66,225 Z" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.3" />
            <path d="M74,290 Q68,320 70,355 Q71,385 76,410 Q77,425 74,438 L72,458 Q71,466 78,467 L88,467 Q92,466 91,458 L90,435 Q92,410 94,385 Q96,355 97,300 L74,300 Z" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.3" />
            <path d="M126,290 Q132,320 130,355 Q129,385 124,410 Q123,425 126,438 L128,458 Q129,466 122,467 L112,467 Q108,466 109,458 L110,435 Q108,410 106,385 Q104,355 103,300 L126,300 Z" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1.3" />
            <ellipse cx="80" cy="474" rx="11" ry="6.5" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
            <ellipse cx="120" cy="474" rx="11" ry="6.5" fill="url(#bodyGrad)" stroke="#5c4632" stroke-width="1" />
          </g>

          <g pointer-events="none">
            @for (j of jointPoints; track j.label) {
              <circle [attr.cx]="j.x" [attr.cy]="j.y" r="2.6" fill="#3d7a9c" stroke="#ffffff" stroke-width="0.6" opacity="0.85">
                <title>{{ lang() === 'en' ? j.label : j.labelAr }}</title>
              </circle>
            }
          </g>

          @for (point of points(); track $index) {
            <g [attr.aria-label]="(lang() === 'en' ? 'Pain point, intensity ' : 'نقطة ألم، الشدة ') + point.intensity + '/10'"
              (click)="onPointClick($index, $event)" (dblclick)="onPointRemove($index, $event)"
              (touchend)="onPointTouchEnd($index, $event)" style="cursor:pointer">
              @if ($index === selectedIndex()) {
                <circle [attr.cx]="point.x" [attr.cy]="point.y" r="13" fill="none"
                  [attr.stroke]="getIntensityColor(point.intensity)" stroke-width="2" opacity="0.45"
                  filter="url(#pulse)" class="pulse-ring" />
              }
              <circle [attr.cx]="point.x" [attr.cy]="point.y"
                [attr.fill]="getIntensityColor(point.intensity)" stroke="#fff"
                [attr.stroke-width]="$index === selectedIndex() ? 3 : 2"
                [attr.r]="$index === selectedIndex() ? 8 : 6" class="point-marker" />
              <title>{{ (lang() === 'en' ? 'Point ' : 'نقطة ') + ($index + 1) + ': ' + (lang() === 'en' ? 'Intensity ' : 'الشدة ') + point.intensity + '/10' + (point.bodyPart ? ' (' + point.bodyPart + ')' : '') }} — {{ lang() === 'en' ? 'double-tap to remove' : 'انقر مرتين للإزالة' }}</title>
            </g>
          }
        </svg>

        <div class="zoom-controls">
          <button type="button" (click)="zoomOut()" aria-label="Zoom out">−</button>
          <span class="zoom-level">{{ (zoom() * 100).toFixed(0) }}%</span>
          <button type="button" (click)="zoomIn()" aria-label="Zoom in">+</button>
        </div>
      </div>

      <div class="sr-only" role="status" aria-live="polite">{{ liveMessage() }}</div>

      <div class="hovered-banner" [class.visible]="!!hoveredName()">
        <span class="hovered-dot" [style.background]="hoveredColor()"></span>
        {{ hoveredName() }}
      </div>

      <div class="category-legend">
        @for (cat of categories(); track cat.key) {
          <button type="button" class="cat-chip" [class.active]="activeCategory() === cat.key"
            [style.--chip-color]="cat.color" (click)="onCategoryTap(cat.key)">
            {{ cat.label }}
          </button>
        }
      </div>

      <div class="chart-footer">
        <div class="summary">
          <strong>{{ points().length }}</strong>
          {{ lang() === 'en' ? (points().length === 1 ? 'point' : 'points') : 'نقاط' }}
          @if (points().length > 0) {
            <span class="avg">· {{ lang() === 'en' ? 'avg' : 'المتوسط' }} {{ averageIntensity() }}/10</span>
          }
        </div>
        <div class="legend">
          <span class="legend-item"><i class="dot mild"></i>{{ lang() === 'en' ? 'Mild' : 'خفيف' }}</span>
          <span class="legend-item"><i class="dot moderate"></i>{{ lang() === 'en' ? 'Moderate' : 'متوسط' }}</span>
          <span class="legend-item"><i class="dot severe"></i>{{ lang() === 'en' ? 'Severe' : 'شديد' }}</span>
        </div>
      </div>

      <p class="hint">{{ lang() === 'en'
        ? 'Tap a spot to log pain · double-tap a marker to remove it · use search or the category chips to find a muscle fast.'
        : 'انقر على أي منطقة لتسجيل الألم · انقر مرتين على العلامة لإزالتها · استخدم البحث أو الفئات للعثور على العضلة بسرعة.' }}</p>
    </div>
  `,
  styles: [`
    :host { display: block; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
    .body-chart-wrapper { display: flex; flex-direction: column; align-items: center; gap: 12px; background: #ffffff; border: 1px solid #e7e2da; border-radius: 16px; padding: 16px; }
    .toolbar { display: flex; align-items: center; gap: 8px; width: 100%; max-width: 340px; flex-wrap: wrap; }
    .icon-btn { border: 1px solid #cbd5e1; background: #f8fafc; color: #334155; font-size: 13px; font-weight: 600; width: 34px; height: 34px; border-radius: 50%; cursor: pointer; flex-shrink: 0; }
    .icon-btn:active { background: #e2e8f0; }
    .search-box { position: relative; flex: 1; min-width: 120px; }
    .search-box input { width: 100%; box-sizing: border-box; border: 1px solid #cbd5e1; border-radius: 20px; padding: 7px 30px 7px 12px; font-size: 13px; outline: none; }
    .search-box input:focus { border-color: #3498db; }
    .clear-btn { position: absolute; right: 6px; top: 50%; transform: translateY(-50%); border: none; background: transparent; color: #94a3b8; font-size: 16px; cursor: pointer; line-height: 1; }
    .view-toggle { border: 1px solid #cbd5e1; background: #fff; color: #334155; font-size: 12.5px; padding: 7px 12px; border-radius: 20px; cursor: pointer; white-space: nowrap; }
    .view-toggle:active { background: #f1f5f9; }
    .svg-shell { position: relative; width: 100%; max-width: 340px; display: flex; justify-content: center; overflow: hidden; border-radius: 12px; background: linear-gradient(180deg,#fafafa,#f3f4f6); }
    svg { width: 100%; max-width: 340px; cursor: crosshair; touch-action: manipulation; transition: transform 0.2s ease; transform-origin: center top; }
    .muscle-path { cursor: pointer; transition: fill-opacity 0.15s ease; outline: none; }
    .muscle-path:hover, .muscle-path:focus { fill-opacity: 0.6 !important; }
    .muscle-path:focus-visible { stroke: #2563eb; stroke-width: 1.4; stroke-opacity: 1; }
    .point-marker { transition: r 0.15s ease, stroke-width 0.15s ease; }
    .pulse-ring { animation: pulseAnim 1.6s ease-in-out infinite; }
    @keyframes pulseAnim { 0% { r: 10; opacity: 0.5; } 70% { r: 17; opacity: 0; } 100% { r: 17; opacity: 0; } }
    .zoom-controls { position: absolute; bottom: 8px; right: 8px; display: flex; align-items: center; gap: 4px; background: rgba(255,255,255,0.9); border: 1px solid #e2e8f0; border-radius: 20px; padding: 3px 6px; font-size: 11px; color: #475569; }
    .zoom-controls button { border: none; background: #f1f5f9; width: 22px; height: 22px; border-radius: 50%; font-size: 14px; line-height: 1; cursor: pointer; color: #334155; }
    .zoom-controls button:active { background: #e2e8f0; }
    .zoom-level { min-width: 34px; text-align: center; }
    .hovered-banner { font-size: 12.5px; color: #64748b; min-height: 16px; opacity: 0; transition: opacity 0.15s ease; display: flex; align-items: center; gap: 6px; }
    .hovered-banner.visible { opacity: 1; }
    .hovered-dot { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
    .category-legend { display: flex; flex-wrap: wrap; gap: 6px; justify-content: center; max-width: 340px; }
    .cat-chip { border: 1px solid #e2e8f0; background: #fff; color: #475569; font-size: 11px; padding: 4px 10px; border-radius: 14px; cursor: pointer; position: relative; }
    .cat-chip::before { content: ''; display: inline-block; width: 7px; height: 7px; border-radius: 50%; background: var(--chip-color); margin-inline-end: 5px; }
    .cat-chip.active { background: #eff6ff; border-color: #93c5fd; color: #1d4ed8; font-weight: 600; }
    .chart-footer { display: flex; align-items: center; justify-content: space-between; width: 100%; max-width: 340px; gap: 12px; flex-wrap: wrap; }
    .summary { font-size: 12.5px; color: #334155; }
    .summary .avg { color: #64748b; }
    .legend { display: flex; gap: 10px; font-size: 12px; color: #64748b; }
    .legend-item { display: inline-flex; align-items: center; gap: 4px; }
    .dot { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
    .dot.mild { background: #22c55e; }
    .dot.moderate { background: #eab308; }
    .dot.severe { background: #ef4444; }
    .hint { font-size: 11px; color: #94a3b8; text-align: center; max-width: 340px; margin: 0; line-height: 1.5; }
    .sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; border: 0; }
  `],
})
export class BodySvgComponent implements AfterViewInit {
  @ViewChild('svgRef') svgRef?: ElementRef<SVGSVGElement>;

  readonly view = input<'front' | 'back'>('front');
  readonly points = input<PainPointDto[]>([]);
  readonly selectedIndex = input<number | null>(null);

  readonly areaClick = output<{ x: number; y: number }>();
  readonly viewChange = output<'front' | 'back'>();
  readonly regionClick = output<MuscleRegionClick>();
  readonly pointSelect = output<number>();
  readonly pointRemove = output<number>();

  protected readonly lang = signal<'en' | 'ar'>('en');
  protected readonly zoom = signal(1);
  protected readonly hoveredId = signal<string | null>(null);
  protected readonly hoveredName = signal('');
  protected readonly hoveredColor = signal('');
  protected readonly liveMessage = signal('');
  protected readonly activeCategory = signal<Category | null>(null);
  protected searchTerm = '';
  protected readonly matchedIds = signal<Set<string>>(new Set());

  readonly currentMuscles = computed<MuscleRegion[]>(() =>
    this.view() === 'front' ? FRONT_REGIONS : BACK_REGIONS
  );

  readonly categories = computed(() => {
    const present = new Set<Category>();
    for (const region of this.currentMuscles()) present.add(region.category);
    return Array.from(present).map(key => ({
      key,
      label: this.lang() === 'en' ? CATEGORY_LABELS[key].en : CATEGORY_LABELS[key].ar,
      color: COLORS[key].base,
    }));
  });

  readonly averageIntensity = computed(() => {
    const pts = this.points();
    if (!pts.length) return 0;
    const sum = pts.reduce((acc, p) => acc + p.intensity, 0);
    return Math.round((sum / pts.length) * 10) / 10;
  });

  protected readonly jointPoints: readonly JointDot[] = [
    { x: 62, y: 79, label: 'AC joint L', labelAr: 'مفصل الترقوة الأيسر' },
    { x: 138, y: 79, label: 'AC joint R', labelAr: 'مفصل الترقوة الأيمن' },
    { x: 56, y: 168, label: 'Elbow L', labelAr: 'الكوع الأيسر' },
    { x: 144, y: 168, label: 'Elbow R', labelAr: 'الكوع الأيمن' },
    { x: 58, y: 258, label: 'Wrist L', labelAr: 'الرسغ الأيسر' },
    { x: 142, y: 258, label: 'Wrist R', labelAr: 'الرسغ الأيمن' },
    { x: 63, y: 231, label: 'Hip L', labelAr: 'الورك الأيسر' },
    { x: 137, y: 231, label: 'Hip R', labelAr: 'الورك الأيمن' },
    { x: 83, y: 358, label: 'Knee L', labelAr: 'الركبة اليسرى' },
    { x: 117, y: 358, label: 'Knee R', labelAr: 'الركبة اليمنى' },
    { x: 80, y: 458, label: 'Ankle L', labelAr: 'الكاحل الأيسر' },
    { x: 120, y: 458, label: 'Ankle R', labelAr: 'الكاحل الأيمن' },
  ];

  ngAfterViewInit(): void {}

  toggleLang(): void {
    this.lang.set(this.lang() === 'en' ? 'ar' : 'en');
  }

  zoomIn(): void {
    this.zoom.set(Math.min(2, Math.round((this.zoom() + 0.15) * 100) / 100));
  }

  zoomOut(): void {
    this.zoom.set(Math.max(0.7, Math.round((this.zoom() - 0.15) * 100) / 100));
  }

  onSearchChange(term: string): void {
    const q = term.trim().toLowerCase();
    if (!q) {
      this.matchedIds.set(new Set());
      return;
    }
    const matches = this.currentMuscles()
      .filter(m => m.name.toLowerCase().includes(q) || m.nameAr.includes(term.trim()))
      .map(m => m.id);
    this.matchedIds.set(new Set(matches));
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.matchedIds.set(new Set());
  }

  /** Chips filter/highlight regions by category — they never add pain points themselves. */
  toggleCategory(cat: Category): void {
    this.activeCategory.set(this.activeCategory() === cat ? null : cat);
  }

  onCategoryTap(category: Category): void {
    this.toggleCategory(category);
  }

  regionFill(id: string): string {
    const muscle = this.currentMuscles().find(m => m.id === id);
    if (!muscle) return 'transparent';
    const colors = COLORS[muscle.category];
    return this.hoveredId() === id ? colors.hover : colors.base;
  }

  regionOpacity(id: string): number {
    const searching = this.matchedIds().size > 0;
    const filtering = this.activeCategory() !== null;
    const muscle = this.currentMuscles().find(m => m.id === id);
    if (!muscle) return 0.35;
    if (searching) return this.matchedIds().has(id) ? 0.75 : 0.08;
    if (filtering) return muscle.category === this.activeCategory() ? 0.7 : 0.08;
    return this.hoveredId() === id ? 0.55 : 0.35;
  }

  onRegionEnter(id: string): void {
    this.hoveredId.set(id);
    const muscle = this.currentMuscles().find(m => m.id === id);
    const label = muscle ? (this.lang() === 'en' ? muscle.name : muscle.nameAr) : '';
    this.hoveredName.set(label);
    this.hoveredColor.set(muscle ? COLORS[muscle.category].base : '');
    if (label) this.liveMessage.set(label);
  }

  onRegionLeave(): void {
    this.hoveredId.set(null);
    this.hoveredName.set('');
  }

  onMuscleClick(id: string, event: Event): void {
    event.stopPropagation();
    const region = this.currentMuscles().find(r => r.id === id);
    if (!region) return;
    this.regionClick.emit({ id: region.id, x: region.cx, y: region.cy, name: region.name, nameAr: region.nameAr });
    this.liveMessage.set((this.lang() === 'en' ? 'Selected ' : 'تم اختيار ') + (this.lang() === 'en' ? region.name : region.nameAr));
  }

  onPointClick(index: number, event: Event): void {
    event.stopPropagation();
    this.pointSelect.emit(index);
  }

  onPointRemove(index: number, event: Event): void {
    event.stopPropagation();
    this.pointRemove.emit(index);
    this.liveMessage.set(this.lang() === 'en' ? 'Point removed' : 'تمت إزالة النقطة');
  }

  private lastTouchTime = 0;
  private lastTouchIndex = -1;
  onPointTouchEnd(index: number, event: Event): void {
    event.stopPropagation();
    const now = Date.now();
    if (this.lastTouchIndex === index && now - this.lastTouchTime < 350) {
      this.onPointRemove(index, event);
      this.lastTouchIndex = -1;
      return;
    }
    this.lastTouchTime = now;
    this.lastTouchIndex = index;
  }

  onSvgClick(event: MouseEvent): void {
    const target = event.target as SVGElement;
    if (target.closest('.muscle-path')) return;
    const svgElement = event.currentTarget as SVGSVGElement;
    const rect = svgElement.getBoundingClientRect();
    const viewBoxWidth = 200;
    const viewBoxHeight = 500;
    const x = Math.round(((event.clientX - rect.left) / rect.width) * viewBoxWidth);
    const y = Math.round(((event.clientY - rect.top) / rect.height) * viewBoxHeight);
    this.areaClick.emit({ x, y });
  }

  getIntensityColor(intensity: number): string {
    if (intensity <= 3) return '#22c55e';
    if (intensity <= 6) return '#eab308';
    return '#ef4444';
  }
}