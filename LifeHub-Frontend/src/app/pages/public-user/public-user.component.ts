import { Component, DestroyRef, OnDestroy, OnInit, SecurityContext, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { marked } from 'marked';
import { User } from '../../models/auth.model';
import { Friendship, FriendshipStatus } from '../../models/friendship.model';
import { CreativeSpace, SpacePrivacy } from '../../models/creative-space.model';
import { Document } from '../../models/document.model';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { FriendshipService } from '../../services/friendship.service';
import { CreativeSpaceService } from '../../services/creative-space.service';
import { DocumentService } from '../../services/document.service';
import { ConfirmationService } from '../../services/confirmation.service';
import { LayoutHeaderStateService } from '../../services/layout-header-state.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../../components/modal/modal.component';
import { ConfigService } from '../../services/config.service';
import { CdkDragDrop, moveItemInArray, DragDropModule } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-public-user',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, ModalComponent, DragDropModule],
  templateUrl: './public-user.component.html',
  styleUrls: ['./public-user.component.scss']
})
export class PublicUserComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);

  user: User | null = null;
  loading = false;
  error = '';

  currentUserId = '';
  friendship: Friendship | null = null;
  loadingFriendship = false;
  publicSpaces: CreativeSpace[] = [];
  loadingSpaces = false;
  publicDocuments: Document[] = [];
  loadingDocs = false;
  previewDoc: Document | null = null;
  previewHtml = '';

  showEdit = false;
  allSpaces: CreativeSpace[] = [];
  loadingAllSpaces = false;
  allPublishedDocs: Document[] = [];
  loadingAllPublishedDocs = false;
  profileForm!: FormGroup;
  passwordForm!: FormGroup;
  submittedProfile = false;
  submittedPassword = false;
  savingProfile = false;
  savingPassword = false;

  showDocPicker = false;
  showSpacePicker = false;

  get editVisibleDocs(): Document[] { return this.publicDocuments; }
  get editAvailableDocs(): Document[] {
    const visibleIds = new Set(this.publicDocuments.map(d => d.id));
    return this.allPublishedDocs.filter(d => !visibleIds.has(d.id));
  }
  get editDocEmptySlots(): null[] {
    return Array(Math.max(0, this.MAX_PROFILE_DOCS - this.publicDocuments.length)).fill(null);
  }
  get editVisibleSpaces(): CreativeSpace[] { return this.publicSpaces; }
  get editAvailableSpaces(): CreativeSpace[] {
    const visibleIds = new Set(this.publicSpaces.map(s => s.id));
    return this.allSpaces.filter(s => s.privacy !== SpacePrivacy.Private && !visibleIds.has(s.id));
  }
  get editSpaceEmptySlots(): null[] {
    return Array(Math.max(0, this.MAX_PROFILE_SPACES - this.publicSpaces.length)).fill(null);
  }

  toggleDocPicker(): void { this.showDocPicker = !this.showDocPicker; this.showSpacePicker = false; }
  toggleSpacePicker(): void { this.showSpacePicker = !this.showSpacePicker; this.showDocPicker = false; }

  dropDoc(event: CdkDragDrop<Document[]>): void {
    moveItemInArray(this.publicDocuments, event.previousIndex, event.currentIndex);
  }

  dropSpace(event: CdkDragDrop<CreativeSpace[]>): void {
    moveItemInArray(this.publicSpaces, event.previousIndex, event.currentIndex);
  }

  removeDocFromProfile(doc: Document): void {
    this.documentService.setDocumentProfileVisibility(doc.id, false).subscribe({
      next: () => { this.publicDocuments = this.publicDocuments.filter(d => d.id !== doc.id); },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar.')
    });
  }

  addDocToProfile(doc: Document): void {
    if (this.publicDocuments.length >= this.MAX_PROFILE_DOCS) {
      this.toast.error(`Solo puedes tener ${this.MAX_PROFILE_DOCS} documentos visibles en tu perfil.`);
      return;
    }
    this.documentService.setDocumentProfileVisibility(doc.id, true).subscribe({
      next: () => { this.publicDocuments = [...this.publicDocuments, doc]; this.showDocPicker = false; },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar.')
    });
  }

  removeSpaceFromProfile(space: CreativeSpace): void {
    const s = this.allSpaces.find(x => x.id === space.id) ?? space;
    this.spaceService.updateSpace(s.id, { name: s.name, description: s.description, privacy: s.privacy, isFavorite: s.isFavorite, isPublicProfileVisible: false }).subscribe({
      next: () => {
        this.publicSpaces = this.publicSpaces.filter(x => x.id !== s.id);
        this.allSpaces = this.allSpaces.map(x => x.id === s.id ? { ...x, isPublicProfileVisible: false } : x);
      },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar.')
    });
  }

  addSpaceToProfile(space: CreativeSpace): void {
    if (this.publicSpaces.length >= this.MAX_PROFILE_SPACES) {
      this.toast.error(`Solo puedes tener ${this.MAX_PROFILE_SPACES} espacios visibles en tu perfil.`);
      return;
    }
    this.spaceService.updateSpace(space.id, { name: space.name, description: space.description, privacy: space.privacy, isFavorite: space.isFavorite, isPublicProfileVisible: true }).subscribe({
      next: () => {
        this.publicSpaces = [...this.publicSpaces, space];
        this.allSpaces = this.allSpaces.map(x => x.id === space.id ? { ...x, isPublicProfileVisible: true } : x);
        this.showSpacePicker = false;
      },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar.')
    });
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer,
    private userService: UserService,
    private authService: AuthService,
    private friendshipService: FriendshipService,
    private spaceService: CreativeSpaceService,
    private documentService: DocumentService,
    private confirmationService: ConfirmationService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private toast: ToastService,
    public config: ConfigService
  ) {}

  ngOnInit(): void {
    document.body.classList.add('profile-page');
    this.initForms();

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(u => {
        this.currentUserId = u?.id ?? '';
        this.setHeaderState();
      });

    const userId = this.route.snapshot.paramMap.get('id');
    if (!userId) {
      this.error = 'Usuario no válido.';
      return;
    }

    this.loadUser(userId);
    this.loadFriendship(userId);
    this.loadPublicSpaces(userId);
    this.loadPublicDocuments(userId);
  }

  ngOnDestroy(): void {
    document.body.classList.remove('profile-page');
    this.layoutHeaderStateService.clearOverride();
  }

  get isOwnProfile(): boolean {
    return !!this.currentUserId && !!this.user && this.user.id === this.currentUserId;
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

  onProfileSubmit(): void {
    this.submittedProfile = true;
    if (this.profileForm.invalid) return;

    this.savingProfile = true;
    const data = {
      ...this.profileForm.value,
      profilePictureUrl: this.profileForm.value.profilePictureUrl?.trim() || null
    };
    this.userService.updateProfile(data).subscribe({
      next: updatedUser => {
        this.user = { ...this.user!, ...updatedUser };
        this.authService.refreshCurrentUser().subscribe();
        this.toast.success('Perfil actualizado correctamente.');
        this.savingProfile = false;
      },
      error: err => {
        this.toast.error(err?.error?.message || 'No se pudo actualizar el perfil.');
        this.savingProfile = false;
      }
    });
  }

  onPasswordSubmit(): void {
    this.submittedPassword = true;
    if (this.passwordForm.invalid) return;

    const newPassword = this.passwordForm.get('newPassword')?.value;
    const confirmPassword = this.passwordForm.get('confirmPassword')?.value;
    if (newPassword !== confirmPassword) {
      this.toast.error('Las contraseñas no coinciden.');
      return;
    }

    this.savingPassword = true;
    this.userService.changePassword(
      this.passwordForm.get('currentPassword')?.value,
      newPassword
    ).subscribe({
      next: () => {
        this.toast.success('Contraseña actualizada correctamente.');
        this.passwordForm.reset();
        this.submittedPassword = false;
        this.savingPassword = false;
      },
      error: err => {
        this.toast.error(err?.error?.message || 'No se pudo actualizar la contraseña.');
        this.savingPassword = false;
      }
    });
  }

  deleteAccount(): void {
    if (!this.confirmationService.confirmDelete('tu cuenta permanentemente')) return;
    this.userService.deleteCurrentUser().subscribe({
      next: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      },
      error: err => this.toast.error(err?.error?.message || 'No se pudo eliminar la cuenta.')
    });
  }

  getDocThumb(doc: Document): string | null {
    if (!doc.content) return null;
    const match = doc.content.match(/!\[.*?\]\(([^)]+)\)/);
    return match ? match[1] : null;
  }

  openDocPreview(doc: Document): void {
    this.previewDoc = doc;
    const raw = marked.parse(doc.content || '', { async: false, breaks: true, gfm: true });
    this.previewHtml = this.sanitizer.sanitize(SecurityContext.HTML, raw as string) || '';
  }

  closeDocPreview(): void {
    this.previewDoc = null;
    this.previewHtml = '';
  }

  downloadDoc(doc: Document): void {
    const blob = new Blob([doc.content || ''], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${doc.title.replace(/[^a-z0-9\-_]/gi, '-') || 'documento'}.md`;
    a.click();
    URL.revokeObjectURL(url);
  }

  get MAX_PROFILE_SPACES(): number { return this.config.maxProfileSpaces; }
  get MAX_PROFILE_DOCS(): number   { return this.config.maxProfileDocs; }
  get MAX_PUBLISHED_DOCS(): number { return this.config.maxPublishedDocs; }

  get profileVisibleSpacesCount(): number {
    return this.allSpaces.filter(s => s.isPublicProfileVisible).length;
  }

  get profileVisibleDocsCount(): number {
    return this.allPublishedDocs.filter(d => d.isProfileVisible).length;
  }

  get publishedDocsCount(): number {
    return this.allPublishedDocs.length;
  }

  togglePublicVisibility(space: CreativeSpace): void {
    const enabling = !space.isPublicProfileVisible;
    if (enabling && this.profileVisibleSpacesCount >= this.MAX_PROFILE_SPACES) {
      this.toast.error(`Solo puedes tener ${this.MAX_PROFILE_SPACES} espacios visibles en tu perfil.`);
      return;
    }
    this.spaceService.updateSpace(space.id, {
      name: space.name,
      description: space.description,
      privacy: space.privacy,
      isFavorite: space.isFavorite,
      isPublicProfileVisible: enabling
    }).subscribe({
      next: updated => { space.isPublicProfileVisible = updated.isPublicProfileVisible; },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar la visibilidad del espacio.')
    });
  }

  private initForms(): void {
    this.profileForm = this.fb.group({
      fullName: ['', Validators.required],
      bio: [''],
      profilePictureUrl: ['']
    });
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  private setHeaderState(): void {
    if (!this.isOwnProfile) return;
    this.layoutHeaderStateService.setOverride({
      actions: [
        ...(this.showEdit ? [{
          label: 'Guardar perfil',
          variant: 'primary' as const,
          action: () => this.onProfileSubmit()
        }] : []),
        {
          label: this.showEdit ? 'Ver perfil' : 'Editar perfil',
          variant: 'secondary' as const,
          action: () => {
            this.showEdit = !this.showEdit;
            if (this.showEdit && this.allSpaces.length === 0) this.loadAllSpaces();
            if (this.showEdit && this.allPublishedDocs.length === 0) this.loadAllPublishedDocs();
            this.setHeaderState();
          }
        }
      ]
    });
  }

  private loadUser(userId: string): void {
    this.loading = true;
    this.error = '';
    this.userService.getUser(userId).subscribe({
      next: user => {
        this.user = user;
        this.profileForm.patchValue({
          fullName: user.fullName || '',
          bio: user.bio || '',
          profilePictureUrl: user.profilePictureUrl || ''
        });
        this.loading = false;
        this.setHeaderState();
      },
      error: err => {
        this.error = err?.error?.message || 'No se pudo cargar el perfil del usuario.';
        this.loading = false;
      }
    });
  }

  private loadAllSpaces(): void {
    this.loadingAllSpaces = true;
    this.spaceService.getSpaces().subscribe({
      next: spaces => { this.allSpaces = spaces.filter(s => s.ownerId === this.currentUserId); this.loadingAllSpaces = false; },
      error: () => { this.loadingAllSpaces = false; }
    });
  }

  private loadPublicSpaces(userId: string): void {
    this.loadingSpaces = true;
    this.spaceService.getPublicSpacesByUser(userId).subscribe({
      next: spaces => { this.publicSpaces = spaces; this.loadingSpaces = false; },
      error: () => { this.loadingSpaces = false; }
    });
  }

  toggleDocumentProfileVisibility(doc: Document): void {
    const enabling = !doc.isProfileVisible;
    if (enabling && this.profileVisibleDocsCount >= this.MAX_PROFILE_DOCS) {
      this.toast.error(`Solo puedes tener ${this.MAX_PROFILE_DOCS} documentos visibles en tu perfil.`);
      return;
    }
    this.documentService.setDocumentProfileVisibility(doc.id, enabling).subscribe({
      next: () => { doc.isProfileVisible = enabling; },
      error: err => this.toast.error(err?.error?.message || 'No se pudo actualizar la visibilidad del documento.')
    });
  }

  private loadAllPublishedDocs(): void {
    this.loadingAllPublishedDocs = true;
    this.documentService.getDocuments().subscribe({
      next: docs => {
        this.allPublishedDocs = docs.filter(d => d.isPublic && d.userId === this.currentUserId);
        this.loadingAllPublishedDocs = false;
      },
      error: () => { this.loadingAllPublishedDocs = false; }
    });
  }

  private loadPublicDocuments(userId: string): void {
    this.loadingDocs = true;
    this.documentService.getPublicDocumentsByUser(userId).subscribe({
      next: docs => { this.publicDocuments = docs; this.loadingDocs = false; },
      error: () => { this.loadingDocs = false; }
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
