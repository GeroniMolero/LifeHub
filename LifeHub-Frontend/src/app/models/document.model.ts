export interface Document {
  id: number;
  userId: string;
  creativeSpaceId?: number;
  title: string;
  description: string;
  content: string;
  type: DocumentType;
  isPublic?: boolean;
  publishedAt?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export enum DocumentType {
  Note = 0,
  TextFile = 1,
  List = 2
}

export interface CreateDocumentRequest {
  title: string;
  description: string;
  content: string;
  type: DocumentType;
  creativeSpaceId?: number;
}

export interface UpdateDocumentRequest {
  title: string;
  description: string;
  content: string;
  creativeSpaceId?: number;
}
