export interface CreativeSpace {
  id: number;
  ownerId: string;
  name: string;
  description: string;
  privacy: SpacePrivacy;
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
  isPublicProfileVisible: boolean;
}

export interface UpdateCreativeSpaceRequest {
  name: string;
  description: string;
  privacy: SpacePrivacy;
  isPublicProfileVisible: boolean;
}
