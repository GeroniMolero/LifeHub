export type SpaceMediaReferenceType = 'external-embed' | 'local-session-file';

export interface SpaceMediaReference {
  id: string;
  type: SpaceMediaReferenceType;
  label: string;
  source: string;
  provider?: string;
  embedUrl?: string;
  mimeType?: string;
  createdAt: string;
}
