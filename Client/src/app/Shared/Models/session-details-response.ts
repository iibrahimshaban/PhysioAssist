export interface SessionDetailsResponse {
  id: string;
  patientName: string;
  slotStart: string;
  slotEnd: string;
  durationInMinutes: number;
  status: number;
  editedTranscript: string;
  attachments: Attachment[];
  audioFileUrl: string | null;
}

interface Attachment {
  id: string;
  fileUrl: string;
  fileName: string;
  fileType: string;
}
