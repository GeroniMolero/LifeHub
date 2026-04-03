import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreativeSpace,
  CreateCreativeSpaceRequest,
  UpdateCreativeSpaceRequest,
  ShareCreativeSpaceRequest,
  SpacePermission
} from '../models/creative-space.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class CreativeSpaceService {
  private apiUrl = `${API_BASE_URL}/creativespaces`;
  private readonly favoriteStorageKeyPrefix = 'lifehub:favorites:';

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

  getFavoriteSpaceIds(userId: string): number[] {
    if (!userId) return [];

    const raw = localStorage.getItem(`${this.favoriteStorageKeyPrefix}${userId}`);
    if (!raw) return [];

    try {
      const parsed = JSON.parse(raw) as number[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  isFavoriteSpace(userId: string, spaceId: number): boolean {
    return this.getFavoriteSpaceIds(userId).includes(spaceId);
  }

  toggleFavoriteSpace(userId: string, spaceId: number): number[] {
    const current = new Set<number>(this.getFavoriteSpaceIds(userId));
    if (current.has(spaceId)) {
      current.delete(spaceId);
    } else {
      current.add(spaceId);
    }

    const next = Array.from(current.values());
    this.saveFavoriteSpaceIds(userId, next);
    return next;
  }

  private saveFavoriteSpaceIds(userId: string, favoriteIds: number[]): void {
    if (!userId) return;
    localStorage.setItem(`${this.favoriteStorageKeyPrefix}${userId}`, JSON.stringify(favoriteIds));
  }
}
