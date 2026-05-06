import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { API_BASE_URL, HUB_BASE_URL } from '../config/api.config';
import { AuthService } from './auth.service';
import { MessageDto } from '../models/message.model';

@Injectable({ providedIn: 'root' })
export class ChatService implements OnDestroy {
  private hub: signalR.HubConnection | null = null;
  private messageReceivedSubject = new Subject<MessageDto>();
  private messageSentSubject = new Subject<number>();
  private unreadCountSubject = new BehaviorSubject<number>(0);

  messageReceived$ = this.messageReceivedSubject.asObservable();
  messageSent$ = this.messageSentSubject.asObservable();
  unreadCount$ = this.unreadCountSubject.asObservable();

  constructor(private http: HttpClient, private authService: AuthService) {
    this.authService.getCurrentUser().subscribe(user => {
      if (user) this.connect();
      else this.disconnect();
    });
  }

  private connect(): void {
    if (this.hub) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${HUB_BASE_URL}/hubs/chat`, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.hub.on('ReceiveMessage', (msg: MessageDto) => {
      this.messageReceivedSubject.next(msg);
    });

    this.hub.on('MessageSent', (id: number) => {
      this.messageSentSubject.next(id);
    });

    this.hub.start()
      .then(() => this.refreshUnreadCount())
      .catch(err => console.error('SignalR:', err));
  }

  private disconnect(): void {
    this.hub?.stop();
    this.hub = null;
    this.unreadCountSubject.next(0);
  }

  sendMessage(receiverId: string, content: string): Promise<void> {
    if (!this.hub) return Promise.reject('Hub no conectado');
    return this.hub.invoke('SendMessageAsync', receiverId, content);
  }

  markAsRead(messageId: number): void {
    this.hub?.invoke('MarkMessageAsReadAsync', messageId).catch(() => {});
  }

  getConversation(userId: string): Observable<MessageDto[]> {
    return this.http.get<MessageDto[]>(`${API_BASE_URL}/messages/conversation/${userId}`);
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${API_BASE_URL}/messages/unread`);
  }

  getUnreadPerSender(): Observable<Record<string, number>> {
    return this.http.get<Record<string, number>>(`${API_BASE_URL}/messages/unread-per-sender`);
  }

  refreshUnreadCount(): void {
    this.getUnreadCount().subscribe({
      next: count => this.unreadCountSubject.next(count),
      error: () => {}
    });
  }

  incrementUnread(): void {
    this.unreadCountSubject.next(this.unreadCountSubject.value + 1);
  }

  decrementUnread(n: number): void {
    this.unreadCountSubject.next(Math.max(0, this.unreadCountSubject.value - n));
  }

  ngOnDestroy(): void {
    this.hub?.stop();
  }
}
