import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreativeSpace,
  CreateCreativeSpaceRequest,
  UpdateCreativeSpaceRequest,
  ShareCreativeSpaceRequest,
  SpacePermission,
  CreateSpaceMediaReferenceRequest
} from '../models/creative-space.model';
import { API_BASE_URL } from '../config/api.config';
import { SpaceMediaReference } from '../models/space-media-reference.model';

@Injectable({
  providedIn: 'root'
})
export class CreativeSpaceService {
  private apiUrl = `${API_BASE_URL}/creativespaces`;

  constructor(private http: HttpClient) {}

  getSpaces(): Observable<CreativeSpace[]> {
    return this.http.get<CreativeSpace[]>(this.apiUrl);
  }

  getSpace(id: number): Observable<CreativeSpace> {
    return this.http.get<CreativeSpace>(`${this.apiUrl}/${id}`);
  }

  createSpace(data: CreateCreativeSpaceRequest): Observable<CreativeSpace> {
    return this.http.post<CreativeSpace>(this.apiUrl, data);
  }

  updateSpace(id: number, data: UpdateCreativeSpaceRequest): Observable<CreativeSpace> {
    return this.http.put<CreativeSpace>(`${this.apiUrl}/${id}`, data);
  }

  deleteSpace(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getPermissions(spaceId: number): Observable<SpacePermission[]> {
    return this.http.get<SpacePermission[]>(`${this.apiUrl}/${spaceId}/permissions`);
  }

  shareSpace(spaceId: number, data: ShareCreativeSpaceRequest): Observable<SpacePermission> {
    return this.http.post<SpacePermission>(`${this.apiUrl}/${spaceId}/permissions`, data);
  }

  removePermission(spaceId: number, targetUserId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${spaceId}/permissions/${targetUserId}`);
  }

  getMediaReferences(spaceId: number): Observable<SpaceMediaReference[]> {
    return this.http.get<SpaceMediaReference[]>(`${this.apiUrl}/${spaceId}/media-references`);
  }

  addMediaReference(spaceId: number, data: CreateSpaceMediaReferenceRequest): Observable<SpaceMediaReference> {
    return this.http.post<SpaceMediaReference>(`${this.apiUrl}/${spaceId}/media-references`, data);
  }

  removeMediaReference(spaceId: number, referenceId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${spaceId}/media-references/${referenceId}`);
  }

  addFavoriteSpace(spaceId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${spaceId}/favorite`, {});
  }

  removeFavoriteSpace(spaceId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${spaceId}/favorite`);
  }
}
