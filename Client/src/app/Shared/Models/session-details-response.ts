export interface SessionDetailsResponse {
  id: string;
  patientName: string;
  PatientId:string;
  slotStart: string;
  slotEnd: string;
  durationInMinutes: number;
  status: number;
  editedTranscript: string;
  attachments: Attachment[];
  audioFileUrl: string | null;
  treatmentPlan: string | null;
}

export interface Attachment {
  id: string;
  fileUrl: string;
  fileName: string;
  fileType: string;
}
