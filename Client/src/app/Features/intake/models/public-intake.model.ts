export interface GenerateIntakeQrLinkRequest {
  expiryHours: number;
}

export interface GenerateIntakeQrLinkResponse {
  token: string;
  publicUrl: string;
  expiresAt: string;
}

export interface PublicIntakeFormResponse {
  formSchemaId: string;
  formName: string;
  formDescription?: string;
  schemaJson: string;
  version: number;
  showPainMap:boolean;
}

export interface PublicIntakeSubmissionResponse {
  submissionId: string;
  submittedAt: string;
  message: string;
}
