import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Document, CreateDocumentRequest, DocumentType, UpdateDocumentRequest } from '../models/document.model';
import { PaginatedResult } from '../models/auth.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = `${API_BASE_URL}/documents`;

  constructor(private http: HttpClient) {}

  getDocuments(spaceId?: number): Observable<Document[]> {
    let params = new HttpParams().set('pageSize', '1000');
    if (spaceId != null) params = params.set('spaceId', spaceId.toString());
    return this.http.get<PaginatedResult<Document>>(this.apiUrl, { params }).pipe(map(r => r.items));
  }

  getDocumentsPage(page: number, pageSize: number, spaceId?: number): Observable<PaginatedResult<Document>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (spaceId != null) params = params.set('spaceId', spaceId.toString());
    return this.http.get<PaginatedResult<Document>>(this.apiUrl, { params });
  }

  copyToSpace(documentId: number, targetSpaceId: number): Observable<Document> {
    return this.http.post<Document>(`${this.apiUrl}/${documentId}/copy`, { targetSpaceId });
  }

  getDocument(id: number): Observable<Document> {
    return this.http.get<Document>(`${this.apiUrl}/${id}`);
  }

  createDocument(data: CreateDocumentRequest): Observable<Document> {
    return this.http.post<Document>(this.apiUrl, data);
  }

  updateDocument(id: number, data: UpdateDocumentRequest): Observable<Document> {
    return this.http.put<Document>(`${this.apiUrl}/${id}`, data);
  }

  deleteDocument(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getPublicDocumentsByUser(userId: string): Observable<Document[]> {
    return this.http.get<Document[]>(`${this.apiUrl}/public/${userId}`);
  }

  setDocumentProfileVisibility(documentId: number, isVisible: boolean): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${documentId}/publication/profile-visibility`, isVisible);
  }

  static getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }
}
