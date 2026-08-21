import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import {
  DynamicFormSchemaDto,
  FormSectionDto,
  FormGroupDto,
  FormQuestionDto
} from '../models';

export interface ValidationError {
  path: string;
  message: string;
}

export interface ConditionOperator {
  label: string;
  value: string;
}

export interface ValidationRuleType {
  label: string;
  value: string;
  hasValue: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class DynamicFormEngineService {
  private readonly transloco = inject(TranslocoService);

  readonly conditionOperators: ConditionOperator[] = [
    { label: 'Equals', value: 'equals' },
    { label: 'Not Equals', value: 'notEquals' },
    { label: 'Contains', value: 'contains' },
    { label: 'Greater Than', value: 'greaterThan' },
    { label: 'Less Than', value: 'lessThan' },
    { label: 'In', value: 'in' },
    { label: 'Not In', value: 'notIn' }
  ];

  readonly validationRuleTypes: ValidationRuleType[] = [
    { label: 'Required', value: 'required', hasValue: false },
    { label: 'Pattern', value: 'pattern', hasValue: true },
    { label: 'Min', value: 'min', hasValue: true },
    { label: 'Max', value: 'max', hasValue: true },
    { label: 'Min Length', value: 'minLength', hasValue: true },
    { label: 'Max Length', value: 'maxLength', hasValue: true },
    { label: 'Email', value: 'email', hasValue: false },
    { label: 'URL', value: 'url', hasValue: false }
  ];

  getConditionOperatorLabel(value: string): string {
    return this.conditionOperators.find(o => o.value === value)?.label || value;
  }

  getValidationRuleLabel(value: string): string {
    return this.validationRuleTypes.find(r => r.value === value)?.label || value;
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
      painscale: 'pi pi-chart-bar',
      bodyselector: 'pi pi-user',
      summary: 'pi pi-file-edit'
    };
    return icons[type] || 'pi pi-question';
  }

