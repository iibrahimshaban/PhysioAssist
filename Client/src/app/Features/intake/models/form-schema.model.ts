export enum FormSchemaStatus {
  Draft = 0,
  Published = 1,
  Archived = 2
}

export interface FormSchemaResponse {
  id: string;
  name: string;
  description?: string;
  schemaJson: string;
  doctorId: string;
  version: number;
  status: FormSchemaStatus;
  isDefault: boolean;
  schemaHash: string;
  publishedAt?: string;
  createdAt: string;
  updatedAt?: string;
  shortCode: string;
  originalFormId?: string;
  originalName?: string;
  copyNumber?: number;
  showPainMap?: boolean;
}

export interface FormSchemaSummaryResponse {
  id: string;
  name: string;
  description?: string;
  version: number;
  status: FormSchemaStatus;
  isDefault: boolean;
  publishedAt?: string;
  createdAt: string;
  shortCode: string;
  originalFormId?: string;
  originalName?: string;
  copyNumber?: number;
  submissionCount?: number;
  fieldsCount?: number;
}

export interface CreateFormSchemaRequest {
  name: string;
  description?: string;
  schemaJson: string;
  isDefault: boolean;
  showPainMap?: boolean;
}

export interface UpdateFormSchemaRequest {
  name: string;
  description?: string;
  schemaJson: string;
  isDefault: boolean;
  showPainMap?: boolean;
}

export interface PublishFormSchemaRequest {
  version: number;
}
