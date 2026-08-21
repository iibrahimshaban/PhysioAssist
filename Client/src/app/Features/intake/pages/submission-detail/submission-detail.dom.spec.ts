import '@angular/compiler';
import { describe, expect, it, beforeAll } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import enCatalog from '../../../../../assets/i18n/intake/en.json';
import arCatalog from '../../../../../assets/i18n/intake/ar.json';
import { SubmissionSummaryCardComponent } from './submission-summary-card/submission-summary-card.component';
import { SubmittedAnswersViewerComponent } from './submitted-answers-viewer/submitted-answers-viewer.component';
import { IntakeStatus, PreVisitIntakeDetailsResponse } from '../../models';

function flatten(o: unknown, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(o as Record<string, unknown>)) {
    const p = prefix ? `${prefix}${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      Object.assign(out, flatten(v, `${p}.`));
    } else {
      out[p] = String(v);
    }
  }
  return out;
}

const translations: Record<'en' | 'ar', Record<string, string>> = {
  en: flatten(enCatalog, 'intake.'),
  ar: flatten(arCatalog, 'intake.'),
};

const lang$ = new BehaviorSubject<'en' | 'ar'>('en');

const translocoStub = {
  langChanges$: lang$,
  config: { reRenderOnLangChange: true },
  getActiveLang: () => lang$.value,
  translate: (key: string) => translations[lang$.value][key] ?? `[${key}]`,
  _loadDependencies: () => of(null),
};

const details = {
  id: 'i1',
  shortCode: 'ABC123',
  doctorId: 'd1',
  formSchemaId: 's1',
  formSchemaVersion: 3,
  formSubmissionData: '{}',
  status: IntakeStatus.Submitted,
  submittedAt: new Date('2026-01-15T10:30:00Z').toISOString(),
  formSchemaName: 'General Intake',
} as PreVisitIntakeDetailsResponse;

async function mountCard() {
  await TestBed.configureTestingModule({ imports: [SubmissionSummaryCardComponent] })
    .overrideProvider(TranslocoService, { useValue: translocoStub })
    .compileComponents();

  const fixture = TestBed.createComponent(SubmissionSummaryCardComponent);
  fixture.componentRef.setInput('details', details);
  fixture.componentRef.setInput('patientName', 'Ahmed Ali');
  fixture.detectChanges();
  return fixture;
}

const schema = {
  formSchemaId: 's1',
  name: 'General Intake',
  sections: [
    {
      sectionId: 'sec1',
      title: 'Required Patient Information',
      description: 'Basic patient demographics and contact details',
      groups: [
        {
          groupId: 'g1',
          title: 'Patient Details',
          questions: [
            { questionId: 'sel1', type: 'select', text: 'How did you know us?', required: false, options: ['Social Media', 'Friend or Family', 'Google Search', 'Doctor Referral', 'Advertisement'] },
            { questionId: 'txt1', type: 'textarea', text: 'Notes', required: false },
          ],
        },
      ],
    },
  ],
} as any;

const submissionData = {
  sections: [
    {
      sectionId: 'sec1',
      groups: [
        {
          groupId: 'g1',
          answers: [
            { questionId: 'sel1', value: 'Google Search' },
            { questionId: 'txt1', value: 'Free text answer kept as-is' },
          ],
        },
      ],
    },
  ],
} as any;

async function mountViewer() {
  await TestBed.configureTestingModule({ imports: [SubmittedAnswersViewerComponent] })
    .overrideProvider(TranslocoService, { useValue: translocoStub })
    .compileComponents();

  const fixture = TestBed.createComponent(SubmittedAnswersViewerComponent);
  fixture.componentRef.setInput('isEditing', false);
  fixture.componentRef.setInput('schema', schema);
  fixture.componentRef.setInput('submissionData', submissionData);
  fixture.componentRef.setInput('details', details);
  fixture.componentRef.setInput('initialAnswers', {});
  fixture.detectChanges();
  return fixture;
}

describe('SubmissionSummaryCardComponent i18n DOM rendering', () => {
  beforeAll(() => {
    const w = window as any;
    if (typeof w.matchMedia !== 'function') {
      w.matchMedia = (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => {},
        removeListener: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => false,
      });
    }
  });

  it('renders English summary labels under en', async () => {
    lang$.next('en');
    const fixture = await mountCard();
    const el: HTMLElement = fixture.nativeElement;

    const labels = Array.from(el.querySelectorAll('.detail-label')).map(p => p.textContent!.trim());
    expect(labels).toContain('Submitted');
    expect(labels).toContain('Submission ID');
    expect(labels).toContain('Form Name');
    expect(labels).toContain('Version');

    const headings = Array.from(el.querySelectorAll('h3')).map(h => h.textContent!.trim());
    expect(headings).toContain(translations.en['intake.detail.summary.formDetails']);
  });

  it('renders Arabic summary labels with no English leak under ar', async () => {
    lang$.next('ar');
    const fixture = await mountCard();
    const el: HTMLElement = fixture.nativeElement;

    const labels = Array.from(el.querySelectorAll('.detail-label')).map(p => p.textContent!.trim());
    expect(labels).toEqual([
      translations.ar['intake.detail.summary.submitted'],
      translations.ar['intake.detail.summary.submissionId'],
      translations.ar['intake.detail.summary.formName'],
      translations.ar['intake.detail.summary.version'],
    ]);

    const bodyText = el.textContent!;
    expect(bodyText).toContain(translations.ar['intake.detail.summary.formDetails']);
    expect(bodyText).not.toContain('Submitted');
    expect(bodyText).not.toContain('Form Details');
    expect(bodyText).not.toContain('Submission ID');
  });
});

describe('SubmittedAnswersViewerComponent i18n DOM rendering', () => {
  it('shows canonical English option values under en', async () => {
    lang$.next('en');
    const fixture = await mountViewer();
    const el: HTMLElement = fixture.nativeElement;

    const values = Array.from(el.querySelectorAll('.answer-value')).map(p => p.textContent!.trim());
    expect(values).toEqual(['Google Search', 'Free text answer kept as-is']);
  });

  it('translates stored option values and keeps free text under ar', async () => {
    lang$.next('ar');
    const fixture = await mountViewer();
    const el: HTMLElement = fixture.nativeElement;

    const values = Array.from(el.querySelectorAll('.answer-value')).map(p => p.textContent!.trim());
    expect(values[0]).toBe(translations.ar['intake.schemaText.optGoogleSearch']);
    expect(values[0]).not.toBe('Google Search');
    expect(values[1]).toBe('Free text answer kept as-is');
  });

  it('translates section titles under ar', async () => {
    lang$.next('ar');
    const fixture = await mountViewer();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.textContent).toContain(translations.ar['intake.schemaText.sectionRequiredPatientInfo']);
    expect(el.textContent).not.toContain('Required Patient Information');
  });
});
