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

export interface PainPointDto {
  x: number;
  y: number;
  intensity: number;
  bodyPart?: string;
}

export interface MuscleRegion {
  id: string;
  name: string;
  nameAr: string;
  category:
    | 'head' | 'neck' | 'chest' | 'back' | 'shoulders' | 'arms' | 'forearms'
    | 'abs' | 'hips' | 'glutes' | 'quads' | 'hamstrings' | 'calves' | 'legs' | 'feet';
  color: string;
  colorHover: string;
  frontPath: string;
  backPath: string;
}

export interface JointDot {
  x: number;
  y: number;
  label: string;
  labelAr: string;
}

type Category = MuscleRegion['category'];

const COLORS: Record<Category, { base: string; hover: string; light: string }> = {
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
};

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

const FRONT_MUSCLES: MuscleRegion[] = [
  { id: 'temporalis', name: 'Temporalis', nameAr: 'عضلة صدغية', category: 'head', color: COLORS.head.base, colorHover: COLORS.head.hover,
    frontPath: 'M78,18 Q82,12 86,18 Q84,26 80,26 Z M114,18 Q118,12 122,18 Q120,26 116,26 Z', backPath: '' },
  { id: 'masseter', name: 'Masseter', nameAr: 'عضلة الماضغة', category: 'head', color: COLORS.head.base, colorHover: COLORS.head.hover,
    frontPath: 'M76,28 Q80,22 84,28 Q82,36 78,36 Z M116,28 Q120,22 124,28 Q122,36 118,36 Z', backPath: '' },
  { id: 'sternocleidomastoid', name: 'Sternocleidomastoid', nameAr: 'العضلة القصية الترقوية الخشائية', category: 'neck', color: COLORS.neck.base, colorHover: COLORS.neck.hover,
    frontPath: 'M82,52 L92,68 M118,52 L108,68', backPath: '' },
  { id: 'trapezius_upper', name: 'Upper Trapezius', nameAr: 'الجزء العلوي من شبه المنحرف', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: 'M62,72 Q72,66 100,64 Q128,66 138,72 Q134,78 128,80 Q100,74 72,80 Q66,78 62,72 Z', backPath: '' },
  { id: 'deltoid_l', name: 'Left Deltoid', nameAr: 'العضلة الدالية اليسرى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: 'M54,80 Q50,88 48,100 Q46,112 48,120 Q52,128 56,124 Q60,110 60,92 Q60,82 54,80 Z', backPath: '' },
  { id: 'deltoid_r', name: 'Right Deltoid', nameAr: 'العضلة الدالية اليمنى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: 'M146,80 Q150,88 152,100 Q154,112 152,120 Q148,128 144,124 Q140,110 140,92 Q140,82 146,80 Z', backPath: '' },
  { id: 'pectoralis_major', name: 'Pectoralis Major', nameAr: 'العضلة الصدرية الكبيرة', category: 'chest', color: COLORS.chest.base, colorHover: COLORS.chest.hover,
    frontPath: 'M66,86 Q82,92 100,88 Q118,92 134,86 Q136,100 132,114 Q118,122 100,124 Q82,122 68,114 Q64,100 66,86 Z', backPath: '' },
  { id: 'serratus_anterior', name: 'Serratus Anterior', nameAr: 'العضلة المنشارية الأمامية', category: 'chest', color: COLORS.chest.light, colorHover: COLORS.chest.hover,
    frontPath: 'M62,114 Q58,124 58,136 Q60,144 64,142 Q66,132 66,122 Z M138,114 Q142,124 142,136 Q140,144 136,142 Q134,132 134,122 Z', backPath: '' },
  { id: 'biceps_l', name: 'Left Biceps', nameAr: 'العضلة ذات الرأسين اليسرى', category: 'arms', color: COLORS.arms.base, colorHover: COLORS.arms.hover,
    frontPath: 'M48,124 Q44,136 42,148 Q41,158 44,162 Q48,158 50,148 Q52,136 54,128 Z', backPath: '' },
  { id: 'biceps_r', name: 'Right Biceps', nameAr: 'العضلة ذات الرأسين اليمنى', category: 'arms', color: COLORS.arms.base, colorHover: COLORS.arms.hover,
    frontPath: 'M152,124 Q156,136 158,148 Q159,158 156,162 Q152,158 150,148 Q148,136 146,128 Z', backPath: '' },
  { id: 'brachialis', name: 'Brachialis', nameAr: 'العضلة العضدية', category: 'arms', color: COLORS.arms.hover, colorHover: COLORS.arms.hover,
    frontPath: 'M44,162 Q42,170 42,178 Q44,172 50,168 Z M156,162 Q158,170 158,178 Q156,172 150,168 Z', backPath: '' },
  { id: 'forearm_l', name: 'Left Forearm', nameAr: 'الساعد الأيسر', category: 'forearms', color: COLORS.forearms.base, colorHover: COLORS.forearms.hover,
    frontPath: 'M42,178 Q40,188 40,200 Q40,212 42,220 Q46,218 48,206 Q48,192 46,180 Z', backPath: '' },
  { id: 'forearm_r', name: 'Right Forearm', nameAr: 'الساعد الأيمن', category: 'forearms', color: COLORS.forearms.base, colorHover: COLORS.forearms.hover,
    frontPath: 'M158,178 Q160,188 160,200 Q160,212 158,220 Q154,218 152,206 Q152,192 154,180 Z', backPath: '' },
  { id: 'rectus_abdominis', name: 'Rectus Abdominis', nameAr: 'العضلة المستقيمة البطنية', category: 'abs', color: COLORS.abs.base, colorHover: COLORS.abs.hover,
    frontPath: 'M80,128 Q88,130 100,130 Q112,130 120,128 Q120,142 118,156 Q116,170 114,184 Q100,188 86,184 Q84,170 82,156 Q80,142 80,128 Z', backPath: '' },
  { id: 'external_oblique', name: 'External Oblique', nameAr: 'العضلة المائلة الخارجية', category: 'abs', color: COLORS.abs.hover, colorHover: COLORS.abs.hover,
    frontPath: 'M68,128 Q64,142 62,156 Q60,170 62,184 Q70,180 76,168 Q80,152 80,138 Q74,132 68,128 Z M132,128 Q136,142 138,156 Q140,170 138,184 Q130,180 124,168 Q120,152 120,138 Q126,132 132,128 Z', backPath: '' },
  { id: 'hip_flexor_l', name: 'Left Hip Flexor', nameAr: 'ثني الورك الأيسر', category: 'hips', color: COLORS.hips.base, colorHover: COLORS.hips.hover,
    frontPath: 'M76,196 Q72,210 70,224 Q68,238 72,248 Q78,244 82,232 Q84,218 82,202 Z', backPath: '' },
  { id: 'hip_flexor_r', name: 'Right Hip Flexor', nameAr: 'ثني الورك الأيمن', category: 'hips', color: COLORS.hips.base, colorHover: COLORS.hips.hover,
    frontPath: 'M124,196 Q128,210 130,224 Q132,238 128,248 Q122,244 118,232 Q116,218 118,202 Z', backPath: '' },
  { id: 'tensor_fasciae_latae_l', name: 'Left TFL', nameAr: 'الموتر للفافة العريضة الأيسر', category: 'hips', color: COLORS.hips.hover, colorHover: COLORS.hips.hover,
    frontPath: 'M68,228 Q66,240 64,252 Q66,258 72,254 Q76,242 74,230 Z', backPath: '' },
  { id: 'tensor_fasciae_latae_r', name: 'Right TFL', nameAr: 'الموتر للفافة العريضة الأيمن', category: 'hips', color: COLORS.hips.hover, colorHover: COLORS.hips.hover,
    frontPath: 'M132,228 Q134,240 136,252 Q134,258 128,254 Q124,242 126,230 Z', backPath: '' },
  { id: 'quadriceps_l', name: 'Left Quadriceps', nameAr: 'عضلات الفخذ الأمامية اليسرى', category: 'quads', color: COLORS.quads.base, colorHover: COLORS.quads.hover,
    frontPath: 'M72,292 Q70,310 68,330 Q67,350 70,360 Q76,362 82,358 Q84,340 84,318 Q83,300 82,292 Z', backPath: '' },
  { id: 'quadriceps_r', name: 'Right Quadriceps', nameAr: 'عضلات الفخذ الأمامية اليمنى', category: 'quads', color: COLORS.quads.base, colorHover: COLORS.quads.hover,
    frontPath: 'M128,292 Q130,310 132,330 Q133,350 130,360 Q124,362 118,358 Q116,340 116,318 Q117,300 118,292 Z', backPath: '' },
  { id: 'patella_l', name: 'Left Patella', nameAr: 'الرضفة اليسرى', category: 'legs', color: COLORS.hips.base, colorHover: COLORS.hips.hover,
    frontPath: 'M70,360 Q76,356 82,360 Q84,368 80,374 Q76,378 70,374 Q68,368 70,360 Z', backPath: '' },
  { id: 'patella_r', name: 'Right Patella', nameAr: 'الرضفة اليمنى', category: 'legs', color: COLORS.hips.base, colorHover: COLORS.hips.hover,
    frontPath: 'M118,360 Q124,356 130,360 Q132,368 128,374 Q124,378 118,374 Q116,368 118,360 Z', backPath: '' },
  { id: 'tibialis_l', name: 'Left Tibialis Anterior', nameAr: 'القصبة الأمامية اليسرى', category: 'calves', color: COLORS.calves.base, colorHover: COLORS.calves.hover,
    frontPath: 'M72,376 Q70,390 68,408 Q68,420 70,430 Q76,432 80,426 Q82,412 82,396 Q82,382 78,376 Z', backPath: '' },
  { id: 'tibialis_r', name: 'Right Tibialis Anterior', nameAr: 'القصبة الأمامية اليمنى', category: 'calves', color: COLORS.calves.base, colorHover: COLORS.calves.hover,
    frontPath: 'M128,376 Q130,390 132,408 Q132,420 130,430 Q124,432 120,426 Q118,412 118,396 Q118,382 122,376 Z', backPath: '' },
  { id: 'foot_l', name: 'Left Foot', nameAr: 'القدم اليسرى', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: 'M70,432 Q68,444 66,458 Q66,468 74,470 L84,470 Q90,468 90,458 Q88,444 86,436 Q82,430 76,428 Z', backPath: '' },
  { id: 'foot_r', name: 'Right Foot', nameAr: 'القدم اليمنى', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: 'M130,432 Q132,444 134,458 Q134,468 126,470 L116,470 Q110,468 110,458 Q112,444 114,436 Q118,430 124,428 Z', backPath: '' },
];

const BACK_MUSCLES: MuscleRegion[] = [
  { id: 'occipitalis', name: 'Occipitalis', nameAr: 'العضلة القذالية', category: 'head', color: COLORS.head.base, colorHover: COLORS.head.hover,
    frontPath: '', backPath: 'M84,14 Q92,8 100,10 Q108,8 116,14 Q114,22 100,20 Q86,22 84,14 Z' },
  { id: 'splenius_cervicis', name: 'Splenius Cervicis', nameAr: 'العضلة الطحالية الرقبية', category: 'neck', color: COLORS.neck.base, colorHover: COLORS.neck.hover,
    frontPath: '', backPath: 'M82,30 Q90,26 100,24 Q110,26 118,30 Q116,38 100,36 Q84,38 82,30 Z' },
  { id: 'trapezius_back', name: 'Trapezius (Full)', nameAr: 'العضلة شبه المنحرفة', category: 'back', color: COLORS.back.base, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M60,68 Q72,58 100,54 Q128,58 140,68 Q142,80 138,94 Q134,108 126,116 Q114,124 100,126 Q86,124 74,116 Q66,108 62,94 Q58,80 60,68 Z' },
  { id: 'rhomboid_l', name: 'Left Rhomboid', nameAr: 'العضلة المعينية اليسرى', category: 'back', color: COLORS.back.hover, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M68,86 Q78,82 88,86 Q86,98 82,108 Q74,112 68,104 Q66,94 68,86 Z' },
  { id: 'rhomboid_r', name: 'Right Rhomboid', nameAr: 'العضلة المعينية اليمنى', category: 'back', color: COLORS.back.hover, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M132,86 Q122,82 112,86 Q114,98 118,108 Q126,112 132,104 Q134,94 132,86 Z' },
  { id: 'infraspinatus_l', name: 'Left Infraspinatus', nameAr: 'العضلة تحت الشوكية اليسرى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: '', backPath: 'M58,88 Q54,96 52,106 Q54,114 58,112 Q62,102 64,92 Z' },
  { id: 'infraspinatus_r', name: 'Right Infraspinatus', nameAr: 'العضلة تحت الشوكية اليمنى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: '', backPath: 'M142,88 Q146,96 148,106 Q146,114 142,112 Q138,102 136,92 Z' },
  { id: 'deltoid_back_l', name: 'Left Deltoid (Posterior)', nameAr: 'العضلة الدالية الخلفية اليسرى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: '', backPath: 'M52,96 Q48,106 46,118 Q46,128 48,134 Q54,132 56,122 Q58,110 56,98 Z' },
  { id: 'deltoid_back_r', name: 'Right Deltoid (Posterior)', nameAr: 'العضلة الدالية الخلفية اليمنى', category: 'shoulders', color: COLORS.shoulders.base, colorHover: COLORS.shoulders.hover,
    frontPath: '', backPath: 'M148,96 Q152,106 154,118 Q154,128 152,134 Q146,132 144,122 Q142,110 144,98 Z' },
  { id: 'triceps_l', name: 'Left Triceps', nameAr: 'العضلة ثلاثية الرؤوس اليسرى', category: 'arms', color: COLORS.arms.base, colorHover: COLORS.arms.hover,
    frontPath: '', backPath: 'M46,134 Q42,146 40,158 Q39,168 42,174 Q48,172 50,160 Q52,148 50,136 Z' },
  { id: 'triceps_r', name: 'Right Triceps', nameAr: 'العضلة ثلاثية الرؤوس اليمنى', category: 'arms', color: COLORS.arms.base, colorHover: COLORS.arms.hover,
    frontPath: '', backPath: 'M154,134 Q158,146 160,158 Q161,168 158,174 Q152,172 150,160 Q148,148 150,136 Z' },
  { id: 'forearm_back_l', name: 'Left Forearm (Posterior)', nameAr: 'الساعد الخلفي الأيسر', category: 'forearms', color: COLORS.forearms.base, colorHover: COLORS.forearms.hover,
    frontPath: '', backPath: 'M40,174 Q38,186 36,198 Q36,210 38,220 Q42,218 44,206 Q44,192 42,180 Z' },
  { id: 'forearm_back_r', name: 'Right Forearm (Posterior)', nameAr: 'الساعد الخلفي الأيمن', category: 'forearms', color: COLORS.forearms.base, colorHover: COLORS.forearms.hover,
    frontPath: '', backPath: 'M160,174 Q162,186 164,198 Q164,210 162,220 Q158,218 156,206 Q156,192 158,180 Z' },
  { id: 'erector_spinae', name: 'Erector Spinae', nameAr: 'العضلة المنتصبة الشوكية', category: 'back', color: COLORS.back.base, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M92,68 Q90,90 88,112 Q87,134 90,156 Q92,178 96,196 Q98,210 100,218 Q102,210 104,196 Q108,178 110,156 Q113,134 112,112 Q110,90 108,68 Q104,66 100,66 Q96,66 92,68 Z' },
  { id: 'latissimus_l', name: 'Left Latissimus Dorsi', nameAr: 'العضلة الظهرية العريضة اليسرى', category: 'back', color: COLORS.back.hover, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M60,116 Q56,130 54,144 Q52,158 54,172 Q56,186 62,194 Q66,190 68,178 Q66,160 64,142 Q62,128 64,118 Z' },
  { id: 'latissimus_r', name: 'Right Latissimus Dorsi', nameAr: 'العضلة الظهرية العريضة اليمنى', category: 'back', color: COLORS.back.hover, colorHover: COLORS.back.hover,
    frontPath: '', backPath: 'M140,116 Q144,130 146,144 Q148,158 146,172 Q144,186 138,194 Q134,190 132,178 Q134,160 136,142 Q138,128 136,118 Z' },
  { id: 'gluteus_l', name: 'Left Gluteus Maximus', nameAr: 'العضلة الألوية الكبرى اليسرى', category: 'glutes', color: COLORS.glutes.base, colorHover: COLORS.glutes.hover,
    frontPath: '', backPath: 'M66,200 Q62,214 60,228 Q58,240 62,248 Q66,246 72,234 Q74,220 72,206 Z' },
  { id: 'gluteus_r', name: 'Right Gluteus Maximus', nameAr: 'العضلة الألوية الكبرى اليمنى', category: 'glutes', color: COLORS.glutes.base, colorHover: COLORS.glutes.hover,
    frontPath: '', backPath: 'M134,200 Q138,214 140,228 Q142,240 138,248 Q134,246 128,234 Q126,220 128,206 Z' },
  { id: 'hamstring_l', name: 'Left Hamstrings', nameAr: 'أوتار الركبة اليسرى', category: 'hamstrings', color: COLORS.hamstrings.base, colorHover: COLORS.hamstrings.hover,
    frontPath: '', backPath: 'M72,248 Q70,268 68,290 Q67,310 70,330 Q72,348 74,358 Q80,356 82,344 Q82,320 80,296 Q78,272 76,250 Z' },
  { id: 'hamstring_r', name: 'Right Hamstrings', nameAr: 'أوتار الركبة اليمنى', category: 'hamstrings', color: COLORS.hamstrings.base, colorHover: COLORS.hamstrings.hover,
    frontPath: '', backPath: 'M128,248 Q130,268 132,290 Q133,310 130,330 Q128,348 126,358 Q120,356 118,344 Q118,320 120,296 Q122,272 124,250 Z' },
  { id: 'calf_l', name: 'Left Calf (Gastrocnemius)', nameAr: 'الساق اليسرى', category: 'calves', color: COLORS.calves.base, colorHover: COLORS.calves.hover,
    frontPath: '', backPath: 'M72,360 Q70,376 68,394 Q68,408 70,422 Q74,424 78,418 Q80,404 80,388 Q80,372 76,358 Z' },
  { id: 'calf_r', name: 'Right Calf (Gastrocnemius)', nameAr: 'الساق اليمنى', category: 'calves', color: COLORS.calves.base, colorHover: COLORS.calves.hover,
    frontPath: '', backPath: 'M128,360 Q130,376 132,394 Q132,408 130,422 Q126,424 122,418 Q120,404 120,388 Q120,372 124,358 Z' },
  { id: 'achilles_l', name: 'Left Achilles', nameAr: 'وتر أخيل الأيسر', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: '', backPath: 'M72,424 Q70,436 70,448 Q70,456 72,462 Q78,462 80,454 Q80,440 78,426 Z' },
  { id: 'achilles_r', name: 'Right Achilles', nameAr: 'وتر أخيل الأيمن', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: '', backPath: 'M128,424 Q130,436 130,448 Q130,456 128,462 Q122,462 120,454 Q120,440 122,426 Z' },
  { id: 'foot_back_l', name: 'Left Foot', nameAr: 'القدم اليسرى', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: '', backPath: 'M64,464 Q62,472 64,478 L84,478 Q88,474 86,466 Q82,462 74,462 Z' },
  { id: 'foot_back_r', name: 'Right Foot', nameAr: 'القدم اليمنى', category: 'feet', color: COLORS.feet.base, colorHover: COLORS.feet.hover,
    frontPath: '', backPath: 'M136,464 Q138,472 136,478 L116,478 Q112,474 114,466 Q118,462 126,462 Z' },
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

          @for (muscle of currentMuscles(); track muscle.id) {
            <path [attr.d]="muscle.frontPath || muscle.backPath"
              [attr.fill]="pathFill(muscle.id)" [attr.fill-opacity]="pathOpacity(muscle.id)"
              [attr.data-muscle-id]="muscle.id" tabindex="0" role="button"
              [attr.aria-label]="(lang() === 'en' ? muscle.name : muscle.nameAr)"
              (mouseenter)="onMuscleEnter(muscle.id)" (mouseleave)="onMuscleLeave()"
              (touchstart)="onMuscleEnter(muscle.id)" (touchend)="onMuscleLeave()"
              (focus)="onMuscleEnter(muscle.id)" (blur)="onMuscleLeave()"
              (click)="onMuscleClick(muscle.id, $event)"
              (keydown.enter)="onMuscleClick(muscle.id, $event)"
              (keydown.space)="onMuscleClick(muscle.id, $event)"
              stroke="#5c4632" stroke-width="0.6" stroke-opacity="0.5" class="muscle-path">
              <title>{{ lang() === 'en' ? muscle.name : muscle.nameAr }}</title>
            </path>
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
        @for (cat of categories; track cat.key) {
          <button type="button" class="cat-chip" [class.active]="activeCategory() === cat.key"
            [style.--chip-color]="cat.color" (click)="toggleCategory(cat.key)">
            {{ lang() === 'en' ? cat.en : cat.ar }}
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
  readonly regionClick = output<string>();
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

  protected readonly categories = (Object.keys(CATEGORY_LABELS) as Category[]).map((key) => ({
    key, en: CATEGORY_LABELS[key].en, ar: CATEGORY_LABELS[key].ar, color: COLORS[key].base,
  }));

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

  readonly currentMuscles = computed<MuscleRegion[]>(() =>
    this.view() === 'front' ? FRONT_MUSCLES : BACK_MUSCLES
  );

  readonly averageIntensity = computed(() => {
    const pts = this.points();
    if (!pts.length) return 0;
    const sum = pts.reduce((acc, p) => acc + p.intensity, 0);
    return Math.round((sum / pts.length) * 10) / 10;
  });

  ngAfterViewInit(): void {
  }

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

  toggleCategory(cat: Category): void {
    this.activeCategory.set(this.activeCategory() === cat ? null : cat);
  }

  pathFill(id: string): string {
    const muscle = this.currentMuscles().find(m => m.id === id);
    if (!muscle) return 'transparent';
    return this.hoveredId() === id ? muscle.colorHover : muscle.color;
  }

  pathOpacity(id: string): number {
    const searching = this.matchedIds().size > 0;
    const filtering = this.activeCategory() !== null;
    const muscle = this.currentMuscles().find(m => m.id === id);
    if (!muscle) return 0.35;
    if (searching) return this.matchedIds().has(id) ? 0.75 : 0.08;
    if (filtering) return muscle.category === this.activeCategory() ? 0.7 : 0.08;
    return this.hoveredId() === id ? 0.55 : 0.35;
  }

  onMuscleEnter(id: string): void {
    this.hoveredId.set(id);
    const muscle = this.currentMuscles().find(m => m.id === id);
    const label = muscle ? (this.lang() === 'en' ? muscle.name : muscle.nameAr) : '';
    this.hoveredName.set(label);
    this.hoveredColor.set(muscle?.color ?? '');
    if (label) this.liveMessage.set(label);
  }

  onMuscleLeave(): void {
    this.hoveredId.set(null);
    this.hoveredName.set('');
  }

  onMuscleClick(id: string, event: Event): void {
    event.stopPropagation();
    this.regionClick.emit(id);
    const muscle = this.currentMuscles().find(m => m.id === id);
    if (muscle) {
      this.liveMessage.set(
        (this.lang() === 'en' ? 'Selected ' : 'تم اختيار ') +
        (this.lang() === 'en' ? muscle.name : muscle.nameAr)
      );
    }
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
