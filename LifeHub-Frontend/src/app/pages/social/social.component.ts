import { Component, DestroyRef, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User } from '../../models/auth.model';
import { Friendship, FriendshipStatus } from '../../models/friendship.model';
import { MessageDto } from '../../models/message.model';
import { AuthService } from '../../services/auth.service';
import { FriendshipService } from '../../services/friendship.service';
import { UserService } from '../../services/user.service';
import { ConfirmationService } from '../../services/confirmation.service';
import { ChatService } from '../../services/chat.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-social',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './social.component.html',
  styleUrls: ['./social.component.scss']
})
export class SocialComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild('messagesEnd') private messagesEnd!: ElementRef;

  // Chat state
  selectedFriend: User | null = null;
  messages: MessageDto[] = [];
  newMessage = '';
  currentUserId = '';
  loadingMessages = false;
  sendingMessage = false;

  // Friends state
  friendships: Friendship[] = [];
  loadingFriendships = false;

  // Search state
  searchQuery = '';
  searchResults: User[] = [];
  searchingUsers = false;

  constructor(
    private authService: AuthService,
    private friendshipService: FriendshipService,
    private userService: UserService,
    private confirmationService: ConfirmationService,
    private chatService: ChatService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => { this.currentUserId = user?.id ?? ''; });

    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(msg => {
        const isActiveConversation =
          msg.senderId === this.selectedFriend?.id ||
          msg.receiverId === this.selectedFriend?.id;

        if (isActiveConversation) {
          this.messages = [...this.messages, msg];
          this.scrollToBottom();
          if (msg.senderId === this.selectedFriend?.id) {
            this.chatService.markAsRead(msg.id);
          }
        } else {
          this.chatService.incrementUnread();
        }
      });

    this.chatService.messageSent$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(serverId => {
        this.messages = this.messages.map(m => m.id === -1 ? { ...m, id: serverId } : m);
        this.sendingMessage = false;
      });

    this.loadFriendships();
  }

  // ── Getters ────────────────────────────────────────────────────────────────

  get friends(): User[] {
    return this.friendships
      .filter(f => f.status === FriendshipStatus.Accepted)
      .map(f => f.requesterId === this.currentUserId ? f.receiver! : f.requester!)
      .filter(u => !!u);
  }

  get acceptedFriends(): Friendship[] {
    return this.friendships.filter(f => f.status === FriendshipStatus.Accepted);
  }

  get pendingIncomingRequests(): Friendship[] {
    if (!this.currentUserId) return [];
    return this.friendships.filter(
      f => f.status === FriendshipStatus.Pending && f.receiverId === this.currentUserId
    );
  }

  // ── Chat ──────────────────────────────────────────────────────────────────

  selectFriend(friend: User): void {
    this.selectedFriend = friend;
    this.messages = [];
    this.loadConversation(friend.id);
  }

  sendMessage(): void {
    const content = this.newMessage.trim();
    if (!content || this.sendingMessage || !this.selectedFriend) return;

    const optimistic: MessageDto = {
      id: -1,
      senderId: this.currentUserId,
      receiverId: this.selectedFriend.id,
      content,
      sentAt: new Date().toISOString(),
      isRead: false
    };

    this.messages = [...this.messages, optimistic];
    this.newMessage = '';
    this.sendingMessage = true;
    this.scrollToBottom();

    this.chatService.sendMessage(this.selectedFriend.id, content).catch(() => {
      this.messages = this.messages.filter(m => m.id !== -1);
      this.sendingMessage = false;
      this.toast.error('No se pudo enviar el mensaje.');
    });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  isSent(msg: MessageDto): boolean {
    return msg.senderId === this.currentUserId;
  }

  // ── Friends management ────────────────────────────────────────────────────

  onSearchInput(value: string): void {
    this.searchQuery = value;
    const term = value.trim();
    if (term.length < 2) {
      this.searchResults = [];
      return;
    }
    this.searchUsers(term);
  }

  sendFriendRequest(user: User): void {
    this.friendshipService.sendFriendRequest(user.id).subscribe({
      next: friendship => {
        this.friendships = [friendship, ...this.friendships.filter(f => f.id !== friendship.id)];
      },
      error: () => this.toast.error('No se pudo enviar la solicitud de amistad.')
    });
  }

  acceptFriendRequest(friendshipId: number): void {
    this.friendshipService.acceptFriendRequest(friendshipId).subscribe({
      next: updated => {
        this.friendships = this.friendships.map(f => f.id === updated.id ? updated : f);
      },
      error: () => this.toast.error('No se pudo aceptar la solicitud.')
    });
  }

  removeFriend(friendshipId: number): void {
    if (!this.confirmationService.confirmDelete('esta amistad')) return;

    this.friendshipService.deleteFriendship(friendshipId).subscribe({
      next: () => {
        this.friendships = this.friendships.filter(f => f.id !== friendshipId);
        if (this.selectedFriend) {
          const stillFriend = this.friends.some(u => u.id === this.selectedFriend!.id);
          if (!stillFriend) this.selectedFriend = null;
        }
      },
      error: () => this.toast.error('No se pudo eliminar la amistad.')
    });
  }

  friendshipState(userId: string): 'none' | 'pending' | 'accepted' {
    const rel = this.friendshipWithUser(userId);
    if (!rel) return 'none';
    if (rel.status === FriendshipStatus.Accepted) return 'accepted';
    if (rel.status === FriendshipStatus.Pending) return 'pending';
    return 'none';
  }

  canAcceptRequestFrom(userId: string): boolean {
    if (!this.currentUserId) return false;
    const rel = this.friendshipWithUser(userId);
    return !!rel
      && rel.status === FriendshipStatus.Pending
      && rel.receiverId === this.currentUserId
      && rel.requesterId === userId;
  }

  acceptRequestFromUser(user: User): void {
    const rel = this.friendshipWithUser(user.id);
    if (rel) this.acceptFriendRequest(rel.id);
  }

  toggleFriend(friendship: Friendship): void {
    const user = this.friendFromFriendship(friendship);
    if (!user) return;
    if (this.selectedFriend?.id === user.id) {
      this.selectedFriend = null;
      this.messages = [];
    } else {
      this.selectFriend(user);
    }
  }

  isFriendSelected(friendship: Friendship): boolean {
    if (!this.selectedFriend) return false;
    return this.friendFromFriendship(friendship)?.id === this.selectedFriend.id;
  }

  // ── Display helpers ───────────────────────────────────────────────────────

  initial(user: User): string {
    return (user.fullName || user.email || 'U').charAt(0).toUpperCase();
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
  }

  friendDisplayName(friendship: Friendship): string {
    const u = this.friendFromFriendship(friendship);
    return u?.fullName || u?.email || 'Usuario';
  }

  friendEmail(friendship: Friendship): string {
    const u = this.friendFromFriendship(friendship);
    return u?.email || '';
  }

  friendProfileLink(friendship: Friendship): string[] {
    const u = this.friendFromFriendship(friendship);
    return u?.id ? ['/users', u.id] : ['/users'];
  }

  friendPicture(friendship: Friendship): string | null {
    return this.friendFromFriendship(friendship)?.profilePictureUrl ?? null;
  }

  userProfileLink(user: User): string[] {
    return ['/users', user.id];
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private loadFriendships(): void {
    this.loadingFriendships = true;
    this.friendshipService.getFriendships().subscribe({
      next: friendships => {
        this.friendships = friendships;
        this.loadingFriendships = false;
      },
      error: () => {
        this.toast.error('No se pudieron cargar los contactos.');
        this.loadingFriendships = false;
      }
    });
  }

  private searchUsers(term: string): void {
    this.searchingUsers = true;
    this.userService.searchUsers(term).subscribe({
      next: users => {
        this.searchResults = users;
        this.searchingUsers = false;
      },
      error: () => {
        this.toast.error('No se pudieron buscar usuarios.');
        this.searchingUsers = false;
      }
    });
  }

  private loadConversation(userId: string): void {
    this.loadingMessages = true;
    this.chatService.getConversation(userId).subscribe({
      next: messages => {
        this.messages = messages;
        this.loadingMessages = false;
        this.scrollToBottom();
        const unread = messages.filter(m => !m.isRead && m.senderId === userId);
        unread.forEach(m => this.chatService.markAsRead(m.id));
        if (unread.length > 0) this.chatService.decrementUnread(unread.length);
      },
      error: () => {
        this.toast.error('No se pudo cargar la conversación.');
        this.loadingMessages = false;
      }
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }

  private friendFromFriendship(friendship: Friendship): User | undefined {
    if (!this.currentUserId) return friendship.receiver || friendship.requester;
    return friendship.requesterId === this.currentUserId ? friendship.receiver : friendship.requester;
  }

  private friendshipWithUser(userId: string): Friendship | undefined {
    return this.friendships.find(f => f.requesterId === userId || f.receiverId === userId);
  }
}
