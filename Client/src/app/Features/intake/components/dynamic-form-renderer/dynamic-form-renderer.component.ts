import { Component, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MessageModule } from 'primeng/message';
import { BodySelectorComponent } from '../body-selector/body-selector.component';
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
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    SelectModule,
    MultiSelect,
    CheckboxModule,
    SelectButtonModule,
    MessageModule,
    BodySelectorComponent
  ],
  template: `
    <div class="dynamic-form-renderer" role="form" aria-label="Dynamic intake form">
      @if (!schema()) {
        <div class="text-center py-12 text-surface-400" role="status">
          <i class="pi pi-inbox text-4xl block mb-3"></i>
          <p>No form schema provided.</p>
        </div>
      } @else if ((schema()?.sections?.length ?? 0) === 0) {
        <div class="text-center py-12 text-surface-400" role="status">
          <i class="pi pi-file text-4xl block mb-3"></i>
          <p>This form has no sections defined yet.</p>
        </div>
      } @else {
        @for (section of schema()!.sections; track section.sectionId) {
          <section class="mb-6" [attr.aria-labelledby]="'section-' + section.sectionId">
            <div class="mb-3">
              <h2 [id]="'section-' + section.sectionId" class="text-xl font-bold text-surface-800">{{ section.title }}</h2>
              @if (section.description) {
                <p class="text-sm text-surface-500 mt-1">{{ section.description }}</p>
              }
            </div>

            @for (group of section.groups; track group.groupId) {
              <fieldset class="mb-4 p-4 border border-surface-200 rounded-lg bg-white">
                @if (group.title) {
                  <legend class="text-base font-semibold text-surface-700 px-1">{{ group.title }}</legend>
                }
                @if (group.description) {
                  <p class="text-sm text-surface-500 mb-3">{{ group.description }}</p>
                }

                @if (group.questions.length === 0) {
                  <p class="text-xs text-surface-400 italic py-2 text-center" role="status">No questions in this group</p>
                }

                @for (question of group.questions; track question.questionId) {
                  @if (isQuestionVisible(question)) {
                    <div class="mb-4 last:mb-0">
                      <div class="flex items-start justify-between">
                        <label class="block text-sm font-medium text-surface-700 mb-1"
                               [attr.for]="'q-' + question.questionId"
                               [id]="'label-' + question.questionId">
                          {{ question.text }}
                          @if (question.required) {
                            <span class="text-red-500 ml-0.5" aria-label="required">*</span>
                          }
                        </label>
                        @if (question.conditions?.length) {
                          <span class="text-xs text-orange-500 shrink-0 ml-2" title="Conditional question">
                            <i class="pi pi-sliders-h" aria-hidden="true"></i>
                          </span>
                        }
                      </div>
                      @if (question.description) {
                        <p class="text-xs text-surface-500 mb-2" [id]="'desc-' + question.questionId">{{ question.description }}</p>
                      }

                      <div class="mb-2">
                        @switch (question.type) {
                          @case ('text') {
                            <input
                              type="text"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-describedby]="(question.description ? 'desc-' + question.questionId + ' ' : '') + 'errors-' + question.questionId"
                              [attr.aria-required]="question.required"
                              class="w-full"
                              [attr.placeholder]="question.placeholder || 'Enter your answer'" />
                          }
                          @case ('email') {
                            <input
                              type="email"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-describedby]="'errors-' + question.questionId"
                              [attr.aria-required]="question.required"
                              class="w-full"
                              [attr.placeholder]="question.placeholder || 'email@example.com'" />
                          }
                          @case ('phone') {
                            <input
                              type="tel"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-describedby]="'errors-' + question.questionId"
                              [attr.aria-required]="question.required"
                              class="w-full"
                              [attr.placeholder]="question.placeholder || '(555) 123-4567'" />
                          }
                          @case ('number') {
                            <p-inputNumber
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [inputId]="'q-' + question.questionId"
                              styleClass="w-full"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-describedby]="'errors-' + question.questionId"
                              [attr.aria-required]="question.required" />
                          }
                          @case ('textarea') {
                            <textarea
                              pTextarea
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-describedby]="'errors-' + question.questionId"
                              [attr.aria-required]="question.required"
                              rows="3"
                              class="w-full"
                              [attr.placeholder]="question.placeholder || 'Enter your answer'"></textarea>
                          }
                          @case ('date') {
                            <input
                              type="date"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-required]="question.required"
                              class="w-full" />
                          }
                          @case ('datetime') {
                            <input
                              type="datetime-local"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              [attr.aria-required]="question.required"
                              class="w-full" />
                          }
                          @case ('select') {
                            <p-select
                              [options]="question.options || []"
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [inputId]="'q-' + question.questionId"
                              placeholder="Select an option"
                              class="w-full"
                              [attr.aria-labelledby]="'label-' + question.questionId" />
                          }
                          @case ('multiselect') {
                            <p-multiSelect
                              [options]="question.options || []"
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [inputId]="'q-' + question.questionId"
                              [placeholder]="question.placeholder || 'Select options'"
                              class="w-full"
                              [attr.aria-labelledby]="'label-' + question.questionId" />
                          }
                          @case ('checkbox') {
                            <div class="space-y-1.5" role="group" [attr.aria-labelledby]="'label-' + question.questionId">
                              @for (opt of (question.options || []); track opt) {
                                <div class="flex items-center gap-2">
                                  <p-checkbox
                                    [inputId]="question.questionId + '_' + opt"
                                    [value]="opt"
                                    [ngModel]="answers()[question.questionId] || []"
                                    (ngModelChange)="updateAnswer(question.questionId, $event)"
                                    [binary]="false" />
                                  <label [for]="question.questionId + '_' + opt" class="text-sm text-surface-700 cursor-pointer">{{ opt }}</label>
                                </div>
                              }
                            </div>
                          }
                          @case ('radio') {
                            <div class="space-y-1.5" role="radiogroup" [attr.aria-labelledby]="'label-' + question.questionId">
                              @for (opt of (question.options || []); track opt) {
                                <div class="flex items-center gap-2">
                                  <input
                                    type="radio"
                                    [name]="question.questionId"
                                    [value]="opt"
                                    [checked]="answers()[question.questionId] === opt"
                                    (change)="updateAnswer(question.questionId, opt)"
                                    [attr.id]="question.questionId + '_' + opt"
                                    class="border-surface-300"
                                    [attr.aria-label]="opt" />
                                  <label [for]="question.questionId + '_' + opt" class="text-sm text-surface-700 cursor-pointer">{{ opt }}</label>
                                </div>
                              }
                            </div>
                          }
                          @case ('boolean') {
                            <div class="flex items-center gap-2">
                              <p-checkbox
                                [binary]="true"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                [inputId]="'q-' + question.questionId"
                                [attr.aria-labelledby]="'label-' + question.questionId" />
                              <label [for]="'q-' + question.questionId" class="text-sm text-surface-700 cursor-pointer">Yes</label>
                            </div>
                          }
                          @case ('file') {
                            <div class="flex items-center gap-3 px-4 py-6 border-2 border-dashed border-surface-300 rounded-md bg-surface-50 cursor-not-allowed"
                                 role="status" aria-label="File upload placeholder">
                              <i class="pi pi-upload text-surface-300 text-xl" aria-hidden="true"></i>
                              <div>
                                <p class="text-sm text-surface-400 font-medium">File upload is not available</p>
                                <p class="text-xs text-surface-300">This feature will be available in a future update</p>
                              </div>
                            </div>
                          }
                          @case ('fileupload') {
                            <div class="flex items-center gap-3 px-4 py-6 border-2 border-dashed border-surface-300 rounded-md bg-surface-50 cursor-not-allowed"
                                 role="status" aria-label="File upload placeholder">
                              <i class="pi pi-upload text-surface-300 text-xl" aria-hidden="true"></i>
                              <div>
                                <p class="text-sm text-surface-400 font-medium">File upload is not available</p>
                                <p class="text-xs text-surface-300">This feature will be available in a future update</p>
                              </div>
                            </div>
                          }
                          @case ('painpoint') {
                            <div class="flex flex-col gap-2">
                              <div class="flex items-center gap-3">
                                <label class="text-xs text-surface-500 w-16">Intensity</label>
                                <p-selectButton
                                  [options]="painScaleOptions"
                                  [ngModel]="answers()[question.questionId]?.intensity"
                                  (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, bodyPart: '', ...(answers()[question.questionId] || {}), intensity: $event })"
                                  styleClass="p-selectbutton-sm" />
                              </div>
                              <div class="flex items-center gap-3">
                                <label class="text-xs text-surface-500 w-16">Anatomical Region</label>
                                <input
                                  type="text"
                                  pInputText
                                  [ngModel]="answers()[question.questionId]?.anatomicalRegion || answers()[question.questionId]?.bodyPart"
                                  (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: $event, bodyPart: $event, intensity: answers()[question.questionId]?.intensity ?? 5 })"
                                  placeholder="e.g. lumbar spine, shoulder, knee"
                                  class="w-full text-sm" />
                              </div>
                              <div class="flex items-center gap-3">
                                <label class="text-xs text-surface-500 w-16">Side</label>
                                <input
                                  type="text"
                                  pInputText
                                  [ngModel]="answers()[question.questionId]?.side"
                                  (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: answers()[question.questionId]?.anatomicalRegion ?? answers()[question.questionId]?.bodyPart ?? '', bodyPart: answers()[question.questionId]?.bodyPart ?? answers()[question.questionId]?.anatomicalRegion ?? '', side: $event, intensity: answers()[question.questionId]?.intensity ?? 5, description: answers()[question.questionId]?.description ?? '' })"
                                  [attr.placeholder]="question.placeholder || 'left, right, bilateral'"
                                  class="w-full text-sm" />
                              </div>
                              <div class="flex items-center gap-3">
                                <label class="text-xs text-surface-500 w-16">Description</label>
                                <input
                                  type="text"
                                  pInputText
                                  [ngModel]="answers()[question.questionId]?.description"
                                  (ngModelChange)="updateAnswer(question.questionId, { x: 0, y: 0, anatomicalRegion: answers()[question.questionId]?.anatomicalRegion ?? answers()[question.questionId]?.bodyPart ?? '', bodyPart: answers()[question.questionId]?.bodyPart ?? answers()[question.questionId]?.anatomicalRegion ?? '', side: answers()[question.questionId]?.side ?? '', intensity: answers()[question.questionId]?.intensity ?? 5, description: $event })"
                                  [attr.placeholder]="question.placeholder || 'Describe the pain'"
                                  class="w-full text-sm" />
                              </div>
                            </div>
                          }
                          @case ('bodyselector') {
                            <app-body-selector
                              (painPointsChange)="updateAnswer(question.questionId, $event)" />
                          }
                          @case ('painscale') {
                            <div class="flex flex-wrap items-center gap-3">
                              <p-selectButton
                                [options]="painScaleOptions"
                                [ngModel]="answers()[question.questionId]"
                                (ngModelChange)="updateAnswer(question.questionId, $event)"
                                styleClass="p-selectbutton-sm"
                                [attr.aria-labelledby]="'label-' + question.questionId" />
                              @if (answers()[question.questionId] != null) {
                                <span class="text-sm text-surface-600" aria-live="polite">/ 10</span>
                              }
                            </div>
                          }
                          @default {
                            <input
                              type="text"
                              pInputText
                              [ngModel]="answers()[question.questionId]"
                              (ngModelChange)="updateAnswer(question.questionId, $event)"
                              [attr.id]="'q-' + question.questionId"
                              [attr.aria-labelledby]="'label-' + question.questionId"
                              class="w-full"
                              [attr.placeholder]="question.placeholder || 'Enter your answer'" />
                          }
                        }
                      </div>

                      @if (question.helpText) {
                        <p class="text-xs text-surface-400 mt-1" [id]="'help-' + question.questionId">{{ question.helpText }}</p>
                      }

                      @if (getQuestionErrors(question).length > 0 && isTouched(question.questionId)) {
                        <div class="mb-2" [id]="'errors-' + question.questionId" role="alert">
                          @for (err of getQuestionErrors(question); track err) {
                            <p-message severity="error" [text]="err" styleClass="!text-xs !py-1 !px-2 !mb-1" />
                          }
                        </div>
                      }

                      <div class="flex items-start gap-2" [id]="'notes-' + question.questionId">
                        <i class="pi pi-comment text-surface-300 text-xs mt-1.5" aria-hidden="true"></i>
                        <input
                          type="text"
                          pInputText
                          [ngModel]="notes()[question.questionId]"
                          (ngModelChange)="updateNote(question.questionId, $event)"
                          class="w-full text-xs"
                          [attr.aria-label]="'Notes for ' + question.text"
                          placeholder="Add a note..." />
                      </div>
                    </div>
                  }
                }
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

  readonly submissionChange = output<DynamicFormSubmissionDto>();
  readonly validityChange = output<boolean>();

  protected readonly painScaleOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(v => ({ label: v.toString(), value: v }));

  protected readonly answers = signal<Record<string, any>>({});
  protected readonly notes = signal<Record<string, string>>({});
  protected readonly touchedFields = signal<Set<string>>(new Set());

  protected isTouched(questionId: string): boolean {
    return this.touchedFields().has(questionId);
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
    const currentNotes = this.notes();

    const sections: SubmissionSectionDto[] = s.sections.map(section => {
      const groups: SubmissionGroupDto[] = section.groups.map(group => {
        const answers: SubmissionAnswerDto[] = group.questions
          .filter(q => this.isQuestionVisible(q, currentAnswers))
          .map(q => ({
            questionId: q.questionId,
            value: this.wrapTypes.has(q.type)
              ? { [q.type]: currentAnswers[q.questionId] }
              : currentAnswers[q.questionId],
            notes: currentNotes[q.questionId] || undefined,
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
    this.touchedFields.update(set => { set.add(questionId); return new Set(set); });
    this.answers.update(current => ({ ...current, [questionId]: value }));
    this.emitOutputs();
  }

  protected updateNote(questionId: string, value: string): void {
    this.notes.update(current => ({ ...current, [questionId]: value }));
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
