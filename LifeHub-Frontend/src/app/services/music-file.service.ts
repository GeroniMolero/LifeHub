import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { CreateMusicFileRequest, MusicFile, UpdateMusicFileRequest } from '../models/music-file.model';

@Injectable({
  providedIn: 'root'
})
export class MusicFileService {
  private apiUrl = `${API_BASE_URL}/musicfiles`;

  constructor(private http: HttpClient) {}

  getMusicFiles(): Observable<MusicFile[]> {
    return this.http.get<MusicFile[]>(this.apiUrl);
  }

  createMusicFile(data: CreateMusicFileRequest): Observable<MusicFile> {
    return this.http.post<MusicFile>(this.apiUrl, data);
  }

  updateMusicFile(id: number, data: UpdateMusicFileRequest): Observable<MusicFile> {
    return this.http.put<MusicFile>(`${this.apiUrl}/${id}`, data);
  }

  deleteMusicFile(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