  generateId(prefix: string): string {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substring(2, 11)}`;
  }

  createDefaultSchema(): DynamicFormSchemaDto {
    return { schemaVersion: 1, sections: [] };
  }

  createDefaultSection(order: number): FormSectionDto {
    return {
      sectionId: this.generateId('section'),
      title: 'New Section',
      description: '',
      order,
      groups: []
    };
  }

  createDefaultGroup(order: number): FormGroupDto {
    return {
      groupId: this.generateId('group'),
      title: 'New Group',
      description: '',
      order,
      questions: []
    };
  }

  createDefaultQuestion(order: number): FormQuestionDto {
    return {
      questionId: this.generateId('question'),
      text: 'New Question',
      description: '',
      type: 'text',
      order,
      required: false,
      options: []
    };
  }

  getAllQuestions(schema: DynamicFormSchemaDto): FormQuestionDto[] {
    return schema.sections.flatMap(s =>
      s.groups.flatMap(g => g.questions)
    );
  }

  validateSchema(schema: DynamicFormSchemaDto): ValidationError[] {
    const errors: ValidationError[] = [];
    if (!schema.schemaVersion || schema.schemaVersion < 1) {
      errors.push({ path: 'schemaVersion', message: this.transloco.translate('intake.engine.validation.versionMin') });
    }
    if (!schema.sections || schema.sections.length === 0) {
      errors.push({ path: 'sections', message: this.transloco.translate('intake.engine.validation.sectionRequired') });
    }

    const allIds = new Set<string>();
    for (const section of schema.sections) {
      if (allIds.has(section.sectionId)) {
        errors.push({ path: `sections`, message: this.transloco.translate('intake.engine.validation.duplicateSection', { id: section.sectionId }) });
      }
      allIds.add(section.sectionId);

      for (const group of section.groups) {
        if (allIds.has(group.groupId)) {
          errors.push({ path: `groups`, message: this.transloco.translate('intake.engine.validation.duplicateGroup', { id: group.groupId }) });
        }
        allIds.add(group.groupId);

        for (const question of group.questions) {
          if (allIds.has(question.questionId)) {
            errors.push({ path: `questions`, message: this.transloco.translate('intake.engine.validation.duplicateQuestion', { id: question.questionId }) });
          }
          allIds.add(question.questionId);

          if (question.conditions) {
            const allQ = this.getAllQuestions(schema);
            for (const condition of question.conditions) {
              if (!allQ.find(q => q.questionId === condition.targetQuestionId)) {
                errors.push({
                  path: `questions.${question.questionId}.conditions`,
                  message: this.transloco.translate('intake.engine.validation.conditionMissingTarget', { id: condition.targetQuestionId })
                });
              }
            }
          }
        }
      }
    }

    return errors;
  }

  computeSchemaHash(schemaJson: string): string {
    let hash = 0;
    for (let i = 0; i < schemaJson.length; i++) {
      const char = schemaJson.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash |= 0;
    }
    return Math.abs(hash).toString(16).padStart(8, '0');
  }

  serializeSchema(schema: DynamicFormSchemaDto): string {
    const cloned = JSON.parse(JSON.stringify(schema)) as DynamicFormSchemaDto;
    for (const section of cloned.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (question.validationRules) {
            for (const rule of question.validationRules) {
              if (typeof rule.value === 'number') {
                rule.value = String(rule.value);
              }
            }
          }
          if (question.conditions) {
            for (const condition of question.conditions) {
              if (typeof condition.value === 'number') {
                condition.value = String(condition.value);
              }
            }
          }
        }
      }
    }
    return JSON.stringify(cloned);
  }

  deserializeSchema(json: string): DynamicFormSchemaDto {
    return JSON.parse(json) as DynamicFormSchemaDto;
  }

  // ── Patient-identifying field helpers ──────────────────────────────────
  // Per project requirements, the canonical email question has locked
  // questionId `question_default_email` and/or user-visible label "Email
  // Address".  A real form may have either (or both) depending on whether
  // the schema was built before or after the locking convention was
  // introduced, so we match both.

  /** Well-known locked question IDs that map to patient email. */
  private readonly EMAIL_QUESTION_IDS = new Set([
    'question_default_email',
    'question_default_email_address',
  ]);

  /** User-visible labels that identify the email field when the questionId
   *  does not use the canonical locked prefix. */
  private readonly EMAIL_QUESTION_TEXTS = new Set([
    'Email',
    'Email Address',
    'Email address',
    'email',
    'email address',
    'E-mail',
    'E-mail Address',
  ]);

  /**
   * Look up the answer value for the email question inside a submission
   * payload.  Returns the trimmed string value (lowercased), or `null` if
   * no email question exists or the answer is still blank/invalid.
   *
   * Matching strategy (robust against schema versions):
   *   1. Try canonical locked question IDs first (O(1), definitive).
   *   2. Fall back to user-visible label matching on the schema metadata
   *      (handles older schemas created before the lock convention).
   */
  extractEmailAnswer(
    schema: DynamicFormSchemaDto,
    submission: { sections: { sectionId: string; groups: { groupId: string; answers: { questionId: string; value?: any }[] }[] }[] } | null,
  ): string | null {
    if (!schema || !submission || !submission.sections) return null;

    // Phase 1 — collect candidate question IDs from the schema.
    let emailQuestionId: string | null = null;

    for (const section of schema.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          const matchesId = this.EMAIL_QUESTION_IDS.has(question.questionId);
          const matchesText = question.text && this.EMAIL_QUESTION_TEXTS.has(question.text.trim());
          if (matchesId || matchesText) {
            emailQuestionId = question.questionId;
            // The locked ID match is strongest, stop immediately when found.
            if (matchesId) break;
          }
        }
        if (emailQuestionId && this.EMAIL_QUESTION_IDS.has(emailQuestionId)) break;
      }
      if (emailQuestionId && this.EMAIL_QUESTION_IDS.has(emailQuestionId)) break;
    }

    if (!emailQuestionId) return null;

    // Phase 2 — extract the answer value from the submission payload.
    for (const section of submission.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          if (answer.questionId === emailQuestionId && answer.value != null) {
            const raw = answer.value;

            // The renderer wraps "wrapTypes" answers as { [questionType]: value },
            // e.g. { email: "someone@example.com" }. Unwrap that shape here;
            // fall back to the raw value for older/unwrapped submissions.
            const unwrapped =
              raw && typeof raw === 'object' && 'email' in raw
                ? (raw as { email: unknown }).email
                : raw;

            if (unwrapped == null) return null;

            const trimmed = String(unwrapped).trim();
            if (trimmed.length === 0) return null;
            return trimmed.toLowerCase();
          }
        }
      }
    }

    return null;
  }

  /**
   * Find a question in a schema by matching the user-visible text.
   * Mirrors the server-side `ExtractInputValuesHelper.FindQuestionIdByText`
   * convention (see project memory).  Returns the question object, or
   * `null` when no match is found.
   */
  findQuestionByText(schema: DynamicFormSchemaDto, text: string): FormQuestionDto | null {
    const target = text.trim().toLowerCase();
    for (const section of schema.sections) {
      for (const group of section.groups) {
        for (const question of group.questions) {
          if (question.text?.trim().toLowerCase() === target) return question;
        }
      }
    }
    return null;
  }
}
