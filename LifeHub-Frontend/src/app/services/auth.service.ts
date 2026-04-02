import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResponse, User } from '../models/auth.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${API_BASE_URL}/auth`;
  private tokenKey = 'lifehub_token';
  private userSubject = new BehaviorSubject<User | null>(null);

  constructor(private http: HttpClient) {
    this.loadUser();
  }

  register(email: string, fullName: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, {
      email,
      fullName,
      password,
      confirmPassword: password
    });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        tap(response => {
          if (response.success && response.token && response.user) {
            localStorage.setItem(this.tokenKey, response.token);
            localStorage.setItem('lifehub_user', JSON.stringify(response.user));
            this.userSubject.next(response.user);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem('lifehub_user');
    this.userSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): Observable<User | null> {
    return this.userSubject.asObservable();
  }

  refreshCurrentUser(): Observable<User> {
    return this.http.get<User>(`${API_BASE_URL}/users/me`).pipe(
      tap(user => {
        localStorage.setItem('lifehub_user', JSON.stringify(user));
        this.userSubject.next(user);
      })
    );
  }

  hasRole(role: string): boolean {
    const user = this.userSubject.value;
    if (!user?.roles) {
      return false;
    }

    return user.roles.some(r => r.toLowerCase() === role.toLowerCase());
  }

  hasClaim(type: string, value?: string): boolean {
    const user = this.userSubject.value;
    if (!user?.claims?.length) {
      return false;
    }

    const expected = value ? `${type}:${value}` : `${type}:`;
    return user.claims.some(c => (value ? c === expected : c.startsWith(expected)));
  }

  canViewAdmin(): boolean {
    return this.hasRole('Admin') || this.hasClaim('permission', 'admin.users.view');
  }

  private loadUser(): void {
    if (this.isAuthenticated()) {
      const storedUser = localStorage.getItem('lifehub_user');
      if (storedUser) {
        try {
          this.userSubject.next(JSON.parse(storedUser));
        } catch {
          // Si hay error al parsear, ignorar
        }
      }
    }
  }
}
