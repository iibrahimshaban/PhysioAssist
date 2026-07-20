export interface DynamicFormSchemaDto {
  schemaVersion: number;
  sections: FormSectionDto[];
}

export interface FormSectionDto {
  sectionId: string;
  title: string;
  description?: string;
  order: number;
  groups: FormGroupDto[];
}

export interface FormGroupDto {
  groupId: string;
  title: string;
  description?: string;
  order: number;
  questions: FormQuestionDto[];
}

export interface FormQuestionDto {
  questionId: string;
  text: string;
  description?: string;
  type: string;
  order: number;
  required: boolean;
  placeholder?: string;
  helpText?: string;
  options?: string[];
  validationRules?: ValidationRuleDto[];
  conditions?: QuestionConditionDto[];
}

export interface ValidationRuleDto {
  ruleType: string;
  value?: any;
  message?: string;
}

export interface QuestionConditionDto {
  targetQuestionId: string;
  operator: string;
  value?: string;
}

export interface DynamicFormSubmissionDto {
  schemaVersion: number;
  formSchemaId: string;
  formSchemaVersion: number;
  sections: SubmissionSectionDto[];
}

export interface SubmissionSectionDto {
  sectionId: string;
  groups: SubmissionGroupDto[];
}

export interface SubmissionGroupDto {
  groupId: string;
  answers: SubmissionAnswerDto[];
}

export interface SubmissionAnswerDto {
  questionId: string;
  value?: any;
  notes?: string;
  attachments?: AttachmentAnswerDto[];
}

export interface AttachmentAnswerDto {
  fileName: string;
  fileUrl: string;
  fileType: string;
  fileSize: number;
}

export interface PainPointDto {
  x: number;
  y: number;
  intensity: number;
  description?: string;
  anatomicalRegion?: string;
  side?: string;
  specificLocation?: string;
  bodyPart?: string;
}
