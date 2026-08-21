import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';

/**
 * CANONICAL SCHEMA TEXT → TRANSLATION KEY MAP
 *
 * Convention for future core/app-defined fields (CoreFieldConstants,
 * DefaultIntakeSchemaTemplate):
 *   1. Store the text English-canonical as always (schema JSON is never translated).
 *   2. Add a matching entry here: exact English string → intake.schemaText.* key.
 *   3. Add that key to BOTH assets/i18n/intake/{en,ar}.json.
 * Render surfaces (dynamic-form-renderer, submitted-answers-viewer) then show
 * Arabic when the active language is ar, English otherwise. Editing surfaces
 * (schema-builder, schema-wizard canvases) intentionally do NOT use this map —
 * staff always edit raw canonical English there.
 *
 * This map covers ONLY the closed set of app-generated strings. Custom
 * questions/options/sections authored freely by doctors/staff are open text
 * with no reliable canonical mapping and are NEVER auto-translated — they
 * render exactly as stored.
 */
export const SCHEMA_TEXT_KEYS: ReadonlyMap<string, string> = new Map([
  // ── Section / group headers (app-defined structure) ──
  ['Required Patient Information', 'intake.schemaText.sectionRequiredPatientInfo'],
  ['Patient Details', 'intake.schemaText.groupPatientDetails'],
  ['Medical Information', 'intake.schemaText.groupMedicalInformation'],
  ['Clinical Summary', 'intake.schemaText.clinicalSummary'],

  // ── Field labels ──
  ['Full Name', 'intake.schemaText.fullName'],
  ['Email Address', 'intake.schemaText.emailAddress'],
  ['Phone Number', 'intake.schemaText.phoneNumber'],
  ['Patient Free Time', 'intake.schemaText.patientFreeTime'],
  ['Gender', 'intake.schemaText.gender'],
  ['Date of Birth', 'intake.schemaText.dateOfBirth'],
  ['Chief Complaint', 'intake.schemaText.chiefComplaint'],
  ['Injury Date', 'intake.schemaText.injuryDate'],
  ['Patient Type', 'intake.schemaText.patientType'],
  ['Address / City', 'intake.schemaText.addressCity'],
  ['Job / Occupation', 'intake.schemaText.jobOccupation'],
  ['Married', 'intake.schemaText.married'],
  ['How did you know us?', 'intake.schemaText.referralSource'],
  ['Previous Injuries', 'intake.schemaText.previousInjuries'],
  ['Notes', 'intake.schemaText.notes'],

  // ── Option values ──
  ['Male', 'intake.schemaText.optMale'],
  ['Female', 'intake.schemaText.optFemale'],
  ['Orthopedic', 'intake.schemaText.optOrthopedic'],
  ['Neurological', 'intake.schemaText.optNeurological'],
  ['Pediatric', 'intake.schemaText.optPediatric'],
  ['GeneralOther', 'intake.schemaText.optGeneralOther'],
  ['Social Media', 'intake.schemaText.optSocialMedia'],
  ['Friend or Family', 'intake.schemaText.optFriendOrFamily'],
  ['Google Search', 'intake.schemaText.optGoogleSearch'],
  ['Doctor Referral', 'intake.schemaText.optDoctorReferral'],
  ['Advertisement', 'intake.schemaText.optAdvertisement'],

  // ── Placeholders ──
  ['e.g. John Doe', 'intake.schemaText.phJohnDoe'],
  ['john@example.com', 'intake.schemaText.phEmail'],
  ['(555) 000-0000', 'intake.schemaText.phPhone'],
  ['e.g. Weekdays after 5pm', 'intake.schemaText.phFreeTime'],
  ['Primary reason for the visit', 'intake.schemaText.phChiefComplaint'],
  ['e.g. Weekdays after 5pm, weekends anytime', 'intake.schemaText.phFreeTimeLong'],
  ['Primary reason for the visit (moved here from the pain map)', 'intake.schemaText.phChiefComplaintLong'],
  ['e.g. Giza, Egypt', 'intake.schemaText.phGiza'],
  ['e.g. Software Engineer', 'intake.schemaText.phSoftwareEngineer'],
  ['e.g. None, or describe prior injuries', 'intake.schemaText.phPriorInjuries'],
  ['e.g. Pain worsens after long sitting.', 'intake.schemaText.phNotes'],

  // ── Section & group descriptions (wizard template clones) ──
  ['Core fields required for every intake form — cannot be removed', 'intake.schemaText.descCoreFields'],
  ['Demographics and contact information', 'intake.schemaText.descDemographicsContact'],
  ['Basic patient demographics and contact details', 'intake.schemaText.descPatientDemographics'],
  ['How to reach the patient', 'intake.schemaText.descHowToReach'],
  ['Additional patient information', 'intake.schemaText.descAdditionalInfo'],
  ['Details about the presenting condition', 'intake.schemaText.descPresentingCondition'],
  ['Classification used by clinicians', 'intake.schemaText.descClinicianClassification'],
  ["Reason for today's visit", 'intake.schemaText.descReasonVisit'],
  ['Describe your pain', 'intake.schemaText.descDescribePain'],
  ['Previous treatments and history', 'intake.schemaText.descPreviousTreatment'],
  ['Your health background', 'intake.schemaText.descHealthBackground'],
  ['Lifestyle factors that may affect your health', 'intake.schemaText.descLifestyleFactors'],
]);

@Injectable({ providedIn: 'root' })
export class SchemaTextI18nService {
  private readonly transloco = inject(TranslocoService);
  /** Signal read inside labelFor so zoneless CD re-renders on language change. */
  private readonly lang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  /** Display label for a schema-persisted text: translated when the text is a
   *  known app-generated string, otherwise returned unchanged. */
  labelFor(text: string | null | undefined): string {
    this.lang();
    const key = SCHEMA_TEXT_KEYS.get((text ?? '').trim());
    return key ? this.transloco.translate(key) : (text ?? '');
  }
}
