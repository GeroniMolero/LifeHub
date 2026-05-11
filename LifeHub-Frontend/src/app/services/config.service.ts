import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, Observable, of, tap } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';

interface AppLimits {
  maxDocumentsPerUser: number;
  maxSpacesPerUser: number;
  maxPublishedDocumentsPerUser: number;
  maxProfileVisibleDocumentsPerUser: number;
  maxProfileVisibleSpacesPerUser: number;
}

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private readonly apiUrl = `${API_BASE_URL}/config`;

  private limits: AppLimits = {
    maxDocumentsPerUser: 20,
    maxSpacesPerUser: 10,
    maxPublishedDocumentsPerUser: 10,
    maxProfileVisibleDocumentsPerUser: 3,
    maxProfileVisibleSpacesPerUser: 3
  };

  constructor(private http: HttpClient) {}

  loadLimits(): Observable<AppLimits> {
    return this.http.get<AppLimits>(`${this.apiUrl}/limits`).pipe(
      tap(l => this.limits = l),
      catchError(() => of(this.limits))
    );
  }

  get maxDocuments(): number     { return this.limits.maxDocumentsPerUser; }
  get maxSpaces(): number        { return this.limits.maxSpacesPerUser; }
  get maxPublishedDocs(): number { return this.limits.maxPublishedDocumentsPerUser; }
  get maxProfileDocs(): number   { return this.limits.maxProfileVisibleDocumentsPerUser; }
  get maxProfileSpaces(): number { return this.limits.maxProfileVisibleSpacesPerUser; }
}
