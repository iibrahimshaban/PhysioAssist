import { Component, Input, Output, EventEmitter, computed, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DynamicFormRendererComponent } from '../../../components/dynamic-form-renderer/dynamic-form-renderer.component';
import { DynamicFormEngineService } from '../../../services/dynamic-form-engine.service';
import {
  DynamicFormSchemaDto,
  DynamicFormSubmissionDto,
  PreVisitIntakeDetailsResponse,
  FormQuestionDto,
  SubmissionAnswerDto
} from '../../../models';

@Component({
  selector: 'app-submitted-answers-viewer',
  standalone: true,
  imports: [CommonModule, DynamicFormRendererComponent],
  templateUrl: './submitted-answers-viewer.component.html',
  styleUrl: './submitted-answers-viewer.component.css'
})
export class SubmittedAnswersViewerComponent {
  private readonly engine = inject(DynamicFormEngineService);

  @Input({ required: true }) isEditing = false;
  @Input() schema: DynamicFormSchemaDto | null = null;
  @Input() submissionData: DynamicFormSubmissionDto | null = null;
  @Input({ required: true }) details!: PreVisitIntakeDetailsResponse;
  @Input() initialAnswers: Record<string, any> = {};

  @Output() submissionChange = new EventEmitter<DynamicFormSubmissionDto>();
  @Output() validityChange = new EventEmitter<boolean>();

  private readonly questionMap = computed<Record<string, FormQuestionDto>>(() => {
    const s = this.schema;
    if (!s) return {};
    const map: Record<string, FormQuestionDto> = {};
    for (const q of this.engine.getAllQuestions(s)) {
      map[q.questionId] = q;
    }
    return map;
  });

  private readonly sectionMap = computed<Record<string, string>>(() => {
    const s = this.schema;
    if (!s) return {};
    const map: Record<string, string> = {};
    for (const section of s.sections) {
      map[section.sectionId] = section.title;
    }
    return map;
  });

  private readonly groupMap = computed<Record<string, string>>(() => {
    const s = this.schema;
    if (!s) return {};
    const map: Record<string, string> = {};
    for (const section of s.sections) {
      for (const group of section.groups) {
        map[group.groupId] = group.title;
      }
    }
    return map;
  });

  private readonly answerMap = computed<Record<string, SubmissionAnswerDto>>(() => {
    const data = this.submissionData;
    const map: Record<string, SubmissionAnswerDto> = {};
    if (!data) return map;
    for (const section of data.sections) {
      for (const group of section.groups) {
        for (const answer of group.answers) {
          map[answer.questionId] = answer;
        }
      }
    }
    return map;
  });

  getAnswerFor(questionId: string): SubmissionAnswerDto {
    return this.answerMap()[questionId] ?? { questionId, value: undefined };
  }

  getSectionTitle(sectionId: string): string | undefined {
    return this.sectionMap()[sectionId];
  }

  getGroupTitle(groupId: string): string | undefined {
    return this.groupMap()[groupId];
  }

  getQuestionText(questionId: string): string | undefined {
    return this.questionMap()[questionId]?.text;
  }

  formatAnswerValue(answer: SubmissionAnswerDto): string {
    return this.formatValueRecursive(answer?.value);
  }

  formatValueRecursive(val: any): string {
    if (val == null || val === '') return '—';

    // Booleans
    if (typeof val === 'boolean') return val ? 'Yes' : 'No';

    // Primitives (strings, numbers)
    if (typeof val === 'string' || typeof val === 'number') {
      const str = String(val).trim();
      return str || '—';
    }

    // Arrays
    if (Array.isArray(val)) {
      if (val.length === 0) return '—';
      const items = val.map(item => this.formatValueRecursive(item)).filter(item => item !== '—');
      return items.length > 0 ? items.join(', ') : '—';
    }

    // Objects
    if (typeof val === 'object') {
      const dict = val as Record<string, any>;
      const keys = Object.keys(dict);
      if (keys.length === 0) return '—';

      // Single key object e.g. { "value": "xyz" } or { "text": "xyz" }
      if (keys.length === 1) {
        return this.formatValueRecursive(dict[keys[0]]);
      }

      // Multiple keys e.g. { value: "Option 1", notes: "Detail" }
      const pairs: string[] = [];
      for (const key of keys) {
        const itemVal = dict[key];
        if (itemVal != null && itemVal !== '') {
          const formatted = this.formatValueRecursive(itemVal);
          if (formatted !== '—') {
            const cleanKey = key
              .replace(/([A-Z])/g, ' $1')
              .replace(/^./, str => str.toUpperCase())
              .trim();
            pairs.push(`${cleanKey}: ${formatted}`);
          }
        }
      }
      return pairs.length > 0 ? pairs.join(' · ') : '—';
    }

    return String(val);
  }

  @ViewChild(DynamicFormRendererComponent) private formRenderer?: DynamicFormRendererComponent;

  markAllTouched(): void {
    this.formRenderer?.markAllAsTouched();
  }
}
