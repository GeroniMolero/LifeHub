export interface AllowedWebsite {
  id: number;
  domain: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAllowedWebsiteRequest {
  domain: string;
  isActive: boolean;
}
