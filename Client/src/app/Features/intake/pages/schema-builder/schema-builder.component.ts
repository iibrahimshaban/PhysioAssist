import { Component, signal, OnInit, inject, computed, DestroyRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { AccordionModule } from 'primeng/accordion';
import { DividerModule } from 'primeng/divider';
import { DialogModule } from 'primeng/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import {
  FormSchemaResponse,
  DynamicFormSchemaDto,
  FormSectionDto,
  FormGroupDto,
  FormQuestionDto,
  QuestionConditionDto,
  ValidationRuleDto
} from '../../models';

type BuilderMode = 'schema' | 'section' | 'group' | 'question' | null;

interface QuestionTypeOption {
  label: string;
  value: string;
  icon: string;
}

// Core field IDs that are locked by the system
const CORE_FIELD_IDS = new Set([
  'core_full_name',
  'core_email',
  'core_phone',
  'core_gender',
  'core_dob',
  'core_occupation'
]);

// Core field texts for matching
const CORE_FIELD_TEXTS = new Set([
  'Full Name',
  'Email Address',
  'Phone Number',
  'Gender',
  'Date of Birth',
  'Occupation'
]);

const CORE_SECTION_ID = 'section_core_fields';
const CORE_GROUP_ID = 'group_core_fields';

interface PublishValidationIssue {
  fieldName: string;
  issue: string;
}

