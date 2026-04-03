import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { Friendship, FriendshipStatus } from '../models/friendship.model';

@Injectable({
  providedIn: 'root'
})
export class FriendshipService {
  private apiUrl = `${API_BASE_URL}/friendships`;

  constructor(private http: HttpClient) {}

  getAcceptedFriendships(): Observable<Friendship[]> {
    return this.http.get<Friendship[]>(`${this.apiUrl}/accepted`);
  }

  getFriendships(): Observable<Friendship[]> {
    return this.http.get<Friendship[]>(this.apiUrl);
  }

  sendFriendRequest(receiverId: string): Observable<Friendship> {
    return this.http.post<Friendship>(this.apiUrl, { receiverId });
  }

  updateFriendshipStatus(id: number, status: FriendshipStatus): Observable<Friendship> {
    return this.http.put<Friendship>(`${this.apiUrl}/${id}`, { status });
  }

  acceptFriendRequest(id: number): Observable<Friendship> {
    return this.updateFriendshipStatus(id, FriendshipStatus.Accepted);
  }

  deleteFriendship(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
