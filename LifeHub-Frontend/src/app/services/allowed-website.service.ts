import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { AllowedWebsite, CreateAllowedWebsiteRequest } from '../models/allowed-website.model';

@Injectable({
  providedIn: 'root'
})
export class AllowedWebsiteService {
  private adminApiUrl = `${API_BASE_URL}/admin/allowed-websites`;
  private allowlistApiUrl = `${API_BASE_URL}/embed-allowlist`;

  constructor(private http: HttpClient) {}

  getAllowedWebsites(): Observable<AllowedWebsite[]> {
    return this.http.get<AllowedWebsite[]>(this.adminApiUrl);
  }

  createAllowedWebsite(data: CreateAllowedWebsiteRequest): Observable<AllowedWebsite> {
    return this.http.post<AllowedWebsite>(this.adminApiUrl, data);
  }

  updateAllowedWebsite(id: number, isActive: boolean): Observable<AllowedWebsite> {
    return this.http.put<AllowedWebsite>(`${this.adminApiUrl}/${id}`, { isActive });
  }

  deleteAllowedWebsite(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminApiUrl}/${id}`);
  }

  getEmbedAllowlist(): Observable<string[]> {
    return this.http.get<string[]>(this.allowlistApiUrl);
  }
}
