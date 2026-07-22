import { Injectable } from '@angular/core';
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
      bodyselector: 'pi pi-user'
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
      errors.push({ path: 'schemaVersion', message: 'Schema version must be at least 1' });
    }
    if (!schema.sections || schema.sections.length === 0) {
      errors.push({ path: 'sections', message: 'Schema must have at least one section' });
    }

    const allIds = new Set<string>();
    for (const section of schema.sections) {
      if (allIds.has(section.sectionId)) {
        errors.push({ path: `sections`, message: `Duplicate section ID: ${section.sectionId}` });
      }
      allIds.add(section.sectionId);

      for (const group of section.groups) {
        if (allIds.has(group.groupId)) {
          errors.push({ path: `groups`, message: `Duplicate group ID: ${group.groupId}` });
        }
        allIds.add(group.groupId);

        for (const question of group.questions) {
          if (allIds.has(question.questionId)) {
            errors.push({ path: `questions`, message: `Duplicate question ID: ${question.questionId}` });
          }
          allIds.add(question.questionId);

          if (question.conditions) {
            const allQ = this.getAllQuestions(schema);
            for (const condition of question.conditions) {
              if (!allQ.find(q => q.questionId === condition.targetQuestionId)) {
                errors.push({
                  path: `questions.${question.questionId}.conditions`,
                  message: `Condition references non-existent question: ${condition.targetQuestionId}`
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
}
