import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Document, CreateDocumentRequest, DocumentType, UpdateDocumentRequest } from '../models/document.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = `${API_BASE_URL}/documents`;

  constructor(private http: HttpClient) {}

  getDocuments(spaceId?: number): Observable<Document[]> {
    const params = spaceId != null ? `?spaceId=${spaceId}` : '';
    return this.http.get<Document[]>(`${this.apiUrl}${params}`);
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
