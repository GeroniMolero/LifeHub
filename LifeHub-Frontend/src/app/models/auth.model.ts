export interface User {
  id: string;
  email?: string;
  fullName?: string;
  profilePictureUrl?: string;
  bio?: string;
  roles?: string[];
  claims?: string[];
  isActive?: boolean;
  createdAt?: string;
}

export interface AuthResponse {
  success: boolean;
  message?: string;
  token?: string;
  user?: User;
}

export interface UserUsage {
  documentsCount: number;
  spacesCount: number;
  publishedDocumentsCount: number;
  profileVisibleDocumentsCount: number;
  profileVisibleSpacesCount: number;
  maxDocuments: number;
  maxSpaces: number;
  maxPublishedDocuments: number;
  maxProfileVisibleDocuments: number;
  maxProfileVisibleSpaces: number;
}

export interface AdminUser {
  id: string;
  email: string;
  fullName?: string;
  profilePictureUrl?: string;
  bio?: string;
  isActive: boolean;
  createdAt: string;
  roles: string[];
  claims: string[];
  usage: UserUsage;
}

export interface ActivityLogEntry {
  id: number;
  userId: string | null;
  userEmail: string | null;
  userFullName: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string;
  createdAt: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
