export interface CreativeSpace {
  id: number;
  ownerId: string;
  name: string;
  description: string;
  privacy: SpacePrivacy;
  isFavorite: boolean;
  isPublicProfileVisible: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export enum SpacePrivacy {
  Private = 0,
  Shared = 1
}

export interface CreateCreativeSpaceRequest {
  name: string;
  description: string;
  privacy: SpacePrivacy;
  isFavorite: boolean;
  isPublicProfileVisible: boolean;
}

export interface UpdateCreativeSpaceRequest {
  name: string;
  description: string;
  privacy: SpacePrivacy;
  isFavorite: boolean;
  isPublicProfileVisible: boolean;
}

export enum SpacePermissionLevel {
  Viewer = 0,
  Editor = 1
}

export interface SpacePermission {
  id: number;
  creativeSpaceId: number;
  userId: string;
  userName?: string;
  userEmail?: string;
  permissionLevel: SpacePermissionLevel;
  grantedByUserId: string;
  createdAt: Date;
}

export interface ShareCreativeSpaceRequest {
  userId: string;
  permissionLevel: SpacePermissionLevel;
}

export interface CreateSpaceMediaReferenceRequest {
  label: string;
  source: string;
  provider?: string;
  embedUrl: string;
}
