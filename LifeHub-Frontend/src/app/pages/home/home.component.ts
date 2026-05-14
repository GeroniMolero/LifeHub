import { Component, DestroyRef, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User, UserUsage } from '../../models/auth.model';
import { CreativeSpace } from '../../models/creative-space.model';
import { Document } from '../../models/document.model';
import { Friendship, FriendshipStatus } from '../../models/friendship.model';
import { CreativeSpaceService } from '../../services/creative-space.service';
import { DocumentService } from '../../services/document.service';
import { FriendshipService } from '../../services/friendship.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
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
  friendships: Friendship[] = [];
  recentDocuments: Document[] = [];
  usage: UserUsage | null = null;

  loadingSpaces = false;
  loadingFriends = false;
  loadingDocuments = false;

  spacesError = '';
  friendsError = '';

  constructor(
    private creativeSpaceService: CreativeSpaceService,
    private documentService: DocumentService,
    private friendshipService: FriendshipService,
    private authService: AuthService,
    private userService: UserService,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.setHeaderState();

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => {
        this.currentUserId = user?.id || '';
        this.setHeaderState();
      });

    this.loadSpaces();
    this.loadFriendships();
    this.loadUsage();
    this.loadRecentDocuments();
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  get favoriteSpaces(): CreativeSpace[] {
    return this.spaces.filter(space => space.isFavorite);
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

  acceptFriendRequest(friendshipId: number): void {
    this.friendshipService.acceptFriendRequest(friendshipId).subscribe({
      next: updated => {
        this.friendships = this.friendships.map(item => (item.id === updated.id ? updated : item));
        this.setHeaderState();
      },
      error: err => {
        this.friendsError = err?.error?.message || 'No se pudo aceptar la solicitud de amistad.';
      }
    });
  }

  friendDisplayName(friendship: Friendship): string {
    const user = this.friendFromFriendship(friendship);
    return user?.fullName || user?.email || 'Usuario';
  }

  friendEmail(friendship: Friendship): string {
    const user = this.friendFromFriendship(friendship);
    return user?.email || '';
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

  usagePercent(count: number, max: number): number {
    if (max <= 0) return 0;
    return Math.min(100, Math.round((count / max) * 100));
  }

  formatUpdatedAt(date: Date): string {
    const d = new Date(date);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffDays === 0) return 'Hoy';
    if (diffDays === 1) return 'Ayer';
    if (diffDays < 7) return `Hace ${diffDays} días`;
    return d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
  }

  private loadRecentDocuments(): void {
    this.loadingDocuments = true;
    this.documentService.getDocumentsPage(1, 5)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.recentDocuments = result.items;
          this.loadingDocuments = false;
        },
        error: () => this.loadingDocuments = false
      });
  }

  private loadUsage(): void {
    this.userService.getUsage()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: usage => this.usage = usage });
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
}
