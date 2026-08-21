import { Component, input, output, signal, effect, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormControl, ValidatorFn, Validators } from '@angular/forms';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelect } from 'primeng/multiselect';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Subscription } from 'rxjs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import {
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  FormQuestionDto,
  QuestionConditionDto,
  SubmissionSectionDto,
  SubmissionGroupDto,
  SubmissionAnswerDto
} from '../../models';
import { BodyPainMapComponent, BodyPainMapPayload } from '../../components/body-pain-map/body-pain-map.component';
import { SchemaTextI18nService } from '../../shared/schema-text-i18n';

@Component({
  selector: 'app-dynamic-form-renderer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputNumberModule,
    MultiSelect,
    SelectButtonModule,
    TranslocoModule,
    BodyPainMapComponent
  ],
  templateUrl: './dynamic-form-renderer.component.html',
  styleUrl: './dynamic-form-renderer.component.css'
})
export class DynamicFormRendererComponent implements OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  protected readonly schemaText = inject(SchemaTextI18nService);

  readonly schema = input<DynamicFormSchemaDto | null>(null);
  readonly formSchemaId = input<string>('');
  readonly formSchemaVersion = input<number>(1);
  readonly conditionLogic = input<'AND' | 'OR'>('AND');
  readonly readOnly = input<boolean>(false);
  readonly initialAnswers = input<Record<string, any> | null>(null);
  // When true, doctor-only / auto-computed fields (e.g. the "summary" Clinical Summary)
  // are hidden from the patient-facing form. They are still shown on doctor-side views.
  readonly patientMode = input<boolean>(false);

  readonly submissionChange = output<DynamicFormSubmissionDto>();
  readonly validityChange = output<boolean>();
  readonly requiredStatsChange = output<{ completed: number; total: number }>();

  protected readonly painScaleOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(v => ({ label: v.toString(), value: v }));

  protected multiSelectOptions(question: FormQuestionDto): { label: string; value: string }[] {
    return (question.options ?? []).map(opt => ({ label: this.schemaText.labelFor(opt), value: opt }));
  }

  readonly form = new FormGroup({});
  private valueChangesSub?: Subscription;
  private previousVisibility = new Map<string, boolean>();

  private readonly wideTypes = new Set([
    'textarea', 'checkbox', 'multiselect', 'painpoint', 'painscale',
    'bodyselector', 'file', 'fileupload', 'summary'
  ]);

  private readonly wrapTypes = new Set([
    'text', 'email', 'phone', 'number', 'textarea', 'date', 'datetime',
    'select', 'radio', 'boolean', 'multiselect', 'checkbox', 'painscale',
    'file', 'fileupload'
  ]);

  markAllAsTouched(): void {
    this.form.markAllAsTouched();
  }

  constructor() {
    effect(() => {
      const s = this.schema();
      if (s) {
        this.buildForm(s);
      }
    });

    effect(() => {
      const initial = this.initialAnswers();
      if (initial && Object.keys(this.form.controls).length > 0) {
        this.form.patchValue(initial, { emitEvent: false });
        this.emitOutputs();
      }
    });

    effect(() => {
      const s = this.schema();
      const logic = this.conditionLogic();
      if (s) {
        this.updateControlVisibility(s, logic);
      }
    });
  }

  ngOnDestroy(): void {
    this.valueChangesSub?.unsubscribe();
  }

  protected isWideQuestion(type: string): boolean {
    return this.wideTypes.has(type);
  }

  protected getControl(questionId: string): FormControl {
    return this.form.get(questionId) as FormControl;
  }

  protected getNestedControl(questionId: string, field: string): FormControl {
    const group = this.form.get(questionId) as unknown as FormGroup;
    return group?.get(field) as FormControl;
  }

  protected isGroupVisible(group: { hiddenFromPatient?: boolean }): boolean {
    // Doctor-only groups (e.g. Clinical Summary) are never shown on the patient-facing
    // public intake form, but remain visible on doctor-side submission/edit views where
    // patientMode is false.
    return !(this.patientMode() && group.hiddenFromPatient === true);
  }

  protected isQuestionVisible(question: FormQuestionDto): boolean {
    // The Clinical Summary (type "summary") is auto-generated server-side after submit
    // and is doctor-only. Never show it on the patient-facing form (otherwise the patient
    // sees an empty "Clinical Summary" box).
    if (this.patientMode() && question.type === 'summary') return false;

    if (!question.conditions || question.conditions.length === 0) return true;

    const currentAnswers = this.form.value as Record<string, any>;
    const logic = this.conditionLogic();

    if (logic === 'OR') {
      return question.conditions.some(condition => this.evaluateCondition(condition, currentAnswers));
    }

    return question.conditions.every(condition => this.evaluateCondition(condition, currentAnswers));
  }

  protected getQuestionErrorMessages(question: FormQuestionDto): string[] {
    const control = this.getControl(question.questionId);
    if (!control || !control.errors || !control.touched) return [];

    const errors: string[] = [];
    const errs = control.errors;

    if (errs['required']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'required');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.required'));
    }
    if (errs['email']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'email');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.email'));
    }
    if (errs['pattern']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'pattern');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.pattern'));
    }
    if (errs['min']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'min');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.min', { min: rule?.value }));
    }
    if (errs['max']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'max');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.max', { max: rule?.value }));
    }
    if (errs['minlength']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'minLength');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.minLength', { requiredLength: rule?.value }));
    }
    if (errs['maxlength']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'maxLength');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.maxLength', { requiredLength: rule?.value }));
    }
    if (errs['url']) {
      const rule = question.validationRules?.find(r => r.ruleType === 'url');
      errors.push(rule?.message || this.transloco.translate('intake.renderer.validation.url'));
    }
    if (errs['custom']) {
      errors.push(errs['custom']);
    }

    return errors;
  }

  protected toggleCheckboxOption(questionId: string, option: string, checked: boolean): void {
    const control = this.getControl(questionId);
    if (!control) return;

    const current: string[] = control.value || [];
    const next = checked
      ? [...current, option]
      : current.filter(o => o !== option);
    control.setValue(next);
    control.markAsTouched();
  }

  protected updateNestedField(questionId: string, field: string, value: any): void {
    const group = this.getControl(questionId) as unknown as FormGroup;
    if (!group) return;

    const control = group.get(field);
    if (control) {
      control.setValue(value);
      control.markAsTouched();
      this.emitOutputs();
    }
  }

  onBodyMapChange(questionId: string, payload: BodyPainMapPayload): void {
    const control = this.form.get(questionId) as FormControl | null;
    if (!control) return;
    control.setValue(payload);
    control.markAsTouched();
    this.emitOutputs();
  }

  readonly submission = signal<DynamicFormSubmissionDto | null>(null);

  readonly isValid = signal(false);

  private buildForm(schema: DynamicFormSchemaDto): void {
    this.valueChangesSub?.unsubscribe();
    Object.keys(this.form.controls).forEach(key => this.form.removeControl(key, { emitEvent: false }));

    for (const section of schema.sections) {
      for (const group of section.groups) {
        if (!this.isGroupVisible(group)) continue;
        for (const question of group.questions) {
          const validators = this.buildValidators(question);

          if (question.type === 'painpoint') {
            const nestedGroup = this.fb.group({
              intensity: [5],
              anatomicalRegion: [''],
              bodyPart: [''],
              side: [''],
              description: ['']
            });
            this.form.addControl(question.questionId, nestedGroup, { emitEvent: false });
          } else if (question.type === 'bodyselector') {
            // Body map selection is stored as a BodyPainMapPayload (regions array) so the
            // backend can require >=1 selected region on submit.
            this.form.addControl(question.questionId, new FormControl<BodyPainMapPayload | null>(null, validators), { emitEvent: false });
          } else if (question.type === 'checkbox' || question.type === 'multiselect') {
            this.form.addControl(question.questionId, new FormControl([], validators), { emitEvent: false });
          } else {
            const defaultValue = question.type === 'boolean' ? false : '';
            this.form.addControl(question.questionId, new FormControl(defaultValue, validators), { emitEvent: false });
          }
        }
      }
    }

    this.valueChangesSub = this.form.valueChanges.subscribe(() => {
      this.emitOutputs();
    });

    this.emitOutputs();
  }

  private buildValidators(question: FormQuestionDto): ValidatorFn[] {
    const validators: ValidatorFn[] = [];

    if (question.required) {
      validators.push(Validators.required);
    }

    if (question.validationRules) {
      for (const rule of question.validationRules) {
        switch (rule.ruleType) {
          case 'required':
            if (!validators.some(v => v === Validators.required)) {
              validators.push(Validators.required);
            }
            break;
          case 'email':
            validators.push(Validators.email);
            break;
          case 'pattern':
            if (rule.value) {
              try {
                validators.push(Validators.pattern(rule.value));
              } catch { /* invalid regex, skip */ }
            }
            break;
          case 'min':
            validators.push(Validators.min(Number(rule.value)));
            break;
          case 'max':
            validators.push(Validators.max(Number(rule.value)));
            break;
          case 'minLength':
            validators.push(Validators.minLength(Number(rule.value)));
            break;
          case 'maxLength':
            validators.push(Validators.maxLength(Number(rule.value)));
            break;
          case 'url':
            validators.push(this.urlValidator());
            break;
        }
      }
    }

    return validators;
  }

  private urlValidator(): ValidatorFn {
    return (control) => {
      if (!control.value) return null;
      try {
        new URL(String(control.value));
        return null;
      } catch {
        return { url: true };
      }
    };
  }

  private updateControlVisibility(schema: DynamicFormSchemaDto, logic: 'AND' | 'OR'): void {
    for (const section of schema.sections) {
      for (const group of section.groups) {
        if (!this.isGroupVisible(group)) continue;
        for (const question of group.questions) {
          const visible = this.isQuestionVisible(question);
          const wasVisible = this.previousVisibility.get(question.questionId);

          if (visible !== wasVisible) {
            const control = this.getControl(question.questionId);
            if (control) {
              if (visible) {
                control.enable({ emitEvent: false });
              } else {
                control.disable({ emitEvent: false });
              }
            }
            this.previousVisibility.set(question.questionId, visible);
          }
        }
      }
    }
  }

  private evaluateCondition(condition: QuestionConditionDto, answers: Record<string, any>): boolean {
    const answer = answers[condition.targetQuestionId];

    switch (condition.operator) {
      case 'equals': return answer === condition.value;
      case 'notEquals': return answer !== condition.value;
      case 'contains': return String(answer ?? '').includes(String(condition.value ?? ''));
      case 'greaterThan': return Number(answer) > Number(condition.value);
      case 'lessThan': return Number(answer) < Number(condition.value);
      case 'in': {
        const values = Array.isArray(condition.value) ? condition.value : String(condition.value ?? '').split(',').map(v => v.trim());
        return values.includes(answer);
      }
      case 'notIn': {
        const values = Array.isArray(condition.value) ? condition.value : String(condition.value ?? '').split(',').map(v => v.trim());
        return !values.includes(answer);
      }
      default: return true;
    }
  }

  private emitOutputs(): void {
    const s = this.schema();

    // Build submission imperatively from live form values
    if (s) {
      const currentAnswers = this.form.value as Record<string, any>;

      const sections: SubmissionSectionDto[] = s.sections.map(section => {
        const groups: SubmissionGroupDto[] = section.groups
          .filter(group => this.isGroupVisible(group))
          .map(group => {
            const answers: SubmissionAnswerDto[] = group.questions
              .filter(q => this.isQuestionVisible(q))
              .map(q => ({
                questionId: q.questionId,
                value: this.wrapTypes.has(q.type)
                  ? { [q.type]: currentAnswers[q.questionId] }
                  : currentAnswers[q.questionId],
                attachments: q.type === 'file' ? [] : undefined
              }));
            return { groupId: group.groupId, answers };
          });
        return { sectionId: section.sectionId, groups };
      });

      const sub: DynamicFormSubmissionDto = {
        schemaVersion: s.schemaVersion,
        formSchemaId: this.formSchemaId(),
        formSchemaVersion: this.formSchemaVersion(),
        sections
      };

      this.submission.set(sub);
      this.submissionChange.emit(sub);
    }

    // Calculate validity and required stats
    let valid = true;
    let requiredTotal = 0;
    let requiredCompleted = 0;

    if (s) {
      for (const section of s.sections) {
        for (const group of section.groups) {
          if (!this.isGroupVisible(group)) continue;
          for (const question of group.questions) {
            if (!this.isQuestionVisible(question)) continue;

            if (question.required) {
              requiredTotal++;
              const control = this.getControl(question.questionId);
              const value = control?.value;
              const filled = value != null && value !== '' && !(Array.isArray(value) && value.length === 0);
              if (filled) {
                requiredCompleted++;
              }
            }

            const control = this.getControl(question.questionId);
            if (control && control.invalid) {
              valid = false;
            }
          }
        }
      }
    }

    this.isValid.set(valid);
    this.validityChange.emit(valid);
    this.requiredStatsChange.emit({ completed: requiredCompleted, total: requiredTotal });
  }
}