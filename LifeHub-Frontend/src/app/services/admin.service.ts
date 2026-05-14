import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminUser, ActivityLogEntry, PaginatedResult } from '../models/auth.model';
import { API_BASE_URL } from '../config/api.config';

export interface ActivityLogQuery {
  userId?: string;
  userEmail?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private api = `${API_BASE_URL}/admin`;

  constructor(private http: HttpClient) {}

  getAdminUsers(page: number = 1, pageSize: number = 20): Observable<PaginatedResult<AdminUser>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PaginatedResult<AdminUser>>(`${this.api}/users`, { params });
  }

  toggleUserActive(id: string): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.api}/users/${id}/toggle-active`, {});
  }

  adminUpdateUser(id: string, data: { email: string; fullName?: string }): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.api}/users/${id}`, data);
  }

  adminSetPassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.api}/users/${id}/set-password`, { newPassword });
  }

  adminUpdateRole(id: string, role: string): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.api}/users/${id}/roles`, { role });
  }

  getActivityLogs(query: ActivityLogQuery): Observable<PaginatedResult<ActivityLogEntry>> {
    let params = new HttpParams();
    if (query.userId)     params = params.set('userId', query.userId);
    if (query.userEmail)  params = params.set('userEmail', query.userEmail);
    if (query.action)     params = params.set('action', query.action);
    if (query.entityType) params = params.set('entityType', query.entityType);
    if (query.from)       params = params.set('from', query.from);
    if (query.to)         params = params.set('to', query.to);
    if (query.page)       params = params.set('page', query.page.toString());
    if (query.pageSize)   params = params.set('pageSize', query.pageSize.toString());
    return this.http.get<PaginatedResult<ActivityLogEntry>>(`${this.api}/activity-logs`, { params });
  }

  triggerBackup(): Observable<{ message: string; backupFile?: string }> {
    return this.http.post<{ message: string; backupFile?: string }>(`${this.api}/backup`, {});
  }
}
