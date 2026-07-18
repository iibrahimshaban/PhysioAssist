import { Component, Input, Output, EventEmitter, computed, inject } from '@angular/core';
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
    if (answer.value == null) return '—';

    if (typeof answer.value === 'object' && !Array.isArray(answer.value)) {
      const dict = answer.value as Record<string, any>;
      const keys = Object.keys(dict);
      if (keys.length === 1) {
        const inner = dict[keys[0]];
        if (inner == null) return '—';
        if (Array.isArray(inner)) return inner.length === 0 ? '—' : inner.join(', ');
        if (typeof inner === 'boolean') return inner ? 'Yes' : 'No';
        return String(inner);
      }
      return String(answer.value);
    }

    if (Array.isArray(answer.value)) {
      if (answer.value.length === 0) return '—';
      return answer.value.join(', ');
    }
    if (typeof answer.value === 'boolean') return answer.value ? 'Yes' : 'No';
    return String(answer.value);
  }
}
