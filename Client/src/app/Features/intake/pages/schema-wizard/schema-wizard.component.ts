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
    sections: []
  });

  readonly saving = signal(false);
  readonly publishing = signal(false);

  // ─── Build Mode State ────────────────────────────────────
  readonly showQuickAdd = signal(false);
  readonly customQuestionText = signal('');
  readonly selectedPreset = signal<QuestionPreset | null>(null);
  readonly editingSectionIndex = signal<number | null>(null);
  readonly editingGroupIndex = signal<number | null>(null);

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
    const sections: FormSectionDto[] = template.sections.map(s => ({
      ...s,
      groups: s.groups.map(g => ({
        ...g,
        questions: g.questions.map(q => ({
          ...q,
          options: q.options ? [...q.options] : undefined
        }))
      }))
    }));

    this.formSchema.set({ schemaVersion: 1, sections });

    // Auto-fill name from template
    if (!this.schemaName() && template.id !== 'blank') {
      this.schemaName.set(template.name);
    }
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

  addQuestionToFirstAvailableGroup(): void {
    const schema = this.formSchema();
    if (schema.sections.length === 0) {
      this.addSection();
      // After adding section, try again on next tick
      setTimeout(() => this.addQuestionToFirstAvailableGroup(), 0);
      return;
    }

    const firstSection = schema.sections[0];
    if (firstSection.groups.length === 0) {
      this.addGroup(0);
      setTimeout(() => this.addQuestionToFirstAvailableGroup(), 0);
      return;
    }

    this.addQuestionToGroup(0, 0);
  }

  addPresetToFirstAvailableGroup(): void {
    const preset = this.selectedPreset();
    if (!preset) return;

    const schema = this.formSchema();
    if (schema.sections.length === 0) {
      this.addSection();
      setTimeout(() => this.addPresetToFirstAvailableGroup(), 0);
      return;
    }

    const firstSection = schema.sections[0];
    if (firstSection.groups.length === 0) {
      this.addGroup(0);
      setTimeout(() => this.addPresetToFirstAvailableGroup(), 0);
      return;
    }

    this.addQuestionToGroup(0, 0, preset);
    this.selectedPreset.set(null);
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