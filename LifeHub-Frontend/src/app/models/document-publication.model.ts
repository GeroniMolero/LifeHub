export interface MediaReference {
  type: string;
  label: string;
  source: string;
  provider?: string;
  embedUrl?: string;
}

export interface DocumentPublication {
  documentId: number;
  isPublic: boolean;
  publishedAt?: string;
  publicTitle?: string;
  publicDescription?: string;
  mediaReferences: MediaReference[];
  externalLinks: string[];
  createdAt?: string;
  updatedAt?: string;
}

export interface UpsertDocumentPublicationRequest {
  isPublic: boolean;
  publicTitle?: string;
  publicDescription?: string;
  mediaReferences: MediaReference[];
  externalLinks: string[];
}

export interface PublicDocumentView {
  documentId: number;
  title: string;
  description: string;
  content: string;
  publishedAt?: string;
  mediaReferences: MediaReference[];
  externalLinks: string[];
}
