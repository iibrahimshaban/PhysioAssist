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
  template: `
    <div class="dynamic-form-renderer" role="form" aria-label="Dynamic intake form">
      @if (!schema()) {
        <div class="text-center py-12 text-slate-400" role="status">
          <i class="pi pi-inbox text-4xl block mb-3"></i>
          <p>No form schema provided.</p>
        </div>
      } @else if ((schema()?.sections?.length ?? 0) === 0) {
        <div class="text-center py-12 text-slate-400" role="status">
          <i class="pi pi-file text-4xl block mb-3"></i>
          <p>This form has no sections defined yet.</p>
        </div>
      } @else {
        @for (section of schema()!.sections; track section.sectionId) {
          <section class="mb-8" [attr.aria-labelledby]="'section-' + section.sectionId">
            @if (section.title || section.description) {
              <div class="mb-4">
                @if (section.title) {
                  <h2 [id]="'section-' + section.sectionId" class="text-2xl font-extrabold text-slate-900 tracking-tight">{{ section.title }}</h2>
                }
                @if (section.description) {
                  <p class="text-sm text-slate-500 mt-2 leading-relaxed">{{ section.description }}</p>
                }
              </div>
            }

            @for (group of section.groups; track group.groupId) {
              <fieldset class="mb-6 bg-white rounded-2xl shadow-sm border border-slate-100 p-5 sm:p-6">
                @if (group.title) {
                  <legend class="text-sm font-bold text-slate-800 uppercase tracking-wider mb-1 px-0 flex items-center gap-2">
                    <i class="pi pi-list text-indigo-500 text-xs"></i>
                    {{ group.title }}
                  </legend>
                }
                @if (group.description) {
                  <p class="text-xs text-slate-400 mb-4">{{ group.description }}</p>
                }

                @if (group.questions.length === 0) {
                  <p class="text-xs text-slate-400 italic py-2 text-center" role="status">No questions in this group</p>
                }

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-4">
                  @for (question of group.questions; track question.questionId) {
                    @if (isQuestionVisible(question)) {
                      <div class="mb-1" [class.sm:col-span-2]="isWideQuestion(question.type)">
                        <div class="flex items-start justify-between gap-2 mb-1.5">
                          <label class="block text-xs font-bold text-slate-600 uppercase tracking-wider"
                                 [attr.for]="'q-' + question.questionId"
                                 [id]="'label-' + question.questionId">
                            {{ question.text }}
                            @if (question.required) {
                              <span class="text-rose-500 ml-0.5" aria-label="required">*</span>
                            }
                          </label>
                          @if (question.conditions?.length) {
                            <span class="text-xs text-amber-500 shrink-0" title="Conditional question">
                              <i class="pi pi-sliders-h" aria-hidden="true"></i>
                            </span>
                          }
                        </div>
                        @if (question.description) {
                          <p class="text-xs text-slate-400 mb-1.5" [id]="'desc-' + question.questionId">{{ question.description }}</p>
                        }

                        <div>
                          @switch (question.type) {
                            @case ('text') {
                              <input
                                type="text"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-describedby]="(question.description ? 'desc-' + question.questionId + ' ' : '') + 'errors-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                                [attr.placeholder]="question.placeholder || 'Enter your answer'" />
                            }
                            @case ('email') {
                              <input
                                type="email"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-describedby]="'errors-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                                [attr.placeholder]="question.placeholder || 'email@example.com'" />
                            }
                            @case ('phone') {
                              <input
                                type="tel"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-describedby]="'errors-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                                [attr.placeholder]="question.placeholder || '(555) 123-4567'" />
                            }
                            @case ('number') {
                              <p-inputNumber
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [inputId]="'q-' + question.questionId"
                                styleClass="w-full"
                                inputStyleClass="!w-full !text-sm !px-3 !py-2 !border !border-slate-200 !rounded-xl"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-describedby]="'errors-' + question.questionId"
                                [attr.aria-required]="question.required" />
                            }
                            @case ('textarea') {
                              <textarea
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-describedby]="'errors-' + question.questionId"
                                [attr.aria-required]="question.required"
                                rows="3"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                                [attr.placeholder]="question.placeholder || 'Enter your answer'"></textarea>
                            }
                            @case ('date') {
                              <input
                                type="date"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400" />
                            }
                            @case ('datetime') {
                              <input
                                type="datetime-local"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400" />
                            }
                            @case ('select') {
                              <select
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                [attr.aria-required]="question.required"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400">
                                <option [ngValue]="null">{{ question.placeholder || 'Select an option' }}</option>
                                @for (opt of (question.options || []); track opt) {
                                  <option [ngValue]="opt">{{ opt }}</option>
                                }
                              </select>
                            }
                            @case ('multiselect') {
                              <p-multiSelect
                                [options]="question.options || []"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [inputId]="'q-' + question.questionId"
                                [placeholder]="question.placeholder || 'Select options'"
                                styleClass="w-full !rounded-xl !border-slate-200"
                                [attr.aria-labelledby]="'label-' + question.questionId" />
                            }
                            @case ('checkbox') {
                              <div class="flex flex-wrap gap-x-5 gap-y-2" role="group" [attr.aria-labelledby]="'label-' + question.questionId">
                                @for (opt of (question.options || []); track opt) {
                                  <label class="inline-flex items-center gap-2 text-sm text-slate-700 cursor-pointer select-none">
                                    <input
                                      type="checkbox"
                                      [id]="question.questionId + '_' + opt"
                                      [checked]="(answers()[question.questionId] || []).includes(opt)"
                                      (change)="toggleCheckboxOption(question.questionId, opt, $any($event.target).checked)"
                                      class="w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-2 focus:ring-indigo-500/30" />
                                    {{ opt }}
                                  </label>
                                }
                              </div>
                            }
                            @case ('radio') {
                              <div class="flex flex-wrap gap-x-5 gap-y-2" role="radiogroup" [attr.aria-labelledby]="'label-' + question.questionId">
                                @for (opt of (question.options || []); track opt) {
                                  <label class="inline-flex items-center gap-2 text-sm text-slate-700 cursor-pointer select-none">
                                    <input
                                      type="radio"
                                      [name]="question.questionId"
                                      [value]="opt"
                                      [checked]="answers()[question.questionId] === opt"
                                      (change)="updateAnswer(question.questionId, opt)"
                                      [id]="question.questionId + '_' + opt"
                                      class="w-4 h-4 border-slate-300 text-indigo-600 focus:ring-2 focus:ring-indigo-500/30"
                                      [attr.aria-label]="opt" />
                                    {{ opt }}
                                  </label>
                                }
                              </div>
                            }
                            @case ('boolean') {
                              <label class="inline-flex items-center gap-2 text-sm text-slate-700 cursor-pointer select-none">
                                <input
                                  type="checkbox"
                                  [ngModel]="answers()[question.questionId]"
                                  (ngModelChange)="updateAnswer(question.questionId, $event)"
                                  [attr.id]="'q-' + question.questionId"
                                  [attr.aria-labelledby]="'label-' + question.questionId"
                                  class="w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-2 focus:ring-indigo-500/30" />
                                Yes
                              </label>
                            }
                            @case ('file') {
                              <div class="flex items-center gap-3 px-4 py-6 border-2 border-dashed border-slate-200 rounded-xl bg-slate-50 cursor-not-allowed"
                                   role="status" aria-label="File upload placeholder">
                                <i class="pi pi-upload text-slate-300 text-xl" aria-hidden="true"></i>
                                <div>
                                  <p class="text-sm text-slate-400 font-medium m-0">File upload is not available</p>
                                  <p class="text-xs text-slate-300 m-0">This feature will be available in a future update</p>
                                </div>
                              </div>
                            }
                            @case ('fileupload') {
                              <div class="flex items-center gap-3 px-4 py-6 border-2 border-dashed border-slate-200 rounded-xl bg-slate-50 cursor-not-allowed"
                                   role="status" aria-label="File upload placeholder">
                                <i class="pi pi-upload text-slate-300 text-xl" aria-hidden="true"></i>
                                <div>
                                  <p class="text-sm text-slate-400 font-medium m-0">File upload is not available</p>
                                  <p class="text-xs text-slate-300 m-0">This feature will be available in a future update</p>
                                </div>
                              </div>
                            }
                            @case ('painpoint') {
                              <div class="flex flex-col gap-3 bg-slate-50 border border-slate-100 rounded-xl p-3">
                                <div class="flex items-center gap-3">
                                  <label class="text-xs font-semibold text-slate-500 w-24 shrink-0">Intensity</label>
                                  <p-selectButton
                                    [options]="painScaleOptions"
                                    [ngModel]="answers()[question.questionId]?.intensity"
                                    (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, bodyPart: '', ...(answers()[question.questionId] || {}), intensity: $event })"
                                    styleClass="p-selectbutton-sm" />
                                </div>
                                <div class="flex items-center gap-3">
                                  <label class="text-xs font-semibold text-slate-500 w-24 shrink-0">Region</label>
                                  <input
                                    type="text"
                                    [ngModel]="answers()[question.questionId]?.anatomicalRegion || answers()[question.questionId]?.bodyPart"
                                    (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: $event, bodyPart: $event, intensity: answers()[question.questionId]?.intensity ?? 5 })"
                                    placeholder="e.g. lumbar spine, shoulder, knee"
                                    class="w-full text-sm px-3 py-1.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/30" />
                                </div>
                                <div class="flex items-center gap-3">
                                  <label class="text-xs font-semibold text-slate-500 w-24 shrink-0">Side</label>
                                  <input
                                    type="text"
                                    [ngModel]="answers()[question.questionId]?.side"
                                    (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: answers()[question.questionId]?.anatomicalRegion ?? answers()[question.questionId]?.bodyPart ?? '', bodyPart: answers()[question.questionId]?.bodyPart ?? answers()[question.questionId]?.anatomicalRegion ?? '', side: $event, intensity: answers()[question.questionId]?.intensity ?? 5, description: answers()[question.questionId]?.description ?? '' })"
                                    [attr.placeholder]="question.placeholder || 'left, right, bilateral'"
                                    class="w-full text-sm px-3 py-1.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/30" />
                                </div>
                                <div class="flex items-center gap-3">
                                  <label class="text-xs font-semibold text-slate-500 w-24 shrink-0">Description</label>
                                  <input
                                    type="text"
                                    [ngModel]="answers()[question.questionId]?.description"
                                    (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: answers()[question.questionId]?.anatomicalRegion ?? answers()[question.questionId]?.bodyPart ?? '', bodyPart: answers()[question.questionId]?.bodyPart ?? answers()[question.questionId]?.anatomicalRegion ?? '', side: answers()[question.questionId]?.side ?? '', intensity: answers()[question.questionId]?.intensity ?? 5, description: $event })"
                                    [attr.placeholder]="question.placeholder || 'Describe the pain'"
                                    class="w-full text-sm px-3 py-1.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/30" />
                                </div>
                              </div>
                            }
                            @default {
                              <input
                                type="text"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [attr.id]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId"
                                class="w-full text-sm px-3 py-2 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-400"
                                [attr.placeholder]="question.placeholder || 'Enter your answer'" />
                            }
                          }
                        </div>

                        @if (question.helpText) {
                          <p class="text-xs text-slate-400 mt-1.5" [id]="'help-' + question.questionId">{{ question.helpText }}</p>
                        }

                        @if (getQuestionErrors(question).length > 0 && isTouched(question.questionId)) {
                          <div class="mt-1.5" [id]="'errors-' + question.questionId" role="alert">
                            @for (err of getQuestionErrors(question); track err) {
                              <p class="text-xs text-rose-500 font-medium flex items-center gap-1 m-0">
                                <i class="pi pi-exclamation-circle text-xs"></i>
                                {{ err }}
                              </p>
                            }
                          </div>
                        }
                      </div>
                    }
                  }
                </div>
              </fieldset>
            }
          </section>
        }
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
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