@Component({
  selector: 'app-schema-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    CardModule,
    InputTextModule,
    TextareaModule,
    CheckboxModule,
    SelectModule,
    InputNumberModule,
    TooltipModule,
    SelectButtonModule,
    AccordionModule,
    DividerModule,
    DialogModule
  ],
  templateUrl: './schema-builder.component.html',
  styleUrl: './schema-builder.component.css'
})
export class SchemaBuilderComponent implements OnInit {
  private readonly apiService = inject(IntakeApiService);
  protected readonly engine = inject(DynamicFormEngineService);
  protected readonly snackbar = inject(SnackbarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Preview mode (read-only) — activated when navigated to from the
  // schema list "Preview" action (?preview=true). Shows the live form
  // preview without any editing chrome (tree, properties, save/publish).
  readonly previewMode = signal(false);

  // Signals
  selectedSchema = signal<FormSchemaResponse | null>(null);
  loading = signal(false);
  saving = signal(false);
  publishing = signal(false);
  selectedItem = signal<string>('schema');
  selectedMode = signal<BuilderMode>('schema');
  schemaVersion = signal(1);

  // Pre-publish validation
  showPublishValidationDialog = signal(false);
  publishValidationIssues = signal<PublishValidationIssue[]>([]);

  // Form Schema Signal
  formSchema = signal<DynamicFormSchemaDto>({
    schemaVersion: 1,
    sections: []
  });

  // Selected items
  selectedSection = signal<FormSectionDto | null>(null);
  selectedGroup = signal<FormGroupDto | null>(null);
  selectedQuestion = signal<FormQuestionDto | null>(null);

  // Computed
  availableQuestions = computed(() => {
    const current = this.selectedQuestion();
    return this.engine.getAllQuestions(this.formSchema())
      .filter(q => !current || q.questionId !== current.questionId)
      .map(q => ({ label: q.text, value: q.questionId }));
  });

  // Pre-publish validation computed
  prePublishValidation = computed(() => {
    const issues: PublishValidationIssue[] = [];
    const schema = this.formSchema();
    const allQuestions = this.engine.getAllQuestions(schema);

    // Check each core field
    for (const coreId of CORE_FIELD_IDS) {
      const found = allQuestions.find(q => q.questionId === coreId);
      if (!found) {
        // Try matching by text
        const byText = allQuestions.find(q => CORE_FIELD_TEXTS.has(q.text));
        if (!byText) {
          issues.push({ fieldName: coreId.replace('core_', '').replace('_', ' '), issue: 'Missing from schema' });
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

  canPublish = computed(() => this.prePublishValidation().length === 0);

  // Expansion state
  private expandedSections = signal<Set<string>>(new Set());
  private expandedGroups = signal<Set<string>>(new Set());

  // Form data
  schemaName = '';
  schemaDescription = '';
  isDefault = false;

  // Condition logic
  conditionLogic: 'and' | 'or' = 'and';
  readonly conditionLogicOptions = [
    { label: 'All (AND)', value: 'and' },
    { label: 'Any (OR)', value: 'or' }
  ];

  // Question types
  questionTypes: QuestionTypeOption[] = [
    { label: 'Text', value: 'text', icon: 'pi pi-pencil' },
    { label: 'Number', value: 'number', icon: 'pi pi-hashtag' },
    { label: 'Email', value: 'email', icon: 'pi pi-at' },
    { label: 'Phone', value: 'phone', icon: 'pi pi-phone' },
    { label: 'Date', value: 'date', icon: 'pi pi-calendar' },
    { label: 'Date Time', value: 'datetime', icon: 'pi pi-clock' },
    { label: 'Textarea', value: 'textarea', icon: 'pi pi-align-left' },
    { label: 'Dropdown', value: 'select', icon: 'pi pi-list' },
    { label: 'Multi Select', value: 'multiselect', icon: 'pi pi-list' },
    { label: 'Checkbox', value: 'checkbox', icon: 'pi pi-check-square' },
    { label: 'Radio', value: 'radio', icon: 'pi pi-circle' },
    { label: 'Boolean', value: 'boolean', icon: 'pi pi-check' },
    { label: 'File Upload', value: 'file', icon: 'pi pi-upload' },
    { label: 'File Upload (Legacy)', value: 'fileupload', icon: 'pi pi-upload' },
    { label: 'Pain Point', value: 'painpoint', icon: 'pi pi-map-marker' },
    { label: 'Body Selector', value: 'bodyselector', icon: 'pi pi-user' },
    { label: 'Pain Scale', value: 'painscale', icon: 'pi pi-chart-bar' }
  ];

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const schemaId = params.get('id');
      if (schemaId) {
        this.loadSchema(schemaId);
      } else {
        this.resetBuilder();
      }
    });

    // Determine whether we are in read-only Preview mode.
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(qp => {
      this.previewMode.set(qp.get('preview') === 'true');
    });
  }

  exitPreview(): void {
    // Return to the editable builder for the same schema (if known),
    // otherwise back to the schema list.
    const id = this.selectedSchema()?.id;
    if (id) {
      this.router.navigate(['/app/intake/schemas/edit', id]);
    } else {
      this.router.navigate(['/app/intake/schemas']);
    }
  }

  private resetBuilder(): void {
    this.selectedSchema.set(null);
    this.schemaName = '';
    this.schemaDescription = '';
    this.isDefault = false;
    this.schemaVersion.set(1);
    this.formSchema.set({
      schemaVersion: 1,
      sections: [this.buildCoreFieldsSection()]
    });
  }

  isEditMode(): boolean {
    return !!this.selectedSchema();
  }

  loadSchema(id: string): void {
    this.loading.set(true);
    this.apiService.getFormSchemaById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (schema) => {
        this.selectedSchema.set(schema);
        this.schemaName = schema.name;
        this.schemaDescription = schema.description || '';
        this.isDefault = schema.isDefault;
        this.schemaVersion.set(schema.version);

        try {
          const parsed = JSON.parse(schema.schemaJson) as DynamicFormSchemaDto;
          const sections = parsed.sections ?? [];
          // Ensure core fields section exists
          if (!sections.some(s => s.sectionId === CORE_SECTION_ID)) {
            sections.unshift(this.buildCoreFieldsSection());
          }
          this.formSchema.set({
            schemaVersion: parsed.schemaVersion ?? 1,
            sections
          });
        } catch (error) {
          console.error('Failed to parse schema JSON:', error);
          this.formSchema.set({ schemaVersion: 1, sections: [] });
        }

        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.snackbar.error('Failed to load schema', [this.extractError(err)]);
      }
    });
  }

  selectItem(type: BuilderMode, item: any): void {
    this.selectedMode.set(type);

    if (type === 'schema') {
      this.selectedItem.set('schema');
      this.selectedSection.set(null);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(null);
    } else if (type === 'section') {
      this.selectedItem.set(item.sectionId);
      this.selectedSection.set(item);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(null);
      this.toggleSection(item.sectionId);
    } else if (type === 'group') {
      this.selectedItem.set(item.groupId);
      this.selectedSection.set(null);
      this.selectedGroup.set(item);
      this.selectedQuestion.set(null);
      this.toggleGroup(item.groupId);
    } else if (type === 'question') {
      this.selectedItem.set(item.questionId);
      this.selectedSection.set(null);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(item);
    }
  }

  // Expansion management
  isSectionExpanded(sectionId: string): boolean {
    return this.expandedSections().has(sectionId);
  }

  isGroupExpanded(groupId: string): boolean {
    return this.expandedGroups().has(groupId);
  }

  toggleSection(sectionId: string): void {
    const expanded = new Set(this.expandedSections());
    if (expanded.has(sectionId)) {
      expanded.delete(sectionId);
    } else {
      expanded.add(sectionId);
    }
    this.expandedSections.set(expanded);
  }

  toggleGroup(groupId: string): void {
    const expanded = new Set(this.expandedGroups());
    if (expanded.has(groupId)) {
      expanded.delete(groupId);
    } else {
      expanded.add(groupId);
    }
    this.expandedGroups.set(expanded);
  }

  // Helper to find sectionId by groupId
  getSectionId(group: FormGroupDto): string | undefined {
    return this.formSchema().sections.find(s => s.groups.some(g => g.groupId === group.groupId))?.sectionId;
  }

  getQuestionTypeIcon(type: string): string {
    return this.questionTypes.find(t => t.value === type)?.icon || 'pi pi-question';
  }

  // Check if a question is a locked core field
  isQuestionLocked(question: FormQuestionDto): boolean {
    return question.isLocked === true || CORE_FIELD_IDS.has(question.questionId);
  }

  // Check if a section is the locked core fields section
  isSectionLocked(section: FormSectionDto): boolean {
    return section.isLocked === true || section.sectionId === CORE_SECTION_ID;
  }

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
            { questionId: 'core_full_name', text: 'Full Name', type: 'text', order: 1, required: true, isLocked: true, placeholder: 'e.g. John Doe' },
            { questionId: 'core_email', text: 'Email Address', type: 'email', order: 2, required: true, isLocked: true, placeholder: 'john@example.com' },
            { questionId: 'core_phone', text: 'Phone Number', type: 'phone', order: 3, required: true, isLocked: true, placeholder: '(555) 000-0000' },
            { questionId: 'core_gender', text: 'Gender', type: 'radio', order: 4, required: true, isLocked: true, options: ['Male', 'Female'] },
            { questionId: 'core_dob', text: 'Date of Birth', type: 'date', order: 5, required: true, isLocked: true },
            { questionId: 'core_occupation', text: 'Occupation', type: 'text', order: 6, required: true, isLocked: true, placeholder: 'e.g. Software Engineer' },
          ]
        }
      ]
    };
  }

  // CRUD operations
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

    this.toggleSection(newSection.sectionId);
    this.selectItem('section', newSection);
  }

  deleteSection(sectionId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);
    if (section) {
      // Check if section is locked
      if (this.isSectionLocked(section)) {
        this.snackbar.warning('Cannot delete', ['This is a required section and cannot be removed.']);
        return;
      }
      // Check if section contains locked questions
      const hasLocked = section.groups.some(g => g.questions.some(q => this.isQuestionLocked(q)));
      if (hasLocked) {
        this.snackbar.warning('Cannot delete', ['This section contains locked required fields and cannot be removed.']);
        return;
      }
    }

    this.formSchema.set({
      ...schema,
      sections: schema.sections.filter(s => s.sectionId !== sectionId)
    });

    if (this.selectedItem() === sectionId) {
      this.selectItem('schema', null);
    }
  }

  addGroup(sectionId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);

    if (section) {
      const newGroup: FormGroupDto = {
        groupId: this.generateId('group'),
        title: 'New Group',
        description: '',
        order: section.groups.length + 1,
        questions: []
      };

      section.groups.push(newGroup);
      this.formSchema.set({ ...schema });

      this.toggleGroup(newGroup.groupId);
      this.selectItem('group', newGroup);
    }
  }

  deleteGroup(sectionId: string, groupId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);

    if (section) {
      const group = section.groups.find(g => g.groupId === groupId);
      if (group) {
        // Check if group contains locked questions
        const hasLocked = group.questions.some(q => this.isQuestionLocked(q));
        if (hasLocked) {
          this.snackbar.warning('Cannot delete', ['This group contains locked required fields and cannot be removed.']);
          return;
        }
      }

      section.groups = section.groups.filter(g => g.groupId !== groupId);
      this.formSchema.set({ ...schema });

      if (this.selectedItem() === groupId) {
        this.selectItem('schema', null);
      }
    }
  }

  addQuestion(sectionId: string, groupId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);
    const group = section?.groups.find(g => g.groupId === groupId);

    if (group) {
      const newQuestion: FormQuestionDto = {
        questionId: this.generateId('question'),
        text: 'New Question',
        description: '',
        type: 'text',
        order: group.questions.length + 1,
        required: false,
        options: []
      };

      group.questions.push(newQuestion);
      this.formSchema.set({ ...schema });

      this.selectItem('question', newQuestion);
    }
  }

  deleteQuestion(sectionId: string, groupId: string, questionId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);
    const group = section?.groups.find(g => g.groupId === groupId);
    const question = group?.questions.find(q => q.questionId === questionId);

    if (question && this.isQuestionLocked(question)) {
      this.snackbar.warning('Cannot delete', ['This field is locked and cannot be removed.']);
      return;
    }

    if (group) {
      group.questions = group.questions.filter(q => q.questionId !== questionId);
      this.formSchema.set({ ...schema });

      if (this.selectedItem() === questionId) {
        this.selectItem('schema', null);
      }
    }
  }

  generateId(prefix: string): string {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  getOptionsString(): string {
    return this.selectedQuestion()?.options?.join(', ') || '';
  }

  updateOptions(value: string): void {
    const question = this.selectedQuestion();
    if (question) {
      question.options = value.split(',').map(o => o.trim()).filter(o => o.length > 0);
    }
  }

  getConditions(): QuestionConditionDto[] {
    return this.selectedQuestion()?.conditions || [];
  }

  getValidationRules(): ValidationRuleDto[] {
    return this.selectedQuestion()?.validationRules || [];
  }

  addCondition(): void {
    const question = this.selectedQuestion();
    if (!question) return;
    if (!question.conditions) {
      question.conditions = [];
    }
    question.conditions.push({
      targetQuestionId: '',
      operator: 'equals',
      value: ''
    });
    this.formSchema.set({ ...this.formSchema() });
  }

  removeCondition(index: number): void {
    const question = this.selectedQuestion();
    if (!question?.conditions) return;
    question.conditions.splice(index, 1);
    if (question.conditions.length === 0) {
      question.conditions = undefined;
    }
    this.formSchema.set({ ...this.formSchema() });
  }

  addValidationRule(): void {
    const question = this.selectedQuestion();
    if (!question) return;
    if (!question.validationRules) {
      question.validationRules = [];
    }
    question.validationRules.push({
      ruleType: 'required',
      value: undefined,
      message: undefined
    });
    this.formSchema.set({ ...this.formSchema() });
  }

  removeValidationRule(index: number): void {
    const question = this.selectedQuestion();
    if (!question?.validationRules) return;
    question.validationRules.splice(index, 1);
    if (question.validationRules.length === 0) {
      question.validationRules = undefined;
    }
    this.formSchema.set({ ...this.formSchema() });
  }

  private getCurrentSchemaJson(): string {
    return this.engine.serializeSchema(this.formSchema());
  }

  saveDraft(): void {
    this.saving.set(true);
    const schemaJson = this.getCurrentSchemaJson();
    const existing = this.selectedSchema();

    const request = {
      name: this.schemaName,
      description: this.schemaDescription || undefined,
      schemaJson,
      isDefault: this.isDefault
    };

    if (existing) {
      this.apiService.updateFormSchema(existing.id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated) => {
          this.selectedSchema.set(updated);
          this.schemaVersion.set(updated.version);
          this.saving.set(false);
          this.snackbar.success('Schema saved', ['Draft updated successfully']);
        },
        error: (err: any) => {
          this.saving.set(false);
          this.snackbar.error('Save failed', [this.extractError(err)]);
        }
      });
    } else {
      this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (created) => {
          this.selectedSchema.set(created);
          this.schemaVersion.set(created.version);
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
  }

  publish(): void {
    // Run pre-publish validation
    const issues = this.prePublishValidation();
    if (issues.length > 0) {
      this.publishValidationIssues.set(issues);
      this.showPublishValidationDialog.set(true);
      return;
    }

    const existing = this.selectedSchema();
    if (!existing) {
      this.saveDraftWithCallback(() => this.doPublish());
    } else {
      this.doPublish();
    }
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

  private saveDraftWithCallback(callback: () => void): void {
    this.saving.set(true);
    const schemaJson = this.getCurrentSchemaJson();
    const request = {
      name: this.schemaName,
      description: this.schemaDescription || undefined,
      schemaJson,
      isDefault: this.isDefault
    };

    this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created) => {
        this.selectedSchema.set(created);
        this.schemaVersion.set(created.version);
        this.saving.set(false);
        callback();
      },
      error: (err: any) => {
        this.saving.set(false);
        this.snackbar.error('Save failed', [this.extractError(err)]);
      }
    });
  }

  private doPublish(): void {
    const existing = this.selectedSchema();
    if (!existing) return;

    this.publishing.set(true);
    this.apiService.publishFormSchema(existing.id, { version: existing.version }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (published) => {
        this.selectedSchema.set(published);
        this.schemaVersion.set(published.version);
        this.publishing.set(false);
        this.snackbar.success('Schema published', ['Form schema is now live']);
        this.router.navigate(['/app/intake/schemas']);
      },
      error: (err: any) => {
        this.publishing.set(false);
        this.snackbar.error('Publish failed', [this.extractError(err)]);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/app/intake/schemas']);
  }
}