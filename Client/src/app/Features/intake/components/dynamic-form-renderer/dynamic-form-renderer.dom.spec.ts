import '@angular/compiler';
import { describe, expect, it, beforeAll } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import enCatalog from '../../../../../assets/i18n/intake/en.json';
import arCatalog from '../../../../../assets/i18n/intake/ar.json';
import { DynamicFormRendererComponent } from './dynamic-form-renderer.component';

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

const schema = {
  formSchemaId: 's1',
  name: 'Test Schema',
  sections: [
    {
      sectionId: 'sec1',
      title: 'Required Patient Information',
      description: 'Basic patient demographics and contact details',
      groups: [
        {
          groupId: 'g1',
          title: 'Patient Details',
          description: 'How to reach the patient',
          questions: [
            {
              questionId: 'text1',
              type: 'text',
              text: 'Full Name',
              required: true,
              placeholder: 'e.g. John Doe',
            },
            {
              questionId: 'multi1',
              type: 'multiselect',
              text: 'How did you know us?',
              required: false,
              placeholder: 'Select all that apply',
              options: ['Social Media', 'Friend or Family', 'Google Search', 'Doctor Referral', 'Advertisement'],
            },
            {
              questionId: 'chief1',
              type: 'textarea',
              text: 'Chief Complaint',
              required: true,
              placeholder: 'Primary reason for the visit (moved here from the pain map)',
            },
            {
              questionId: 'notes1',
              type: 'textarea',
              text: 'Notes',
              required: false,
              placeholder: 'e.g. Pain worsens after long sitting.',
            },
          ],
        },
      ],
    },
  ],
} as any;

async function mount() {
  await TestBed.configureTestingModule({ imports: [DynamicFormRendererComponent] })
    .overrideProvider(TranslocoService, { useValue: translocoStub })
    .compileComponents();

  const fixture = TestBed.createComponent(DynamicFormRendererComponent);
  fixture.componentRef.setInput('schema', schema);
  fixture.detectChanges();
  return fixture;
}

describe('DynamicFormRendererComponent schema text i18n DOM rendering', () => {
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

  it('renders the canonical English placeholder under en', async () => {
    lang$.next('en');
    const fixture = await mount();

    const input = fixture.nativeElement.querySelector('#q-text1') as HTMLInputElement;
    expect(input.getAttribute('placeholder')).toBe('e.g. John Doe');
  });

  it('renders Arabic placeholder in the DOM under ar', async () => {
    lang$.next('ar');
    const fixture = await mount();

    const input = fixture.nativeElement.querySelector('#q-text1') as HTMLInputElement;
    expect(input.getAttribute('placeholder')).toBe(translations.ar['intake.schemaText.phJohnDoe']);
    expect(input.getAttribute('placeholder')).not.toBe('e.g. John Doe');
  });

  it('renders Arabic placeholders for the Chief Complaint and Notes textareas under ar', async () => {
    lang$.next('ar');
    const fixture = await mount();

    const chief = fixture.nativeElement.querySelector('#q-chief1') as HTMLTextAreaElement;
    expect(chief.getAttribute('placeholder')).toBe(translations.ar['intake.schemaText.phChiefComplaintLong']);
    expect(chief.getAttribute('placeholder')).not.toContain('Primary reason');

    const notes = fixture.nativeElement.querySelector('#q-notes1') as HTMLTextAreaElement;
    expect(notes.getAttribute('placeholder')).toBe(translations.ar['intake.schemaText.phNotes']);
    expect(notes.getAttribute('placeholder')).not.toContain('Pain worsens');
  });

  it('renders Arabic section and group descriptions under ar', async () => {
    lang$.next('ar');
    const fixture = await mount();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.textContent).toContain(translations.ar['intake.schemaText.descPatientDemographics']);
    expect(el.textContent).toContain(translations.ar['intake.schemaText.descHowToReach']);
    expect(el.textContent).not.toContain('Basic patient demographics');
    expect(el.textContent).not.toContain('How to reach the patient');
  });

  it('renders Arabic labels in the multiselect dropdown panel and stores canonical values', async () => {
    lang$.next('ar');
    const fixture = await mount();
    const el: HTMLElement = fixture.nativeElement;

    const trigger = el.querySelector('.p-multiselect') as HTMLElement;
    expect(trigger).toBeTruthy();
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const items = Array.from(document.querySelectorAll<HTMLElement>('.p-multiselect-option'));
    expect(items.length).toBe(5);

    const rendered = items.map(i => i.textContent!.trim());
    expect(rendered).toEqual([
      translations.ar['intake.schemaText.optSocialMedia'],
      translations.ar['intake.schemaText.optFriendOrFamily'],
      translations.ar['intake.schemaText.optGoogleSearch'],
      translations.ar['intake.schemaText.optDoctorReferral'],
      translations.ar['intake.schemaText.optAdvertisement'],
    ]);

    const control = (fixture.componentInstance as any).form.get('multi1');
    control.setValue(['Google Search']);
    expect(control.value).toEqual(['Google Search']);
  });
});
