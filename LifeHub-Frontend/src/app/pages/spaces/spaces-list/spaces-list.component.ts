import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  CreativeSpace,
  SpacePrivacy,
  SpacePermission,
  SpacePermissionLevel
} from '../../../models/creative-space.model';
import { CreativeSpaceService } from '../../../services/creative-space.service';
import { AuthService } from '../../../services/auth.service';
import { Friendship } from '../../../models/friendship.model';
import { FriendshipService } from '../../../services/friendship.service';
import { User } from '../../../models/auth.model';

@Component({
  selector: 'app-spaces-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './spaces-list.component.html',
  styleUrls: ['./spaces-list.component.scss']
})
export class SpacesListComponent implements OnInit {
  spaces: CreativeSpace[] = [];
  favoriteIds = new Set<number>();
  createForm!: FormGroup;
  editForm!: FormGroup;
  editingId: number | null = null;
  currentUserId = '';
  openPermissionsSpaceId: number | null = null;
  permissionForms: { [spaceId: number]: FormGroup } = {};
  permissionsBySpace: { [spaceId: number]: SpacePermission[] } = {};
  permissionLoadingBySpace: { [spaceId: number]: boolean } = {};
  permissionErrorBySpace: { [spaceId: number]: string } = {};
  acceptedFriendships: Friendship[] = [];
  friendsLoading = false;
  friendsError = '';
  loading = false;
  error = '';
  showCreate = false;
  SpacePrivacy = SpacePrivacy;
  SpacePermissionLevel = SpacePermissionLevel;

  constructor(
    private fb: FormBuilder,
    private creativeSpaceService: CreativeSpaceService,
    private authService: AuthService,
    private friendshipService: FriendshipService
  ) {}

