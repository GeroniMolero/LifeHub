import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DocumentVersion, CreateDocumentVersionRequest } from '../models/document-version.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class DocumentVersionService {
  private apiUrl = `${API_BASE_URL}/documentversions`;

  constructor(private http: HttpClient) {}

  getDocumentVersions(documentId: number): Observable<DocumentVersion[]> {
    return this.http.get<DocumentVersion[]>(`${this.apiUrl}/document/${documentId}`);
  }

  createSnapshot(documentId: number, data: CreateDocumentVersionRequest): Observable<DocumentVersion> {
    return this.http.post<DocumentVersion>(`${this.apiUrl}/document/${documentId}/snapshot`, data);
  }

  restoreVersion(versionId: number): Observable<{ message: string; documentId: number; restoredVersion: number }> {
    return this.http.post<{ message: string; documentId: number; restoredVersion: number }>(`${this.apiUrl}/${versionId}/restore`, {});
  }
}
