export interface DocumentVersion {
  id: number;
  documentId: number;
  versionNumber: number;
  title: string;
  description: string;
  content: string;
  createdAt: Date;
  createdByUserId: string;
}

export interface CreateDocumentVersionRequest {
  note?: string;
}
