import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { DocumentPublication, PublicDocumentView, UpsertDocumentPublicationRequest } from '../models/document-publication.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentPublicationService {
  private apiUrl = `${API_BASE_URL}/documents`;
  private publicApiUrl = `${API_BASE_URL}/public/documents`;

  constructor(private http: HttpClient) {}

  getPublication(documentId: number): Observable<DocumentPublication> {
    return this.http.get<DocumentPublication>(`${this.apiUrl}/${documentId}/publication`);
  }

  upsertPublication(documentId: number, payload: UpsertDocumentPublicationRequest): Observable<DocumentPublication> {
    return this.http.put<DocumentPublication>(`${this.apiUrl}/${documentId}/publication`, payload);
  }

  getPublicDocument(documentId: number): Observable<PublicDocumentView> {
    return this.http.get<PublicDocumentView>(`${this.publicApiUrl}/${documentId}`);
  }
}
