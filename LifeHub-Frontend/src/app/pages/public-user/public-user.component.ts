import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { User } from '../../models/auth.model';
import { Friendship, FriendshipStatus } from '../../models/friendship.model';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { FriendshipService } from '../../services/friendship.service';
import { ConfirmationService } from '../../services/confirmation.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-public-user',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-user.component.html',
  styleUrls: ['./public-user.component.scss']
})
export class PublicUserComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  user: User | null = null;
  loading = false;
  error = '';

  currentUserId = '';
  friendship: Friendship | null = null;
  loadingFriendship = false;

  constructor(
    private route: ActivatedRoute,
    private userService: UserService,
    private authService: AuthService,
    private friendshipService: FriendshipService,
    private confirmationService: ConfirmationService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(u => { this.currentUserId = u?.id ?? ''; });

    const userId = this.route.snapshot.paramMap.get('id');
    if (!userId) {
      this.error = 'Usuario no válido.';
      return;
    }

    this.loadUser(userId);
    this.loadFriendship(userId);
  }

  get friendshipState(): 'none' | 'pending-sent' | 'pending-received' | 'accepted' {
    if (!this.friendship) return 'none';
    if (this.friendship.status === FriendshipStatus.Accepted) return 'accepted';
    if (this.friendship.status === FriendshipStatus.Pending) {
      return this.friendship.requesterId === this.currentUserId ? 'pending-sent' : 'pending-received';
    }
    return 'none';
  }

  sendFriendRequest(): void {
    if (!this.user) return;
    this.friendshipService.sendFriendRequest(this.user.id).subscribe({
      next: f => { this.friendship = f; },
      error: () => this.toast.error('No se pudo enviar la solicitud.')
    });
  }

  acceptFriendRequest(): void {
    if (!this.friendship) return;
    this.friendshipService.acceptFriendRequest(this.friendship.id).subscribe({
      next: f => { this.friendship = f; },
      error: () => this.toast.error('No se pudo aceptar la solicitud.')
    });
  }

  removeFriend(): void {
    if (!this.friendship) return;
    if (!this.confirmationService.confirmDelete('esta amistad')) return;
    this.friendshipService.deleteFriendship(this.friendship.id).subscribe({
      next: () => { this.friendship = null; this.toast.success('Amistad eliminada.'); },
      error: () => this.toast.error('No se pudo eliminar la amistad.')
    });
  }

  private loadUser(userId: string): void {
    this.loading = true;
    this.error = '';
    this.userService.getUser(userId).subscribe({
      next: user => { this.user = user; this.loading = false; },
      error: err => {
        this.error = err?.error?.message || 'No se pudo cargar el perfil del usuario.';
        this.loading = false;
      }
    });
  }

  private loadFriendship(userId: string): void {
    this.loadingFriendship = true;
    this.friendshipService.getFriendships().subscribe({
      next: friendships => {
        this.friendship = friendships.find(
          f => f.requesterId === userId || f.receiverId === userId
        ) ?? null;
        this.loadingFriendship = false;
      },
      error: () => { this.loadingFriendship = false; }
    });
  }
}
