import { Component, signal, computed, inject, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import {
  DynamicFormSchemaDto,
  FormSectionDto,
  FormGroupDto,
  FormQuestionDto,
} from '../../models';

// Pinned/required core fields. These MUST match CoreFieldConstants.HardRequiredFields
// on the backend (which uses question_default_* IDs); using the same IDs lets the
// schema pass the server's publish guard and core-field merge. Previously this list
// was hardcoded with core_* IDs and only 6 fields (and included "Occupation", which
// is not a core field) — so Chief Complaint, Patient Type, Injury Date and Patient
// Free Time were missing and from-scratch forms failed to publish.
const CORE_FIELD_IDS = new Set([
  'question_default_full_name',
  'question_default_email',
  'question_default_phone',
  'question_default_gender',
  'question_default_dob',
  'question_default_free_time',
  'question_default_chief_complaint',
  'question_default_injury_date',
  'question_default_patient_type'
]);

const CORE_FIELD_TEXTS = new Set([
  'Full Name',
  'Email Address',
  'Phone Number',
  'Gender',
  'Date of Birth',
  'Patient Free Time',
  'Chief Complaint',
  'Injury Date',
  'Patient Type'
]);

interface PublishValidationIssue {
  fieldName: string;
  issue: string;
}

const CORE_SECTION_ID = 'section_core_fields';
const CORE_GROUP_ID = 'group_core_fields';
const MEDICAL_INFO_GROUP_ID = 'group_medical_information';
const CLINICAL_SUMMARY_GROUP_ID = 'group_clinical_summary';

/* ─── Wizard Step Enum ─────────────────────────────────── */
type WizardStep = 'template' | 'details' | 'build' | 'review';

/* ─── Template Definition ───────────────────────────────── */
interface FormTemplate {
  id: string;
  name: string;
  description: string;
  icon: string;
  color: string;
  sections: FormSectionDto[];
}

/* ─── Pre-Built Templates ───────────────────────────────── */
const FORM_TEMPLATES: FormTemplate[] = [
  {
    id: 'general-intake',
    name: 'General Patient Intake',
    description: 'Standard pre-visit form with patient demographics, contact info, and reason for visit.',
    icon: 'pi pi-file-edit',
    color: '#6a92cb',
    sections: [
      {
        sectionId: 'section_patient_info',
        title: 'Patient Information',
        description: 'Basic patient demographics and contact details',
        order: 1,
        groups: [
          {
            groupId: 'group_contact',
            title: 'Contact Details',
            description: 'How to reach the patient',
            order: 1,
            questions: [
              { questionId: 'q_full_name', text: 'Full Name', type: 'text', order: 1, required: true },
              { questionId: 'q_email', text: 'Email Address', type: 'email', order: 2, required: true },
              { questionId: 'q_phone', text: 'Phone Number', type: 'phone', order: 3, required: true },
              { questionId: 'q_dob', text: 'Date of Birth', type: 'date', order: 4, required: true },
            ]
          },
          {
            groupId: 'group_demographics',
            title: 'Demographics',
            description: 'Additional patient information',
            order: 2,
            questions: [
              { questionId: 'q_gender', text: 'Gender', type: 'select', order: 1, required: false, options: ['Male', 'Female', 'Other', 'Prefer not to say'] },
              { questionId: 'q_address', text: 'Home Address', type: 'textarea', order: 2, required: false },
              { questionId: 'q_emergency_contact', text: 'Emergency Contact Name', type: 'text', order: 3, required: false },
              { questionId: 'q_emergency_phone', text: 'Emergency Contact Phone', type: 'phone', order: 4, required: false },
            ]
          }
        ]
      },
      {
        sectionId: 'section_visit',
        title: 'Visit Information',
        description: 'Reason for today\'s visit',
        order: 2,
        groups: [
          {
            groupId: 'group_reason',
            title: 'Reason for Visit',
            order: 1,
            questions: [
              { questionId: 'q_chief_complaint', text: 'What is the main reason for your visit today?', type: 'textarea', order: 1, required: true },
              { questionId: 'q_symptoms_start', text: 'When did your symptoms start?', type: 'date', order: 2, required: false },
              { questionId: 'q_pain_level', text: 'Current pain level (0-10)', type: 'painscale', order: 3, required: false },
            ]
          }
        ]
      }
    ]
  },
  {
    id: 'pain-assessment',
    name: 'Pain Assessment',
    description: 'Detailed pain evaluation with body map, pain scale, and symptom tracking.',
    icon: 'pi pi-map-marker',
    color: '#f97316',
    sections: [
      {
        sectionId: 'section_pain_main',
        title: 'Pain Details',
        description: 'Describe your pain',
        order: 1,
        groups: [
          {
            groupId: 'group_pain_location',
            title: 'Pain Location',
            order: 1,
            questions: [
              { questionId: 'q_pain_body_map', text: 'Show us where it hurts', type: 'bodyselector', order: 1, required: true },
              { questionId: 'q_pain_region', text: 'Primary pain region', type: 'select', order: 2, required: true, options: ['Head', 'Neck', 'Shoulder', 'Back - Upper', 'Back - Lower', 'Arm', 'Leg', 'Knee', 'Hip', 'Other'] },
            ]
          },
          {
            groupId: 'group_pain_characteristics',
            title: 'Pain Characteristics',
            order: 2,
            questions: [
              { questionId: 'q_pain_type', text: 'How would you describe the pain?', type: 'multiselect', order: 1, required: true, options: ['Sharp', 'Dull', 'Burning', 'Throbbing', 'Stabbing', 'Aching', 'Numbness', 'Tingling'] },
              { questionId: 'q_pain_intensity', text: 'Rate your pain on a scale of 0-10', type: 'painscale', order: 2, required: true },
              { questionId: 'q_pain_duration', text: 'How long have you had this pain?', type: 'select', order: 3, required: true, options: ['Less than a week', '1-2 weeks', '2-4 weeks', '1-3 months', '3-6 months', '6+ months'] },
              { questionId: 'q_pain_constant', text: 'Is the pain constant or intermittent?', type: 'radio', order: 4, required: false, options: ['Constant', 'Intermittent', 'Varies'] },
            ]
          },
          {
            groupId: 'group_pain_triggers',
            title: 'Triggers & Relief',
            order: 3,
            questions: [
              { questionId: 'q_aggravating', text: 'What makes the pain worse?', type: 'textarea', order: 1, required: false },
              { questionId: 'q_relieving', text: 'What makes the pain better?', type: 'textarea', order: 2, required: false },
              { questionId: 'q_medications', text: 'Are you taking any pain medications?', type: 'radio', order: 3, required: false, options: ['Yes', 'No'] },
            ]
          }
        ]
      },
      {
        sectionId: 'section_pain_history',
        title: 'Pain History',
        description: 'Previous treatments and history',
        order: 2,
        groups: [
          {
            groupId: 'group_previous_treatment',
            title: 'Previous Treatment',
            order: 1,
            questions: [
              { questionId: 'q_previous_care', text: 'Have you seen anyone else for this pain?', type: 'radio', order: 1, required: false, options: ['Yes', 'No'] },
              { questionId: 'q_previous_treatment_type', text: 'What treatments have you tried?', type: 'multiselect', order: 2, required: false, options: ['Physical Therapy', 'Chiropractic', 'Massage', 'Acupuncture', 'Surgery', 'Medication', 'Other'] },
              { questionId: 'q_imaging', text: 'Have you had any imaging (X-ray, MRI, CT)?', type: 'radio', order: 3, required: false, options: ['Yes', 'No'] },
            ]
          }
        ]
      }
    ]
  },
  {
    id: 'medical-history',
    name: 'Medical History',
    description: 'Comprehensive medical history including conditions, medications, allergies, and surgeries.',
    icon: 'pi pi-heart',
    color: '#e04b5f',
    sections: [
      {
        sectionId: 'section_medical_history',
        title: 'Medical History',
        description: 'Your health background',
        order: 1,
        groups: [
          {
            groupId: 'group_conditions',
            title: 'Current & Past Conditions',
            order: 1,
            questions: [
              { questionId: 'q_conditions', text: 'Do you have any current medical conditions?', type: 'textarea', order: 1, required: false, placeholder: 'e.g. Diabetes, Hypertension, Asthma' },
              { questionId: 'q_surgeries', text: 'Have you had any surgeries?', type: 'textarea', order: 2, required: false, placeholder: 'List any surgeries and approximate dates' },
              { questionId: 'q_family_history', text: 'Family medical history', type: 'textarea', order: 3, required: false, placeholder: 'Any relevant family medical conditions' },
            ]
          },
          {
            groupId: 'group_medications',
            title: 'Medications & Allergies',
            order: 2,
            questions: [
              { questionId: 'q_medications_list', text: 'Current medications', type: 'textarea', order: 1, required: false, placeholder: 'List all medications and dosages' },
              { questionId: 'q_allergies', text: 'Do you have any allergies?', type: 'textarea', order: 2, required: true, placeholder: 'List all allergies (medications, food, etc.)' },
              { questionId: 'q_blood_type', text: 'Blood Type', type: 'select', order: 3, required: false, options: ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Unknown'] },
            ]
          }
        ]
      },
      {
        sectionId: 'section_lifestyle',
        title: 'Lifestyle & Habits',
        description: 'Lifestyle factors that may affect your health',
        order: 2,
        groups: [
          {
            groupId: 'group_lifestyle',
            title: 'Lifestyle',
            order: 1,
            questions: [
              { questionId: 'q_smoking', text: 'Do you smoke?', type: 'radio', order: 1, required: false, options: ['Never', 'Former', 'Current'] },
              { questionId: 'q_alcohol', text: 'Alcohol consumption', type: 'select', order: 2, required: false, options: ['None', 'Occasional', 'Moderate', 'Heavy'] },
              { questionId: 'q_exercise', text: 'How often do you exercise?', type: 'select', order: 3, required: false, options: ['Daily', '2-3 times/week', 'Once a week', 'Rarely', 'Never'] },
              { questionId: 'q_occupation', text: 'Occupation', type: 'text', order: 4, required: false },
            ]
          }
        ]
      }
    ]
  },
  {
    id: 'blank',
    name: 'Start from Scratch',
    description: 'Begin with an empty form and build it your way with guided help along the way.',
    icon: 'pi pi-plus-circle',
    color: '#6b7780',
    sections: []
  }
];

/* ─── Quick-Add Question Presets ────────────────────────── */
interface QuestionPreset {
  label: string;
  icon: string;
  type: string;
  text: string;
  required: boolean;
  options?: string[];
}

const QUESTION_PRESETS: QuestionPreset[] = [
  { label: 'Full Name', icon: 'pi pi-user', type: 'text', text: 'Full Name', required: true },
  { label: 'Email', icon: 'pi pi-at', type: 'email', text: 'Email Address', required: true },
  { label: 'Phone', icon: 'pi pi-phone', type: 'phone', text: 'Phone Number', required: true },
  { label: 'Date of Birth', icon: 'pi pi-calendar', type: 'date', text: 'Date of Birth', required: true },
  { label: 'Address', icon: 'pi pi-home', type: 'textarea', text: 'Home Address', required: false },
  { label: 'Gender', icon: 'pi pi-venus-mars', type: 'select', text: 'Gender', required: false, options: ['Male', 'Female', 'Other', 'Prefer not to say'] },
  { label: 'Chief Complaint', icon: 'pi pi-question-circle', type: 'textarea', text: 'What is the main reason for your visit today?', required: true },
  { label: 'Pain Level', icon: 'pi pi-chart-bar', type: 'painscale', text: 'Rate your pain on a scale of 0-10', required: true },
  { label: 'Pain Location', icon: 'pi pi-map-marker', type: 'select', text: 'Primary pain region', required: true, options: ['Head', 'Neck', 'Shoulder', 'Back - Upper', 'Back - Lower', 'Arm', 'Leg', 'Knee', 'Hip', 'Other'] },
  { label: 'Medications', icon: 'pi pi-pill', type: 'textarea', text: 'Current medications', required: false },
  { label: 'Allergies', icon: 'pi pi-exclamation-triangle', type: 'textarea', text: 'Do you have any allergies?', required: true },
  { label: 'Medical Conditions', icon: 'pi pi-heart', type: 'textarea', text: 'Do you have any current medical conditions?', required: false },
  { label: 'Surgeries', icon: 'pi pi-scissors', type: 'textarea', text: 'Have you had any surgeries?', required: false },
  { label: 'Smoking Status', icon: 'pi pi-ban', type: 'radio', text: 'Do you smoke?', required: false, options: ['Never', 'Former', 'Current'] },
  { label: 'Occupation', icon: 'pi pi-briefcase', type: 'text', text: 'Occupation', required: false },
  { label: 'Emergency Contact', icon: 'pi pi-phone', type: 'text', text: 'Emergency Contact Name', required: false },
];

@Component({
  selector: 'app-schema-wizard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    CheckboxModule,
    TooltipModule,
    DialogModule,
  ],
  templateUrl: './schema-wizard.component.html',
  styleUrl: './schema-wizard.component.css'
})
export class SchemaWizardComponent {
  private readonly apiService = inject(IntakeApiService);
  private readonly engine = inject(DynamicFormEngineService);
  private readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // ─── Wizard State ────────────────────────────────────────
  readonly currentStep = signal<WizardStep>('template');
  readonly templates = FORM_TEMPLATES;
  readonly questionPresets = QUESTION_PRESETS;

  readonly selectedTemplateId = signal<string | null>(null);
  readonly schemaName = signal('');
  readonly schemaDescription = signal('');
  readonly isDefault = signal(false);

  readonly formSchema = signal<DynamicFormSchemaDto>({
    schemaVersion: 1,
    sections: [this.buildCoreFieldsSection()]
  });

  readonly saving = signal(false);
  readonly publishing = signal(false);

  // Pre-publish validation
  readonly showPublishValidationDialog = signal(false);
  readonly publishValidationIssues = signal<PublishValidationIssue[]>([]);

  // ─── Build Mode State ────────────────────────────────────
  readonly showQuickAdd = signal(false);
  readonly showGroupPicker = signal(false);
  readonly customQuestionText = signal('');
  readonly selectedPreset = signal<QuestionPreset | null>(null);
  readonly editingSectionIndex = signal<number | null>(null);
  readonly editingGroupIndex = signal<number | null>(null);

  readonly quickAddTargetGroupTitle = computed<string | null>(() => {
    const si = this.editingSectionIndex();
    const gi = this.editingGroupIndex();
    if (si === null || gi === null) return null;
    const section = this.formSchema().sections[si];
    if (!section) return null;
    const group = section.groups[gi];
    return group ? group.title : null;
  });

  // Flat list of every group in the schema, across every section, for the "which group
  // should this go in?" picker. Rebuilt reactively whenever the schema changes.
  readonly groupTargets = computed(() => {
    const targets: { sectionIndex: number; groupIndex: number; sectionTitle: string; groupTitle: string; questionCount: number; locked: boolean; hidden: boolean }[] = [];
    this.formSchema().sections.forEach((section, si) => {
      section.groups.forEach((group, gi) => {
        targets.push({
          sectionIndex: si,
          groupIndex: gi,
          sectionTitle: section.title,
          groupTitle: group.title,
          questionCount: group.questions.length,
          locked: group.isLocked === true,
          hidden: group.hiddenFromPatient === true,
        });
      });
    });
    return targets;
  });

  // ─── Computed ────────────────────────────────────────────
  readonly selectedTemplate = computed(() =>
    this.templates.find(t => t.id === this.selectedTemplateId()) || null
  );

  readonly totalQuestions = computed(() => {
    let count = 0;
    for (const section of this.formSchema().sections) {
      for (const group of section.groups) {
        count += group.questions.length;
      }
    }
    return count;
  });

  readonly totalSections = computed(() => this.formSchema().sections.length);

  readonly canProceedFromTemplate = computed(() =>
    this.selectedTemplateId() !== null
  );

  readonly canProceedFromDetails = computed(() =>
    this.schemaName().trim().length > 0
  );

  readonly canProceedFromBuild = computed(() =>
    this.formSchema().sections.length > 0 && this.totalQuestions() > 0
  );

  readonly prePublishValidation = computed((): PublishValidationIssue[] => {
    const issues: PublishValidationIssue[] = [];
    const schema = this.formSchema();
    const allQuestions = this.engine.getAllQuestions(schema);

    for (const coreId of CORE_FIELD_IDS) {
      const found = allQuestions.find(q => q.questionId === coreId);
      if (!found) {
        const byText = allQuestions.find(q => CORE_FIELD_TEXTS.has(q.text));
        if (!byText) {
          issues.push({ fieldName: coreId.replace('question_default_', '').replace('_', ' '), issue: 'Missing from schema' });
          continue;
        }
        if (!byText.required) {
          issues.push({ fieldName: byText.text, issue: 'Required flag is disabled' });
        }
      } else {
        if (!found.required) {
          issues.push({ fieldName: found.text, issue: 'Required flag is disabled' });
        }
      }
    }

    return issues;
  });

  readonly canPublish = computed(() => this.prePublishValidation().length === 0);

  readonly stepProgress = computed(() => {
    const steps: { key: WizardStep; label: string; icon: string; completed: boolean }[] = [
      { key: 'template', label: 'Template', icon: 'pi pi-template', completed: this.selectedTemplateId() !== null },
      { key: 'details', label: 'Details', icon: 'pi pi-info-circle', completed: this.schemaName().trim().length > 0 },
      { key: 'build', label: 'Build', icon: 'pi pi-pencil', completed: this.canProceedFromBuild() },
      { key: 'review', label: 'Review', icon: 'pi pi-check-circle', completed: false },
    ];
    return steps;
  });

  readonly currentStepIndex = computed(() => {
    const order: WizardStep[] = ['template', 'details', 'build', 'review'];
    return order.indexOf(this.currentStep());
  });

  // ─── Step Navigation ─────────────────────────────────────
  goToStep(step: WizardStep): void {
    this.currentStep.set(step);
  }

  nextStep(): void {
    const order: WizardStep[] = ['template', 'details', 'build', 'review'];
    const currentIdx = order.indexOf(this.currentStep());
    if (currentIdx < order.length - 1) {
      // If moving from template to details, apply the template
      if (this.currentStep() === 'template' && this.selectedTemplate()) {
        this.applyTemplate();
      }
      this.currentStep.set(order[currentIdx + 1]);
    }
  }

  prevStep(): void {
    const order: WizardStep[] = ['template', 'details', 'build', 'review'];
    const currentIdx = order.indexOf(this.currentStep());
    if (currentIdx > 0) {
      this.currentStep.set(order[currentIdx - 1]);
    }
  }

  // ─── Template Selection ──────────────────────────────────
  selectTemplate(templateId: string): void {
    this.selectedTemplateId.set(templateId);
  }

  private applyTemplate(): void {
    const template = this.selectedTemplate();
    if (!template) return;

    // Deep clone the template sections
    const userSections: FormSectionDto[] = template.sections.map(s => ({
      ...s,
      groups: s.groups.map(g => ({
        ...g,
        questions: g.questions.map(q => ({
          ...q,
          options: q.options ? [...q.options] : undefined
        }))
      }))
    }));

    const coreSection = this.buildCoreFieldsSection();
    this.formSchema.set({ schemaVersion: 1, sections: [coreSection, ...userSections] });

    // Auto-fill name from template
    if (!this.schemaName() && template.id !== 'blank') {
      this.schemaName.set(template.name);
    }
  }

  // ─── Core Fields (Locked Section) ────────────────────────
  // Mirrors schema-builder.component.ts buildCoreFieldsSection() field-for-field
  // (which itself mirrors backend CoreFieldConstants.HardRequiredFields). Keep these
  // two in sync until the shared CoreFieldsService lands.
  private buildCoreFieldsSection(): FormSectionDto {
    return {
      sectionId: CORE_SECTION_ID,
      title: 'Required Patient Information',
      description: 'Core fields required for every intake form — cannot be removed',
      order: 0,
      isLocked: true,
      groups: [
        {
          groupId: CORE_GROUP_ID,
          title: 'Patient Details',
          description: 'Demographics and contact information',
          order: 1,
          isLocked: true,
          questions: [
            { questionId: 'question_default_full_name', text: 'Full Name', type: 'text', order: 1, required: true, isLocked: true, placeholder: 'e.g. John Doe' },
            { questionId: 'question_default_email', text: 'Email Address', type: 'email', order: 2, required: true, isLocked: true, placeholder: 'john@example.com' },
            { questionId: 'question_default_phone', text: 'Phone Number', type: 'phone', order: 3, required: true, isLocked: true, placeholder: '(555) 000-0000' },
            { questionId: 'question_default_free_time', text: 'Patient Free Time', type: 'text', order: 4, required: true, isLocked: true, placeholder: 'e.g. Weekdays after 5pm' },
            { questionId: 'question_default_gender', text: 'Gender', type: 'radio', order: 5, required: true, isLocked: true, options: ['Male', 'Female'] },
            { questionId: 'question_default_dob', text: 'Date of Birth', type: 'date', order: 6, required: true, isLocked: true },
          ]
        },
        {
          groupId: MEDICAL_INFO_GROUP_ID,
          title: 'Medical Information',
          description: 'Details about the presenting condition',
          order: 2,
          isLocked: true,
          questions: [
            { questionId: 'question_default_chief_complaint', text: 'Chief Complaint', type: 'textarea', order: 1, required: true, isLocked: true, placeholder: 'Primary reason for the visit' },
            { questionId: 'question_default_injury_date', text: 'Injury Date', type: 'date', order: 2, required: true, isLocked: true },
          ]
        },
        {
          groupId: CLINICAL_SUMMARY_GROUP_ID,
          title: 'Clinical Summary',
          description: 'Classification used by clinicians',
          order: 3,
          hiddenFromPatient: true,
          isLocked: true,
          questions: [
            { questionId: 'question_default_patient_type', text: 'Patient Type', type: 'select', order: 1, required: true, isLocked: true, options: ['Orthopedic', 'Neurological', 'Pediatric', 'GeneralOther'] },
          ]
        }
      ]
    };
  }

  isSectionLocked(section: FormSectionDto): boolean {
    return section.sectionId === CORE_SECTION_ID || section.isLocked === true;
  }

  isQuestionLocked(question: any): boolean {
    return question.isLocked === true || question.questionId?.startsWith('question_default_') === true;
  }

  // Locked groups block deletion of their existing (core) questions, but adding extra
  // custom questions is always allowed — mirrors schema-builder's "Add Question: allowed
  // even for locked groups" behavior. Targeting is explicit (via the group picker or a
  // pre-selected group), so this is safe to allow on every group, not just the first.
  canAddQuestionInline(group: FormGroupDto): boolean {
    return true;
  }

  // ─── Build: Section Management ───────────────────────────
  addSection(): void {
    const schema = this.formSchema();
    const newSection: FormSectionDto = {
      sectionId: this.generateId('section'),
      title: 'New Section',
      description: '',
      order: schema.sections.length + 1,
      groups: []
    };
    this.formSchema.set({
      ...schema,
      sections: [...schema.sections, newSection]
    });
    this.editingSectionIndex.set(schema.sections.length);
    this.editingGroupIndex.set(null);
  }

  removeSection(index: number): void {
    const schema = this.formSchema();
    const section = schema.sections[index];
    if (section && this.isSectionLocked(section)) return;
    const sections = schema.sections.filter((_, i) => i !== index);
    this.formSchema.set({ ...schema, sections });
    if (this.editingSectionIndex() === index) {
      this.editingSectionIndex.set(null);
    }
  }

  // ─── Build: Group Management ─────────────────────────────
  addGroup(sectionIndex: number): void {
    const schema = this.formSchema();
    const section = schema.sections[sectionIndex];
    if (!section) return;

    const newGroup: FormGroupDto = {
      groupId: this.generateId('group'),
      title: 'New Group',
      description: '',
      order: section.groups.length + 1,
      questions: []
    };
    section.groups = [...section.groups, newGroup];
    this.formSchema.set({ ...schema });
    this.editingSectionIndex.set(sectionIndex);
    this.editingGroupIndex.set(section.groups.length - 1);
  }

  removeGroup(sectionIndex: number, groupIndex: number): void {
    const schema = this.formSchema();
    const section = schema.sections[sectionIndex];
    if (!section) return;
    section.groups = section.groups.filter((_, i) => i !== groupIndex);
    this.formSchema.set({ ...schema });
    if (this.editingGroupIndex() === groupIndex && this.editingSectionIndex() === sectionIndex) {
      this.editingGroupIndex.set(null);
    }
  }

  // Available on every group, including locked core groups — hiding a group from
  // patients only affects the public intake form's visibility (enforced backend-side
  // by ValidateRequiredFields skipping HiddenFromPatient groups); it doesn't touch
  // whether the group/questions can be deleted, so lock status is irrelevant here.
  toggleGroupVisibility(sectionIndex: number, groupIndex: number, hidden: boolean): void {
    const schema = this.formSchema();
    const section = schema.sections[sectionIndex];
    const group = section?.groups[groupIndex];
    if (!group) return;
    group.hiddenFromPatient = hidden;
    this.formSchema.set({ ...schema });
  }

  // ─── Build: Question Management ──────────────────────────
  addQuestionToGroup(sectionIndex: number, groupIndex: number, preset?: QuestionPreset): void {
    const schema = this.formSchema();
    const section = schema.sections[sectionIndex];
    const group = section?.groups[groupIndex];
    if (!group) return;

    let text: string;
    let type: string;
    let required: boolean;
    let options: string[] | undefined;

    if (preset) {
      text = preset.text;
      type = preset.type;
      required = preset.required;
      options = preset.options ? [...preset.options] : undefined;
    } else if (this.customQuestionText().trim()) {
      text = this.customQuestionText().trim();
      type = 'text';
      required = false;
      options = undefined;
    } else {
      return;
    }

    const newQuestion: FormQuestionDto = {
      questionId: this.generateId('question'),
      text,
      type,
      order: group.questions.length + 1,
      required,
      options,
    };

    group.questions = [...group.questions, newQuestion];
    this.formSchema.set({ ...schema });
    this.customQuestionText.set('');
    this.selectedPreset.set(null);
  }

  removeQuestion(sectionIndex: number, groupIndex: number, questionIndex: number): void {
    const schema = this.formSchema();
    const section = schema.sections[sectionIndex];
    const group = section?.groups[groupIndex];
    if (!group) return;
    group.questions = group.questions.filter((_, i) => i !== questionIndex);
    this.formSchema.set({ ...schema });
  }

  // ─── Template Helpers ────────────────────────────────────
  countTemplateQuestions(template: FormTemplate): number {
    let count = 0;
    for (const section of template.sections) {
      for (const group of section.groups) {
        count += group.questions.length;
      }
    }
    return count;
  }

  countAllGroups(): number {
    let count = 0;
    for (const section of this.formSchema().sections) {
      count += section.groups.length;
    }
    return count;
  }

  // ─── Build UI Helpers ────────────────────────────────────
  toggleEditSection(index: number): void {
    if (this.editingSectionIndex() === index) {
      this.editingSectionIndex.set(null);
    } else {
      this.editingSectionIndex.set(index);
      this.editingGroupIndex.set(null);
    }
  }

  toggleEditGroup(sectionIndex: number, groupIndex: number): void {
    if (this.editingSectionIndex() === sectionIndex && this.editingGroupIndex() === groupIndex) {
      this.editingGroupIndex.set(null);
    } else {
      this.editingSectionIndex.set(sectionIndex);
      this.editingGroupIndex.set(groupIndex);
    }
  }

  togglePreset(preset: QuestionPreset): void {
    if (this.selectedPreset() === preset) {
      this.selectedPreset.set(null);
    } else {
      this.selectedPreset.set(preset);
    }
  }

  showQuickAddForGroup(sectionIndex: number, groupIndex: number): void {
    this.editingSectionIndex.set(sectionIndex);
    this.editingGroupIndex.set(groupIndex);
    this.showQuickAdd.set(true);
  }

  private hasPendingQuestion(): boolean {
    return this.selectedPreset() !== null || this.customQuestionText().trim().length > 0;
  }

  // Called from the "Add to Form" button (preset selected) and the custom-question
  // input (Enter key / + button). If a specific group was pre-selected — by clicking
  // that group's own "Add Question" button — add straight there. Otherwise, ask which
  // group it should go in via the picker, since a form can have several groups.
  requestAddQuestion(): void {
    if (!this.hasPendingQuestion()) return;

    const si = this.editingSectionIndex();
    const gi = this.editingGroupIndex();
    const preselectedGroup = si !== null && gi !== null
      ? this.formSchema().sections[si]?.groups[gi]
      : undefined;

    if (preselectedGroup) {
      this.addQuestionToGroup(si!, gi!, this.selectedPreset() ?? undefined);
      this.editingSectionIndex.set(null);
      this.editingGroupIndex.set(null);
      return;
    }

    this.showGroupPicker.set(true);
  }

  addToPickedGroup(sectionIndex: number, groupIndex: number): void {
    this.addQuestionToGroup(sectionIndex, groupIndex, this.selectedPreset() ?? undefined);
    this.showGroupPicker.set(false);
  }

  closeGroupPicker(): void {
    this.showGroupPicker.set(false);
  }

  // ─── Helpers ─────────────────────────────────────────────
  generateId(prefix: string): string {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  getQuestionTypeIcon(type: string): string {
    const icons: Record<string, string> = {
      text: 'pi pi-pencil',
      number: 'pi pi-hashtag',
      email: 'pi pi-at',
      phone: 'pi pi-phone',
      date: 'pi pi-calendar',
      datetime: 'pi pi-clock',
      textarea: 'pi pi-align-left',
      select: 'pi pi-list',
      multiselect: 'pi pi-list',
      checkbox: 'pi pi-check-square',
      radio: 'pi pi-circle',
      boolean: 'pi pi-check',
      file: 'pi pi-upload',
      fileupload: 'pi pi-upload',
      painpoint: 'pi pi-map-marker',
      bodyselector: 'pi pi-user',
      painscale: 'pi pi-chart-bar',
    };
    return icons[type] || 'pi pi-question';
  }

  // ─── Save & Publish ──────────────────────────────────────
  saveDraft(): void {
    this.saving.set(true);
    const schemaJson = this.engine.serializeSchema(this.formSchema());

    const request = {
      name: this.schemaName(),
      description: this.schemaDescription() || undefined,
      schemaJson,
      isDefault: this.isDefault()
    };

    this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created) => {
        this.saving.set(false);
        this.snackbar.success('Schema saved', ['Draft created successfully']);
        this.router.navigate(['/app/intake/schemas/edit', created.id], { replaceUrl: true });
      },
      error: (err: any) => {
        this.saving.set(false);
        this.snackbar.error('Save failed', [this.extractError(err)]);
      }
    });
  }

  saveAndPublish(): void {
    const issues = this.prePublishValidation();
    if (issues.length > 0) {
      this.publishValidationIssues.set(issues);
      this.showPublishValidationDialog.set(true);
      return;
    }

    this.publishing.set(true);
    const schemaJson = this.engine.serializeSchema(this.formSchema());

    const request = {
      name: this.schemaName(),
      description: this.schemaDescription() || undefined,
      schemaJson,
      isDefault: this.isDefault()
    };

    this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created) => {
        this.apiService.publishFormSchema(created.id, { version: created.version })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.publishing.set(false);
              this.snackbar.success('Schema published', ['Form schema is now live']);
              this.router.navigate(['/app/intake/schemas']);
            },
            error: (err: any) => {
              this.publishing.set(false);
              this.snackbar.error('Publish failed', [this.extractError(err)]);
            }
          });
      },
      error: (err: any) => {
        this.publishing.set(false);
        this.snackbar.error('Save failed', [this.extractError(err)]);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/app/intake/schemas']);
  }

  private extractError(err: any): string {
    const body = err?.error;
    if (body?.detail) return body.detail;
    if (body?.errors) {
      const msgs = Object.values(body.errors as Record<string, string[]>).flat();
      return msgs.join('; ');
    }
    return body?.title || 'Unexpected error';
  }
}