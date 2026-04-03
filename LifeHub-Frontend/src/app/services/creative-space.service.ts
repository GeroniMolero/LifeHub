import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreativeSpace, CreateCreativeSpaceRequest, UpdateCreativeSpaceRequest } from '../models/creative-space.model';
import { API_BASE_URL } from '../config/api.config';

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
}
