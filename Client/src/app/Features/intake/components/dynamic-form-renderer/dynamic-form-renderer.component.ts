import { Component, input, output, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelect } from 'primeng/multiselect';
import { SelectButtonModule } from 'primeng/selectbutton';
import {
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  FormQuestionDto,
  QuestionConditionDto,
  ValidationRuleDto,
  SubmissionSectionDto,
  SubmissionGroupDto,
  SubmissionAnswerDto
} from '../../models';

@Component({
  selector: 'app-dynamic-form-renderer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputNumberModule,
    MultiSelect,
    SelectButtonModule
  ],
  templateUrl: './dynamic-form-renderer.component.html',
  styleUrl: './dynamic-form-renderer.component.css'
})
export class DynamicFormRendererComponent {
  readonly schema = input<DynamicFormSchemaDto | null>(null);
  readonly formSchemaId = input<string>('');
  readonly formSchemaVersion = input<number>(1);
  readonly conditionLogic = input<'AND' | 'OR'>('AND');
  /** Pre-fills the answers signal — e.g. for the submission detail page's edit mode,
   *  seeded from the previously stored submission (already unwrapped by the caller). */
  readonly initialAnswers = input<Record<string, any> | null>(null);

  readonly submissionChange = output<DynamicFormSubmissionDto>();
  readonly validityChange = output<boolean>();

  protected readonly painScaleOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(v => ({ label: v.toString(), value: v }));

  protected readonly answers = signal<Record<string, any>>({});
  protected readonly touchedFields = signal<Set<string>>(new Set());

  constructor() {
    effect(() => {
      const initial = this.initialAnswers();
      if (initial) {
        this.answers.set({ ...initial });
      }
    });
  }

  private readonly wideTypes = new Set([
    'textarea', 'checkbox', 'multiselect', 'radio', 'painpoint', 'painscale',
    'bodyselector', 'file', 'fileupload'
  ]);

  protected isTouched(questionId: string): boolean {
    return this.touchedFields().has(questionId);
  }

  protected isWideQuestion(type: string): boolean {
    return this.wideTypes.has(type);
  }

  protected toggleCheckboxOption(questionId: string, option: string, checked: boolean): void {
    const current: string[] = this.answers()[questionId] || [];
    const next = checked
      ? [...current, option]
      : current.filter(o => o !== option);
    this.updateAnswer(questionId, next);
  }

  private readonly wrapTypes = new Set([
    'text', 'email', 'phone', 'number', 'textarea', 'date', 'datetime',
    'select', 'radio', 'boolean', 'multiselect', 'checkbox', 'painscale',
    'file', 'fileupload'
  ]);

  readonly submission = computed<DynamicFormSubmissionDto | null>(() => {
    const s = this.schema();
    if (!s) return null;

    const currentAnswers = this.answers();

    const sections: SubmissionSectionDto[] = s.sections.map(section => {
      const groups: SubmissionGroupDto[] = section.groups.map(group => {
        const answers: SubmissionAnswerDto[] = group.questions
          .filter(q => this.isQuestionVisible(q, currentAnswers))
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

    return {
      schemaVersion: s.schemaVersion,
      formSchemaId: this.formSchemaId(),
      formSchemaVersion: this.formSchemaVersion(),
      sections
    };
  });

  readonly isValid = computed(() => {
    const s = this.schema();
    if (!s) return false;

    const currentAnswers = this.answers();

    for (const section of s.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (!this.isQuestionVisible(question, currentAnswers)) continue;

          const answer = currentAnswers[question.questionId];

          if (this.getQuestionErrors(question, answer, currentAnswers).length > 0) {
            return false;
          }
        }
      }
    }

    return true;
  });

  protected isQuestionVisible(question: FormQuestionDto, overrideAnswers?: Record<string, any>): boolean {
    if (!question.conditions || question.conditions.length === 0) return true;

    const currentAnswers = overrideAnswers ?? this.answers();
    const logic = this.conditionLogic();

    if (logic === 'OR') {
      return question.conditions.some(condition => this.evaluateCondition(condition, currentAnswers));
    }

    return question.conditions.every(condition => this.evaluateCondition(condition, currentAnswers));
  }

  protected getQuestionErrors(
    question: FormQuestionDto,
    overrideAnswer?: any,
    overrideAnswers?: Record<string, any>
  ): string[] {
    const answer = overrideAnswer !== undefined ? overrideAnswer : this.answers()[question.questionId];
    const allAnswers = overrideAnswers ?? this.answers();
    const errors: string[] = [];

    if (question.required && (answer == null || answer === '' || (Array.isArray(answer) && answer.length === 0))) {
      const hasRequiredRule = question.validationRules?.some(r => r.ruleType === 'required');
      if (!hasRequiredRule) {
        errors.push('This field is required.');
      }
    }

    if (question.validationRules) {
      for (const rule of question.validationRules) {
        const error = this.evaluateValidationRule(rule, question, answer, allAnswers);
        if (error) {
          errors.push(error);
        }
      }
    }

    return errors;
  }

  private evaluateValidationRule(
    rule: ValidationRuleDto,
    _question: FormQuestionDto,
    answer: any,
    _allAnswers: Record<string, any>
  ): string | null {
    const msg = rule.message || '';

    switch (rule.ruleType) {
      case 'required': {
        if (answer == null || answer === '' || (Array.isArray(answer) && answer.length === 0)) {
          return msg || 'This field is required.';
        }
        return null;
      }
      case 'pattern': {
        if (answer != null && answer !== '' && rule.value) {
          try {
            if (!new RegExp(rule.value).test(String(answer))) {
              return msg || 'Value does not match the required format.';
            }
          } catch {
            return msg || 'Invalid validation pattern.';
          }
        }
        return null;
      }
      case 'min': {
        const num = Number(answer);
        if (!isNaN(num) && num < Number(rule.value)) {
          return msg || `Minimum value is ${rule.value}.`;
        }
        return null;
      }
      case 'max': {
        const num = Number(answer);
        if (!isNaN(num) && num > Number(rule.value)) {
          return msg || `Maximum value is ${rule.value}.`;
        }
        return null;
      }
      case 'minLength': {
        const str = String(answer ?? '');
        if (str.length < Number(rule.value)) {
          return msg || `Minimum length is ${rule.value} characters.`;
        }
        return null;
      }
      case 'maxLength': {
        const str = String(answer ?? '');
        if (str.length > Number(rule.value)) {
          return msg || `Maximum length is ${rule.value} characters.`;
        }
        return null;
      }
      case 'email': {
        if (answer != null && answer !== '') {
          const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
          if (!emailRegex.test(String(answer))) {
            return msg || 'Please enter a valid email address.';
          }
        }
        return null;
      }
      case 'url': {
        if (answer != null && answer !== '') {
          try {
            new URL(String(answer));
          } catch {
            return msg || 'Please enter a valid URL.';
          }
        }
        return null;
      }
      default:
        return null;
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

  protected updateAnswer(questionId: string, value: any): void {
    this.touchedFields.update(set => { const next = new Set(set); next.add(questionId); return next; });
    this.answers.update(current => ({ ...current, [questionId]: value }));
    this.emitOutputs();
  }

  private emitOutputs(): void {
    const sub = this.submission();
    if (sub) {
      this.submissionChange.emit(sub);
    }
    this.validityChange.emit(this.isValid());
  }
}