  ngOnInit(): void {
    this.createForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      privacy: [SpacePrivacy.Private],
      isPublicProfileVisible: [false]
    });

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      privacy: [SpacePrivacy.Private],
      isPublicProfileVisible: [false]
    });

    this.authService.getCurrentUser().subscribe(user => {
      this.currentUserId = user?.id || '';
      this.favoriteIds = new Set(this.creativeSpaceService.getFavoriteSpaceIds(this.currentUserId));
    });

    this.loadAcceptedFriends();
    this.loadSpaces();
  }

  get friendOptions(): User[] {
    if (!this.currentUserId) return [];

    const map = new Map<string, User>();
    for (const friendship of this.acceptedFriendships) {
      const friend = friendship.requesterId === this.currentUserId
        ? friendship.receiver
        : friendship.requester;

      if (friend?.id && !map.has(friend.id)) {
        map.set(friend.id, friend);
      }
    }

    return Array.from(map.values()).sort((a, b) => {
      const nameA = (a.fullName || a.email || '').toLowerCase();
      const nameB = (b.fullName || b.email || '').toLowerCase();
      return nameA.localeCompare(nameB);
    });
  }

  toggleCreate(): void {
    this.showCreate = !this.showCreate;
  }

  createSpace(): void {
    if (this.createForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.createSpace(this.createForm.value).subscribe({
      next: (space) => {
        this.spaces = [space, ...this.spaces];
        this.showCreate = false;
        this.createForm.reset({
          name: '',
          description: '',
          privacy: SpacePrivacy.Private,
          isPublicProfileVisible: false
        });
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo crear el espacio.';
        this.loading = false;
      }
    });
  }

  startEdit(space: CreativeSpace): void {
    this.editingId = space.id;
    this.editForm.patchValue(space);
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(spaceId: number): void {
    if (this.editForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.updateSpace(spaceId, this.editForm.value).subscribe({
      next: (updated) => {
        this.spaces = this.spaces.map(space => space.id === spaceId ? updated : space);
        this.editingId = null;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo actualizar el espacio.';
        this.loading = false;
      }
    });
  }

  deleteSpace(spaceId: number): void {
    if (!confirm('¿Seguro que quieres eliminar este espacio creativo?')) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.deleteSpace(spaceId).subscribe({
      next: () => {
        this.spaces = this.spaces.filter(space => space.id !== spaceId);
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo eliminar el espacio.';
        this.loading = false;
      }
    });
  }

  isOwner(space: CreativeSpace): boolean {
    return !!this.currentUserId && space.ownerId === this.currentUserId;
  }

  isFavorite(spaceId: number): boolean {
    return this.favoriteIds.has(spaceId);
  }

  toggleFavorite(spaceId: number): void {
    if (!this.currentUserId) return;
    const next = this.creativeSpaceService.toggleFavoriteSpace(this.currentUserId, spaceId);
    this.favoriteIds = new Set(next);
  }

  togglePermissions(space: CreativeSpace): void {
    if (!this.isOwner(space)) return;

    if (this.openPermissionsSpaceId === space.id) {
      this.openPermissionsSpaceId = null;
      return;
    }

    this.openPermissionsSpaceId = space.id;
    this.ensurePermissionForm(space.id);
    this.loadPermissions(space.id);
  }

  shareSpace(space: CreativeSpace): void {
    const form = this.permissionForms[space.id];
    if (!form || form.invalid) return;

    const userId = String(form.value.userId || '').trim();
    const permissionLevel = Number(form.value.permissionLevel);

    this.permissionLoadingBySpace[space.id] = true;
    this.permissionErrorBySpace[space.id] = '';

    this.creativeSpaceService.shareSpace(space.id, {
      userId,
      permissionLevel
    }).subscribe({
      next: permission => {
        const existing = this.permissionsBySpace[space.id] || [];
        const filtered = existing.filter(item => item.userId !== permission.userId);
        this.permissionsBySpace[space.id] = [permission, ...filtered];
        space.privacy = SpacePrivacy.Shared;
        form.patchValue({ userId: '', permissionLevel: SpacePermissionLevel.Viewer });
        this.permissionLoadingBySpace[space.id] = false;
      },
      error: (err) => {
        this.permissionErrorBySpace[space.id] = err?.error?.message || 'No se pudo compartir el espacio.';
        this.permissionLoadingBySpace[space.id] = false;
      }
    });
  }

  removePermission(space: CreativeSpace, userId: string): void {
    if (!confirm('¿Seguro que quieres revocar el acceso de este usuario?')) return;

    this.permissionLoadingBySpace[space.id] = true;
    this.permissionErrorBySpace[space.id] = '';

    this.creativeSpaceService.removePermission(space.id, userId).subscribe({
      next: () => {
        this.permissionsBySpace[space.id] = (this.permissionsBySpace[space.id] || [])
          .filter(item => item.userId !== userId);
        this.permissionLoadingBySpace[space.id] = false;
      },
      error: (err) => {
        this.permissionErrorBySpace[space.id] = err?.error?.message || 'No se pudo revocar el permiso.';
        this.permissionLoadingBySpace[space.id] = false;
      }
    });
  }

  permissionText(level: SpacePermissionLevel): string {
    return level === SpacePermissionLevel.Editor ? 'Editor' : 'Lector';
  }

  friendOptionLabel(friend: User): string {
    const name = (friend.fullName || '').trim();
    const email = (friend.email || '').trim();

    if (name && email) {
      return `${name} - ${email}`;
    }

    return name || email || 'Usuario sin identificar';
  }

  privacyText(privacy: SpacePrivacy): string {
    return privacy === SpacePrivacy.Shared ? 'Compartido' : 'Privado';
  }

  private loadSpaces(): void {
    this.loading = true;
    this.error = '';
    this.creativeSpaceService.getSpaces().subscribe({
      next: (spaces) => {
        this.spaces = spaces;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudieron cargar los espacios.';
        this.loading = false;
      }
    });
  }

  private ensurePermissionForm(spaceId: number): void {
    if (this.permissionForms[spaceId]) return;

    this.permissionForms[spaceId] = this.fb.group({
      userId: ['', Validators.required],
      permissionLevel: [SpacePermissionLevel.Viewer, Validators.required]
    });
  }

  private loadPermissions(spaceId: number): void {
    this.permissionLoadingBySpace[spaceId] = true;
    this.permissionErrorBySpace[spaceId] = '';

    this.creativeSpaceService.getPermissions(spaceId).subscribe({
      next: permissions => {
        this.permissionsBySpace[spaceId] = permissions;
        this.permissionLoadingBySpace[spaceId] = false;
      },
      error: (err) => {
        this.permissionErrorBySpace[spaceId] = err?.error?.message || 'No se pudieron cargar los permisos.';
        this.permissionLoadingBySpace[spaceId] = false;
      }
    });
  }

  private loadAcceptedFriends(): void {
    this.friendsLoading = true;
    this.friendsError = '';

    this.friendshipService.getAcceptedFriendships().subscribe({
      next: friendships => {
        this.acceptedFriendships = friendships;
        this.friendsLoading = false;
      },
      error: (err) => {
        this.friendsError = err?.error?.message || 'No se pudo cargar la lista de amigos.';
        this.friendsLoading = false;
      }
    });
  }
}
