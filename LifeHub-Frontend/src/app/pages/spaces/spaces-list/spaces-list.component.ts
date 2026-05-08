import { Component, DestroyRef, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  CreativeSpace,
  SpacePrivacy,
  SpacePermission,
  SpacePermissionLevel
} from '../../../models/creative-space.model';
import { ConfirmationService } from '../../../services/confirmation.service';
import { CreativeSpaceService } from '../../../services/creative-space.service';
import { AuthService } from '../../../services/auth.service';
import { Friendship } from '../../../models/friendship.model';
import { FriendshipService } from '../../../services/friendship.service';
import { User } from '../../../models/auth.model';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';
import { ToastService } from '../../../services/toast.service';
import { ModalComponent } from '../../../components/modal/modal.component';

@Component({
  selector: 'app-spaces-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, ModalComponent],
  templateUrl: './spaces-list.component.html',
  styleUrls: ['./spaces-list.component.scss']
})
export class SpacesListComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);

  spaces: CreativeSpace[] = [];
  filterForm!: FormGroup;
  showFiltersDropdown = false;
  createForm!: FormGroup;
  editForm!: FormGroup;
  editingId: number | null = null;
  editModalTab: 'edit' | 'permissions' = 'edit';
  currentUserId = '';
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
  submittedCreate = false;
  submittedEdit = false;
  SpacePrivacy = SpacePrivacy;
  SpacePermissionLevel = SpacePermissionLevel;

  constructor(
    private fb: FormBuilder,
    private creativeSpaceService: CreativeSpaceService,
    private confirmationService: ConfirmationService,
    private authService: AuthService,
    private friendshipService: FriendshipService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterForm = this.fb.group({
      search:    [''],
      favorites: ['all'],
      privacy:   ['all'],
      sort:      ['updated-desc']
    });

    this.createForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      isPublicProfileVisible: [false]
    });

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      isPublicProfileVisible: [false]
    });

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => {
        this.currentUserId = user?.id || '';
      });

    this.setHeaderState();
    this.loadAcceptedFriends();
    this.loadSpaces();
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  get editingSpace(): CreativeSpace | null {
    if (this.editingId === null) return null;
    return this.spaces.find(s => s.id === this.editingId) ?? null;
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

  get filteredSpaces(): CreativeSpace[] {
    const search    = String(this.filterForm?.get('search')?.value    ?? '').trim().toLowerCase();
    const favorites = String(this.filterForm?.get('favorites')?.value ?? 'all');
    const privacy   = String(this.filterForm?.get('privacy')?.value   ?? 'all');
    const sort      = String(this.filterForm?.get('sort')?.value      ?? 'updated-desc');

    const filtered = this.spaces.filter(space => {
      if (search && !space.name.toLowerCase().includes(search)) return false;
      const isFav = this.isFavorite(space.id);
      if (favorites === 'favorites'     && !isFav) return false;
      if (favorites === 'non-favorites' &&  isFav) return false;
      if (privacy === 'shared'  && space.privacy !== SpacePrivacy.Shared)  return false;
      if (privacy === 'private' && space.privacy !== SpacePrivacy.Private) return false;
      return true;
    });

    return filtered.sort((a, b) => {
      const aDate = new Date(a.updatedAt).getTime();
      const bDate = new Date(b.updatedAt).getTime();
      return sort === 'updated-asc' ? aDate - bDate : bDate - aDate;
    });
  }

  get hasActiveFilters(): boolean {
    if (!this.filterForm) return false;
    const { search, favorites, privacy, sort } = this.filterForm.getRawValue();
    return !!String(search ?? '').trim()
      || favorites !== 'all'
      || privacy   !== 'all'
      || sort      !== 'updated-desc';
  }

  clearFilters(): void {
    this.filterForm.reset({ search: '', favorites: 'all', privacy: 'all', sort: 'updated-desc' });
  }

  toggleFiltersDropdown(): void {
    this.showFiltersDropdown = !this.showFiltersDropdown;
  }

  toggleCreate(): void {
    this.showCreate = !this.showCreate;
    this.setHeaderState();
  }

  createSpace(): void {
    this.submittedCreate = true;
    if (this.createForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.createSpace({
      ...this.createForm.value,
      privacy: SpacePrivacy.Private,
      isFavorite: false
    }).subscribe({
      next: (space) => {
        this.spaces = [space, ...this.spaces];
        this.showCreate = false;
        this.submittedCreate = false;
        this.createForm.reset({ name: '', description: '', isPublicProfileVisible: false });
        this.setHeaderState();
        this.loading = false;
      },
      error: () => {
        this.toastService.error('No se pudo crear el espacio.');
        this.loading = false;
      }
    });
  }

  startEdit(space: CreativeSpace): void {
    this.editingId = space.id;
    this.editModalTab = 'edit';
    this.editForm.patchValue(space);
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editModalTab = 'edit';
    this.submittedEdit = false;
  }

  setEditModalTab(tab: 'edit' | 'permissions'): void {
    this.editModalTab = tab;
    if (tab === 'permissions' && this.editingId !== null) {
      this.ensurePermissionForm(this.editingId);
      this.loadPermissions(this.editingId);
    }
  }

  saveEdit(spaceId: number): void {
    this.submittedEdit = true;
    if (this.editForm.invalid) return;

    this.loading = true;
    this.error = '';
    const current = this.spaces.find(s => s.id === spaceId);
    this.creativeSpaceService.updateSpace(spaceId, {
      ...this.editForm.value,
      privacy: current?.privacy ?? SpacePrivacy.Private,
      isFavorite: current?.isFavorite ?? false
    }).subscribe({
      next: (updated) => {
        this.spaces = this.spaces.map(space => space.id === spaceId ? updated : space);
        this.editingId = null;
        this.submittedEdit = false;
        this.loading = false;
      },
      error: () => {
        this.toastService.error('No se pudo actualizar el espacio.');
        this.loading = false;
      }
    });
  }

  deleteSpace(spaceId: number): void {
    if (!this.confirmationService.confirmDelete('este espacio creativo')) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.deleteSpace(spaceId).subscribe({
      next: () => {
        this.spaces = this.spaces.filter(space => space.id !== spaceId);
        this.loading = false;
      },
      error: () => {
        this.toastService.error('No se pudo eliminar el espacio.');
        this.loading = false;
      }
    });
  }

  isOwner(space: CreativeSpace): boolean {
    return !!this.currentUserId && space.ownerId === this.currentUserId;
  }

  isFavorite(spaceId: number): boolean {
    return this.spaces.find(space => space.id === spaceId)?.isFavorite ?? false;
  }

  toggleFavorite(spaceId: number): void {
    if (!this.currentUserId) return;

    const target = this.spaces.find(space => space.id === spaceId);
    if (!target) return;

    const request$ = target.isFavorite
      ? this.creativeSpaceService.removeFavoriteSpace(spaceId)
      : this.creativeSpaceService.addFavoriteSpace(spaceId);

    request$.subscribe({
      next: () => {
        target.isFavorite = !target.isFavorite;
        target.updatedAt = new Date();
        this.spaces = [...this.spaces];
      },
      error: () => {
        this.toastService.error('No se pudo actualizar favorito.');
      }
    });
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
        this.toastService.error(err?.error?.message || 'No se pudo compartir el espacio.');
        this.permissionLoadingBySpace[space.id] = false;
      }
    });
  }

  removePermission(space: CreativeSpace, userId: string): void {
    if (!this.confirmationService.confirmAction('¿Seguro que quieres revocar el acceso de este usuario?')) return;

    this.permissionLoadingBySpace[space.id] = true;
    this.permissionErrorBySpace[space.id] = '';

    this.creativeSpaceService.removePermission(space.id, userId).subscribe({
      next: () => {
        const remaining = (this.permissionsBySpace[space.id] || []).filter(item => item.userId !== userId);
        this.permissionsBySpace[space.id] = remaining;
        if (remaining.length === 0) {
          space.privacy = SpacePrivacy.Private;
          this.spaces = [...this.spaces];
        }
        this.permissionLoadingBySpace[space.id] = false;
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'No se pudo revocar el permiso.');
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
        this.toastService.error('No se pudieron cargar los espacios.');
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
        this.toastService.error(err?.error?.message || 'No se pudieron cargar los permisos.');
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
        this.toastService.error(err?.error?.message || 'No se pudo cargar la lista de amigos.');
        this.friendsLoading = false;
      }
    });
  }

  private setHeaderState(): void {
    this.layoutHeaderStateService.setOverride({
      actions: [
        {
          label: this.showCreate ? 'Cancelar' : 'Nuevo espacio',
          variant: this.showCreate ? 'secondary' : 'primary',
          action: () => this.toggleCreate()
        }
      ]
    });
  }
}
