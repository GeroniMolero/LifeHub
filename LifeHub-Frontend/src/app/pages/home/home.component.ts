import { Component, DestroyRef, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User } from '../../models/auth.model';
import { CreativeSpace } from '../../models/creative-space.model';
import { Friendship, FriendshipStatus } from '../../models/friendship.model';
import { CreativeSpaceService } from '../../services/creative-space.service';
import { ConfirmationService } from '../../services/confirmation.service';
import { FriendshipService } from '../../services/friendship.service';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { LayoutHeaderStateService } from '../../services/layout-header-state.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);

  currentUserId = '';
  spaces: CreativeSpace[] = [];
  favoriteIds = new Set<number>();
  friendships: Friendship[] = [];
  searchQuery = '';
  searchResults: User[] = [];

  loadingSpaces = false;
  loadingFriends = false;
  searchingUsers = false;

  spacesError = '';
  friendsError = '';
  searchError = '';

  constructor(
    private creativeSpaceService: CreativeSpaceService,
    private confirmationService: ConfirmationService,
    private friendshipService: FriendshipService,
    private userService: UserService,
    private authService: AuthService,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.setHeaderState();

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => {
        this.currentUserId = user?.id || '';
        this.favoriteIds = new Set(this.creativeSpaceService.getFavoriteSpaceIds(this.currentUserId));
        this.setHeaderState();
      });

    this.loadSpaces();
    this.loadFriendships();
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  get favoriteSpaces(): CreativeSpace[] {
    return this.spaces.filter(space => this.favoriteIds.has(space.id));
  }

  get acceptedFriends(): Friendship[] {
    return this.friendships.filter(item => item.status === FriendshipStatus.Accepted);
  }

  get pendingIncomingRequests(): Friendship[] {
    if (!this.currentUserId) return [];
    return this.friendships.filter(
      item => item.status === FriendshipStatus.Pending && item.receiverId === this.currentUserId
    );
  }

  get totalSpacesCount(): number {
    return this.spaces.length;
  }

  get favoriteSpacesCount(): number {
    return this.favoriteSpaces.length;
  }

  get acceptedFriendsCount(): number {
    return this.acceptedFriends.length;
  }

  get pendingRequestsCount(): number {
    return this.pendingIncomingRequests.length;
  }

  onSearchInput(value: string): void {
    this.searchQuery = value;
    const term = value.trim();

    if (term.length < 2) {
      this.searchResults = [];
      this.searchError = '';
      return;
    }

    this.searchUsers(term);
  }

  sendFriendRequest(user: User): void {
    this.friendshipService.sendFriendRequest(user.id).subscribe({
      next: friendship => {
        this.friendships = [friendship, ...this.friendships.filter(f => f.id !== friendship.id)];
      },
      error: err => {
        this.searchError = err?.error?.message || 'No se pudo enviar la solicitud de amistad.';
      }
    });
  }

  acceptFriendRequest(friendshipId: number): void {
    this.friendshipService.acceptFriendRequest(friendshipId).subscribe({
      next: updated => {
        this.friendships = this.friendships.map(item => (item.id === updated.id ? updated : item));
      },
      error: err => {
        this.friendsError = err?.error?.message || 'No se pudo aceptar la solicitud de amistad.';
      }
    });
  }

  removeFriend(friendshipId: number): void {
    if (!this.confirmationService.confirmDelete('esta amistad')) return;

    this.friendshipService.deleteFriendship(friendshipId).subscribe({
      next: () => {
        this.friendships = this.friendships.filter(item => item.id !== friendshipId);
      },
      error: err => {
        this.friendsError = err?.error?.message || 'No se pudo eliminar la amistad.';
      }
    });
  }

  friendshipState(userId: string): 'none' | 'pending' | 'accepted' {
    const relation = this.friendshipWithUser(userId);
    if (!relation) return 'none';
    if (relation.status === FriendshipStatus.Accepted) return 'accepted';
    if (relation.status === FriendshipStatus.Pending) return 'pending';
    return 'none';
  }

  canAcceptRequestFrom(userId: string): boolean {
    if (!this.currentUserId) return false;
    const relation = this.friendshipWithUser(userId);
    return !!relation
      && relation.status === FriendshipStatus.Pending
      && relation.receiverId === this.currentUserId
      && relation.requesterId === userId;
  }

  acceptRequestFromUser(user: User): void {
    const relation = this.friendshipWithUser(user.id);
    if (!relation) return;
    this.acceptFriendRequest(relation.id);
  }

  friendDisplayName(friendship: Friendship): string {
    const user = this.friendFromFriendship(friendship);
    return user?.fullName || user?.email || 'Usuario';
  }

  friendEmail(friendship: Friendship): string {
    const user = this.friendFromFriendship(friendship);
    return user?.email || 'Sin email';
  }

  friendProfileLink(friendship: Friendship): string[] {
    const user = this.friendFromFriendship(friendship);
    return user?.id ? ['/users', user.id] : ['/users'];
  }

  userProfileLink(user: User): string[] {
    return ['/users', user.id];
  }

  private searchUsers(term: string): void {
    this.searchingUsers = true;
    this.searchError = '';

    this.userService.searchUsers(term).subscribe({
      next: users => {
        this.searchResults = users;
        this.searchingUsers = false;
      },
      error: err => {
        this.searchError = err?.error?.message || 'No se pudieron buscar usuarios.';
        this.searchingUsers = false;
      }
    });
  }

  private loadSpaces(): void {
    this.loadingSpaces = true;
    this.spacesError = '';

    this.creativeSpaceService.getSpaces().subscribe({
      next: spaces => {
        this.spaces = spaces;
        this.loadingSpaces = false;
        this.setHeaderState();
      },
      error: () => {
        this.spacesError = 'No se pudieron cargar los espacios.';
        this.loadingSpaces = false;
        this.setHeaderState();
      }
    });
  }

  private loadFriendships(): void {
    this.loadingFriends = true;
    this.friendsError = '';

    this.friendshipService.getFriendships().subscribe({
      next: friendships => {
        this.friendships = friendships;
        this.loadingFriends = false;
        this.setHeaderState();
      },
      error: err => {
        this.friendsError = err?.error?.message || 'No se pudieron cargar las amistades.';
        this.loadingFriends = false;
        this.setHeaderState();
      }
    });
  }

  private setHeaderState(): void {
    const pendingRequestsMeta = this.pendingRequestsCount > 0
      ? [`${this.pendingRequestsCount} solicitudes pendientes`]
      : [];

    this.layoutHeaderStateService.setOverride({
      title: 'Inicio',
      description: 'Resumen rápido de tu actividad en LifeHub',
      meta: pendingRequestsMeta,
      actions: []
    });
  }

  private friendFromFriendship(friendship: Friendship): User | undefined {
    if (!this.currentUserId) return friendship.receiver || friendship.requester;
    return friendship.requesterId === this.currentUserId ? friendship.receiver : friendship.requester;
  }

  private friendshipWithUser(userId: string): Friendship | undefined {
    return this.friendships.find(item => item.requesterId === userId || item.receiverId === userId);
  }

}
