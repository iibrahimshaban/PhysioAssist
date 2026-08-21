import '@angular/compiler';
import { describe, expect, it } from 'vitest';
import { BehaviorSubject } from 'rxjs';
import { Injector } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import enCatalog from '../../../../assets/i18n/intake/en.json';
import arCatalog from '../../../../assets/i18n/intake/ar.json';
import { SCHEMA_TEXT_KEYS, SchemaTextI18nService } from './schema-text-i18n';

/** Every app-generated string that can reach a public render surface,
 *  mirroring DefaultIntakeSchemaTemplate.cs + CoreFieldConstants.cs.
 *  If a core field is added backend-side, add its text here AND to
 *  SCHEMA_TEXT_KEYS — this test fails otherwise. */
const CANONICAL_SCHEMA_TEXTS = [
  // Section / group headers
  'Required Patient Information',
  'Patient Details',
  'Medical Information',
  'Clinical Summary',
  // Field labels
  'Full Name',
  'Email Address',
  'Phone Number',
  'Patient Free Time',
  'Gender',
  'Date of Birth',
  'Chief Complaint',
  'Injury Date',
  'Patient Type',
  'Address / City',
  'Job / Occupation',
  'Married',
  'How did you know us?',
  'Previous Injuries',
  'Notes',
  // Option values
  'Male',
  'Female',
  'Orthopedic',
  'Neurological',
  'Pediatric',
  'GeneralOther',
  'Social Media',
  'Friend or Family',
  'Google Search',
  'Doctor Referral',
  'Advertisement',
  // Placeholders
  'e.g. John Doe',
  'john@example.com',
  '(555) 000-0000',
  'e.g. Weekdays after 5pm',
  'Primary reason for the visit',
  'e.g. Weekdays after 5pm, weekends anytime',
  'Primary reason for the visit (moved here from the pain map)',
  'e.g. Giza, Egypt',
  'e.g. Software Engineer',
  'e.g. None, or describe prior injuries',
  'e.g. Pain worsens after long sitting.',

  // ── Section & group descriptions (wizard template clones) ──
  'Core fields required for every intake form — cannot be removed',
  'Demographics and contact information',
  'Basic patient demographics and contact details',
  'How to reach the patient',
  'Additional patient information',
  'Details about the presenting condition',
  'Classification used by clinicians',
  "Reason for today's visit",
  'Describe your pain',
  'Previous treatments and history',
  'Your health background',
  'Lifestyle factors that may affect your health',
];

function flatten(o: object, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(o)) {
    const p = prefix ? `${prefix}${k}` : k;
    if (v && typeof v === 'object') Object.assign(out, flatten(v, `${p}.`));
    else out[p] = String(v);
  }
  return out;
}

function loadTranslations(): Record<'en' | 'ar', Record<string, string>> {
  return {
    en: flatten(enCatalog, 'intake.'),
    ar: flatten(arCatalog, 'intake.'),
  };
}

function createService(lang$: BehaviorSubject<'en' | 'ar'>, translations: Record<'en' | 'ar', Record<string, string>>) {
  const stub = {
    langChanges$: lang$,
    getActiveLang: () => lang$.value,
    translate: (key: string) => translations[lang$.value][key] ?? `[${key}]`,
  };
  return Injector.create({
    providers: [
      SchemaTextI18nService,
      { provide: TranslocoService, useValue: stub },
    ],
  }).get(SchemaTextI18nService);
}

describe('SCHEMA_TEXT_KEYS coverage', () => {
  it('maps every canonical app-generated schema text', () => {
    const missing = CANONICAL_SCHEMA_TEXTS.filter(t => !SCHEMA_TEXT_KEYS.has(t));
    expect(missing).toEqual([]);
  });

  it('resolves every map key in both en and ar catalogs', () => {
    const translations = loadTranslations();
    const missing: string[] = [];
    for (const key of SCHEMA_TEXT_KEYS.values()) {
      if (!(key in translations.en)) missing.push(`en:${key}`);
      if (!(key in translations.ar)) missing.push(`ar:${key}`);
    }
    expect(missing).toEqual([]);
  });
});

describe('SchemaTextI18nService.labelFor', () => {
  it('translates canonical texts under ar and keeps English under en', () => {
    const translations = loadTranslations();
    const lang$ = new BehaviorSubject<'en' | 'ar'>('en');
    const service = createService(lang$, translations);

    lang$.next('en');
    expect(service.labelFor('Full Name')).toBe(translations.en['intake.schemaText.fullName']);
    expect(service.labelFor('Patient Details')).toBe('Patient Details');

    lang$.next('ar');
    expect(service.labelFor('Full Name')).toBe(translations.ar['intake.schemaText.fullName']);
    expect(service.labelFor('Full Name')).not.toBe('Full Name');
    expect(service.labelFor('Required Patient Information')).toBe(translations.ar['intake.schemaText.sectionRequiredPatientInfo']);
    expect(service.labelFor('e.g. Giza, Egypt')).toBe(translations.ar['intake.schemaText.phGiza']);

    for (const text of CANONICAL_SCHEMA_TEXTS) {
      const rendered = service.labelFor(text);
      expect(rendered).not.toMatch(/^intake\./);
      expect(rendered).not.toBe(`[${SCHEMA_TEXT_KEYS.get(text)}]`);
    }
  });

  it('returns unknown (staff-authored) text unchanged in every language', () => {
    const translations = loadTranslations();
    const lang$ = new BehaviorSubject<'en' | 'ar'>('ar');
    const service = createService(lang$, translations);

    expect(service.labelFor('My custom knee question')).toBe('My custom knee question');
    expect(service.labelFor(null)).toBe('');
    expect(service.labelFor(undefined)).toBe('');
    expect(service.labelFor('  Full Name  ')).toBe(translations.ar['intake.schemaText.fullName']);
  });
});
