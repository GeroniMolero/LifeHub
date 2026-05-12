import { Component, DestroyRef, HostListener, OnDestroy, OnInit, SecurityContext, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/operators';
import { marked } from 'marked';
import { HttpErrorResponse } from '@angular/common/http';

import { CreativeSpace, SpacePermission, SpacePermissionLevel, SpacePrivacy, UpdateCreativeSpaceRequest } from '../../../models/creative-space.model';
import { CreateDocumentRequest, Document, DocumentType, UpdateDocumentRequest } from '../../../models/document.model';
import { SpaceMediaReference } from '../../../models/space-media-reference.model';
import { Friendship } from '../../../models/friendship.model';
import { User } from '../../../models/auth.model';
import { MEDIA_EMBED_ALLOWED_DOMAINS } from '../../../config/media-allowlist.config';
import { AllowedWebsiteService } from '../../../services/allowed-website.service';
import { AuthService } from '../../../services/auth.service';
import { ConfirmationService } from '../../../services/confirmation.service';
import { CreativeSpaceService } from '../../../services/creative-space.service';
import { DocumentService } from '../../../services/document.service';
import { DocumentVersionService } from '../../../services/document-version.service';
import { FriendshipService } from '../../../services/friendship.service';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';
import { SpaceMediaSessionService } from '../../../services/space-media-session.service';
import { ToastService } from '../../../services/toast.service';
import { ModalComponent } from '../../../components/modal/modal.component';
import { SpaceDocumentsSidebarComponent } from '../components/space-documents-sidebar/space-documents-sidebar.component';
import { SpaceEditorMainComponent } from '../components/space-editor-main/space-editor-main.component';
import { SpaceMediaSidebarComponent } from '../components/space-media-sidebar/space-media-sidebar.component';

interface VisualMediaLayout {
  x: number;
  y: number;
  width: number;
  zIndex: number;
}

@Component({
  selector: 'app-space-workspace',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ModalComponent,
    SpaceDocumentsSidebarComponent,
    SpaceEditorMainComponent,
    SpaceMediaSidebarComponent
  ],
  templateUrl: './space-workspace.component.html',
  styleUrls: ['./space-workspace.component.scss']
})
export class SpaceWorkspaceComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);

  space: CreativeSpace | null = null;
  documents: Document[] = [];
  mediaReferences: SpaceMediaReference[] = [];
  selectedDocument: Document | null = null;
  activeTab: 'code' | 'preview' | 'split' = 'code';

  loading = true;
  loadingDocuments = false;
  loadingMedia = false;
  error = '';
  mediaError = '';
  renderedPreview = '';

  showDocumentModal = false;
  documentModalTab: 'create' | 'import' = 'create';
  importableDocuments: Document[] = [];
  loadingImportable = false;
  showMediaModal = false;
  showSettingsModal = false;
  settingsModalTab: 'edit' | 'permissions' = 'edit';
  submittedEdit = false;
  permissionForm: FormGroup | null = null;
  permissions: SpacePermission[] = [];
  permissionLoading = false;
  acceptedFriendships: Friendship[] = [];
  friendsLoading = false;
  currentUserId = '';
  readonly SpacePermissionLevel = SpacePermissionLevel;
  readonly DocumentType = DocumentType;
  mediaTab: 'embed' | 'local' = 'embed';

  editSpaceForm!: FormGroup;
  createDocumentForm!: FormGroup;
  editDocumentForm!: FormGroup;
  createEmbedForm!: FormGroup;
  localFileLabelControl!: FormControl<string>;

  selectedLocalFile: File | null = null;
  selectedMediaId: string | null = null;
  localFileBlobUrls = new Map<string, string>();
  visualLayouts = new Map<string, VisualMediaLayout>();
  activeVisualMediaIds = new Set<string>();
  private draggingMedia: { id: string; offsetX: number; offsetY: number } | null = null;
  private zIndexCounter = 10;

  readonly allowedEmbedDomains: string[] = [...MEDIA_EMBED_ALLOWED_DOMAINS];
  private readonly markdownRenderer = new marked.Renderer();

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer,
    private allowedWebsiteService: AllowedWebsiteService,
    private authService: AuthService,
    private confirmationService: ConfirmationService,
    private creativeSpaceService: CreativeSpaceService,
    private documentService: DocumentService,
    private documentVersionService: DocumentVersionService,
    private friendshipService: FriendshipService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private mediaSessionService: SpaceMediaSessionService,
    private toastService: ToastService
  ) {
    // Defense-in-depth: do not render raw HTML blocks/tags from markdown input.
    this.markdownRenderer.html = ({ text }) => this.escapeHtml(text);

    // Only allow https:// links; block http:// and other schemes.
    this.markdownRenderer.link = ({ href, title, text }) => {
      if (!href?.startsWith('https://')) return text;
      const titleAttr = title ? ` title="${title}"` : '';
      return `<a href="${href}"${titleAttr} target="_blank" rel="noopener noreferrer">${text}</a>`;
    };

    // Only render images from allowlisted domains.
    this.markdownRenderer.image = ({ href, title, text }) => {
      if (!this.isDomainAllowed(href ?? '')) {
        return `<span class="blocked-image" title="Imagen bloqueada: dominio no permitido">[imagen bloqueada: ${text || href}]</span>`;
      }
      const titleAttr = title ? ` title="${title}"` : '';
      return `<img src="${href}" alt="${text}"${titleAttr}>`;
    };
  }

  private isDomainAllowed(url: string): boolean {
    try {
      const { hostname } = new URL(url);
      return this.allowedEmbedDomains.some(
        domain => hostname === domain || hostname.endsWith('.' + domain)
      );
    } catch {
      return false;
    }
  }

  ngOnInit(): void {
    this.editSpaceForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      privacy: [SpacePrivacy.Private],
      isPublicProfileVisible: [false]
    });

    this.createDocumentForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: [''],
      type: [DocumentType.Note]
    });

    this.editDocumentForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: ['']
    });

    this.editDocumentForm.get('content')!.valueChanges
      .pipe(debounceTime(50), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateRenderedPreview());

    this.createEmbedForm = this.fb.group({
      label: ['', Validators.required],
      url: ['', [Validators.required, Validators.pattern(/^https:\/\/.+/i)]]
    });

    this.localFileLabelControl = this.fb.control('', { nonNullable: true });

    this.allowedWebsiteService.getEmbedAllowlist().subscribe({
      next: (domains) => {
        if (domains?.length) {
          this.allowedEmbedDomains.splice(0, this.allowedEmbedDomains.length, ...domains);
        }
      },
      error: () => {
        // Fallback: mantener allowlist local por defecto si el endpoint no está disponible.
      }
    });

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => { this.currentUserId = user?.id || ''; });

    this.loadAcceptedFriends();

    this.route.data
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (space) => {
          this.applySpaceState(space['space'] as CreativeSpace);
          this.loading = false;
          this.loadDocuments();
          this.loadMediaReferences();
        },
        error: () => {
          this.toastService.error('No se pudo cargar el espacio creativo.');
          this.loading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
    this.localFileBlobUrls.forEach(url => URL.revokeObjectURL(url));
  }

  @HostListener('document:pointermove', ['$event'])
  onPointerMove(event: PointerEvent): void {
    if (!this.draggingMedia) return;

    const layout = this.visualLayouts.get(this.draggingMedia.id);
    if (!layout) return;

    const canvas = document.querySelector('.main-content-canvas') as HTMLElement | null;
    const canvasRect = canvas?.getBoundingClientRect();
    if (!canvasRect) return;

    layout.x = Math.max(0, event.clientX - canvasRect.left - this.draggingMedia.offsetX);
    layout.y = Math.max(0, event.clientY - canvasRect.top - this.draggingMedia.offsetY);
  }

  @HostListener('document:pointerup')
  onPointerUp(): void {
    this.draggingMedia = null;
  }

  openSettingsModal(): void {
    this.showSettingsModal = true;
    this.settingsModalTab = 'edit';
    this.submittedEdit = false;
    if (this.space) {
      this.editSpaceForm.patchValue({
        name: this.space.name,
        description: this.space.description,
        privacy: this.space.privacy,
        isPublicProfileVisible: this.space.isPublicProfileVisible
      });
      this.ensurePermissionForm();
    }
  }

  closeSettingsModal(): void {
    this.showSettingsModal = false;
    this.settingsModalTab = 'edit';
    this.submittedEdit = false;
  }

  setSettingsModalTab(tab: 'edit' | 'permissions'): void {
    this.settingsModalTab = tab;
    if (tab === 'permissions' && this.space) {
      this.loadPermissions(this.space.id);
    }
  }

  saveSpace(): void {
    this.submittedEdit = true;
    if (!this.space || this.editSpaceForm.invalid) return;

    const payload: UpdateCreativeSpaceRequest = {
      ...this.editSpaceForm.value
    };

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.updateSpace(this.space.id, payload).subscribe({
      next: (updatedSpace) => {
        this.showSettingsModal = false;
        this.submittedEdit = false;
        this.applySpaceState(updatedSpace);
        this.loading = false;
      },
      error: () => {
        this.toastService.error('No se pudo actualizar el espacio creativo.');
        this.loading = false;
      }
    });
  }

  shareSpace(): void {
    if (!this.space || !this.permissionForm || this.permissionForm.invalid) return;

    const userId = String(this.permissionForm.value.userId || '').trim();
    const permissionLevel = Number(this.permissionForm.value.permissionLevel);

    this.permissionLoading = true;
    this.creativeSpaceService.shareSpace(this.space.id, { userId, permissionLevel }).subscribe({
      next: permission => {
        this.permissions = [permission, ...this.permissions.filter(p => p.userId !== permission.userId)];
        this.space!.privacy = SpacePrivacy.Shared;
        this.permissionForm!.patchValue({ userId: '', permissionLevel: SpacePermissionLevel.Viewer });
        this.permissionLoading = false;
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'No se pudo compartir el espacio.');
        this.permissionLoading = false;
      }
    });
  }

  removePermission(userId: string): void {
    if (!this.space || !this.confirmationService.confirmAction('¿Seguro que quieres revocar el acceso de este usuario?')) return;

    this.permissionLoading = true;
    this.creativeSpaceService.removePermission(this.space.id, userId).subscribe({
      next: () => {
        this.permissions = this.permissions.filter(p => p.userId !== userId);
        if (this.permissions.length === 0) {
          this.space!.privacy = SpacePrivacy.Private;
        }
        this.permissionLoading = false;
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'No se pudo revocar el permiso.');
        this.permissionLoading = false;
      }
    });
  }

  isOwner(): boolean {
    return !!this.currentUserId && this.space?.ownerId === this.currentUserId;
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

  permissionText(level: SpacePermissionLevel): string {
    return level === SpacePermissionLevel.Editor ? 'Editor' : 'Lector';
  }

  friendOptionLabel(friend: User): string {
    const name = (friend.fullName || '').trim();
    const email = (friend.email || '').trim();
    if (name && email) return `${name} - ${email}`;
    return name || email || 'Usuario sin identificar';
  }

  openDocumentModal(tab: 'create' | 'import'): void {
    this.documentModalTab = tab;
    this.showDocumentModal = true;
    if (tab === 'import') {
      this.loadImportableDocuments();
    }
  }

  closeDocumentModal(): void {
    this.showDocumentModal = false;
    this.createDocumentForm.reset({ title: '', description: '', content: '', type: DocumentType.Note });
  }

  openMediaModal(): void {
    this.showMediaModal = true;
    this.mediaTab = 'embed';
    this.mediaError = '';
  }

  closeMediaModal(): void {
    this.showMediaModal = false;
    this.selectedLocalFile = null;
    this.localFileLabelControl.setValue('');
    this.createEmbedForm.reset({ label: '', url: '' });
  }


  createDocument(): void {
    if (this.createDocumentForm.invalid || !this.space) return;

    const payload: CreateDocumentRequest = {
      ...this.createDocumentForm.value,
      creativeSpaceId: this.space.id
    };

    this.loadingDocuments = true;
    this.documentService.createDocument(payload).subscribe({
      next: (doc) => {
        this.documents = [doc, ...this.documents];
        this.selectDocument(doc);
        this.closeDocumentModal();
        this.loadingDocuments = false;
      },
      error: () => {
        this.toastService.error('No se pudo crear el documento en este espacio.');
        this.loadingDocuments = false;
      }
    });
  }

  selectDocument(document: Document): void {
    this.selectedDocument = document;
    this.activeTab = 'code';
    this.editDocumentForm.patchValue(document);
    this.updateRenderedPreview();
  }

  openImportTab(): void {
    this.documentModalTab = 'import';
    this.loadImportableDocuments();
  }

  private loadImportableDocuments(): void {
    this.loadingImportable = true;
    this.documentService.getDocuments().subscribe({
      next: (docs) => {
        this.importableDocuments = docs.filter(d =>
          d.userId === this.currentUserId && d.creativeSpaceId !== this.space?.id
        );
        this.loadingImportable = false;
      },
      error: () => { this.loadingImportable = false; }
    });
  }

  importDocument(doc: Document): void {
    if (!this.space) return;
    this.loadingImportable = true;
    this.documentService.copyToSpace(doc.id, this.space.id).subscribe({
      next: (copy) => {
        this.documents = [copy, ...this.documents];
        this.closeDocumentModal();
        this.loadingImportable = false;
        this.toastService.success('Documento importado al espacio.');
      },
      error: (err: any) => {
        this.toastService.error(err?.error?.message ?? 'No se pudo importar el documento.');
        this.loadingImportable = false;
      }
    });
  }

  setActiveTab(tab: 'code' | 'preview' | 'split'): void {
    this.activeTab = tab;
    if (tab === 'preview' || tab === 'split') {
      this.updateRenderedPreview();
    }
  }

  saveDocument(): void {
    if (!this.selectedDocument || this.editDocumentForm.invalid || !this.space) return;

    this.loadingDocuments = true;
    this.documentVersionService.getDocumentVersions(this.selectedDocument.id).subscribe({
      next: (versions) => {
        if (versions.length >= 30) {
          this.toastService.error('Este documento ya tiene 30 versiones. Debes borrar una versión antes de guardar.');
          this.loadingDocuments = false;
          return;
        }

        this.performDocumentSave();
      },
      error: () => {
        this.toastService.error('No se pudo verificar el límite de versiones antes de guardar.');
        this.loadingDocuments = false;
      }
    });
  }

  private performDocumentSave(): void {
    if (!this.selectedDocument || this.editDocumentForm.invalid || !this.space) return;

    const payload: UpdateDocumentRequest = {
      ...this.editDocumentForm.value,
      creativeSpaceId: this.space.id
    };

    this.documentService.updateDocument(this.selectedDocument.id, payload).subscribe({
      next: (updated) => {
        this.documents = this.documents.map(doc => doc.id === updated.id ? updated : doc);
        this.selectedDocument = updated;
        this.editDocumentForm.patchValue(updated);
        this.updateRenderedPreview();
        this.loadingDocuments = false;
      },
      error: (err: HttpErrorResponse) => {
        this.toastService.error(err?.error?.message ?? 'No se pudo guardar el documento.');
        this.loadingDocuments = false;
      }
    });
  }

  deleteDocument(documentId: number): void {
    if (!this.confirmationService.confirmDelete('este documento')) return;

    this.loadingDocuments = true;
    this.documentService.deleteDocument(documentId).subscribe({
      next: () => {
        this.documents = this.documents.filter(doc => doc.id !== documentId);
        if (this.selectedDocument?.id === documentId) {
          this.selectedDocument = null;
        }
        this.loadingDocuments = false;
      },
      error: () => {
        this.toastService.error('No se pudo eliminar el documento.');
        this.loadingDocuments = false;
      }
    });
  }

  addEmbedReference(): void {
    if (this.createEmbedForm.invalid || !this.space) return;

    this.mediaError = '';
    const label = String(this.createEmbedForm.value.label || '').trim();
    const sourceUrl = String(this.createEmbedForm.value.url || '').trim();

    const parsed = this.parseAndValidateEmbedUrl(sourceUrl);
    if (!parsed) {
      this.toastService.error('Enlace no permitido. Usa una URL https de una web autorizada y compatible con embed.');
      return;
    }

    this.loadingMedia = true;
    this.creativeSpaceService.addMediaReference(this.space.id, {
      label,
      source: sourceUrl,
      provider: parsed.provider,
      embedUrl: parsed.embedUrl
    }).subscribe({
      next: (reference) => {
        this.ensureVisualLayout(reference);
        this.mediaReferences = [
          reference,
          ...this.mediaReferences.filter(item => item.id !== reference.id)
        ];
        this.closeMediaModal();
        this.loadingMedia = false;
      },
      error: () => {
        this.toastService.error('No se pudo guardar el enlace multimedia en la base de datos.');
        this.loadingMedia = false;
      }
    });
  }

  onLocalFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (!target.files || target.files.length === 0) {
      this.selectedLocalFile = null;
      return;
    }

    const file = target.files[0];
    if (!(file.type.startsWith('audio/') || file.type.startsWith('video/') || file.type.startsWith('image/'))) {
      this.toastService.error('Selecciona un archivo de audio, video o imagen válido.');
      this.selectedLocalFile = null;
      return;
    }

    this.mediaError = '';
    this.selectedLocalFile = file;
  }

  addLocalFileReference(): void {
    if (!this.space || !this.selectedLocalFile) return;

    const file = this.selectedLocalFile;
    const customLabel = String(this.localFileLabelControl.value || '').trim();

    const reference: SpaceMediaReference = {
      id: this.newReferenceId(),
      type: 'local-session-file',
      label: customLabel || file.name,
      source: file.name,
      mimeType: file.type,
      createdAt: new Date().toISOString()
    };

    const blobUrl = URL.createObjectURL(file);
    this.localFileBlobUrls.set(reference.id, blobUrl);
    this.ensureVisualLayout(reference);

    this.mediaSessionService.addReference(this.space.id, reference);
    this.mediaReferences = [reference, ...this.mediaReferences.filter(item => item.id !== reference.id)];
    this.closeMediaModal();
  }

  get visualMediaReferences(): SpaceMediaReference[] {
    return this.mediaReferences.filter(item => this.isVisualReference(item));
  }

  get activeVisualMediaReferences(): SpaceMediaReference[] {
    return this.visualMediaReferences.filter(item => this.activeVisualMediaIds.has(item.id));
  }

  get audioMediaReferences(): SpaceMediaReference[] {
    return this.mediaReferences.filter(item => this.isAudioReference(item));
  }

  toggleMedia(id: string): void {
    this.selectedMediaId = this.selectedMediaId === id ? null : id;
  }

  onMediaReferenceClick(item: SpaceMediaReference): void {
    this.toggleMedia(item.id);
    if (!this.isVisualReference(item)) {
      return;
    }

    if (this.activeVisualMediaIds.has(item.id)) {
      this.activeVisualMediaIds.delete(item.id);
      return;
    }

    this.activeVisualMediaIds.add(item.id);
    this.ensureVisualLayout(item);
    this.bringVisualToFront(item.id);
  }

  private isEmbedReference(item: SpaceMediaReference): boolean {
    return item.type === 'external-embed' && !!item.embedUrl;
  }

  private isAudioReference(item: SpaceMediaReference): boolean {
    return item.type === 'local-session-file' && !!item.mimeType?.startsWith('audio/');
  }

  startDraggingMedia(event: PointerEvent, id: string): void {
    event.preventDefault();
    const target = event.currentTarget as HTMLElement | null;
    const card = target?.closest('.visual-item') as HTMLElement | null;
    if (!card) return;

    const rect = card.getBoundingClientRect();
    this.draggingMedia = {
      id,
      offsetX: event.clientX - rect.left,
      offsetY: event.clientY - rect.top
    };

    this.bringVisualToFront(id);
  }

  removeMediaReference(id: string): void {
    if (!this.space) return;

    if (!this.confirmationService.confirmAction('¿Seguro que quieres eliminar esta referencia multimedia?')) {
      return;
    }

    const reference = this.mediaReferences.find(item => item.id === id);
    if (!reference) return;

    const blobUrl = this.localFileBlobUrls.get(id);
    if (blobUrl) {
      URL.revokeObjectURL(blobUrl);
      this.localFileBlobUrls.delete(id);
    }

    if (this.selectedMediaId === id) {
      this.selectedMediaId = null;
    }
    this.activeVisualMediaIds.delete(id);
    this.visualLayouts.delete(id);

    if (reference.type === 'external-embed') {
      this.loadingMedia = true;
      this.creativeSpaceService.removeMediaReference(this.space.id, id).subscribe({
        next: () => {
          this.mediaReferences = this.mediaReferences.filter(item => item.id !== id);
          this.loadingMedia = false;
        },
        error: () => {
          this.toastService.error('No se pudo eliminar el enlace multimedia en la base de datos.');
          this.loadingMedia = false;
        }
      });
      return;
    }

    this.mediaSessionService.removeReference(this.space.id, id);
    this.mediaReferences = this.mediaReferences.filter(item => item.id !== id);
  }

  getDocumentTypeText(type?: DocumentType | string | number): string {
    const map: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (map[Number(type)] || 'Nota') : 'Nota';
  }

  getPrivacyText(privacy: SpacePrivacy): string {
    return privacy === SpacePrivacy.Shared ? 'Compartido' : 'Privado';
  }

  private loadDocuments(): void {
    if (!this.space) return;

    this.loadingDocuments = true;
    this.documentService.getDocuments(this.space.id).subscribe({
      next: (documents) => {
        this.documents = documents;
        this.loadingDocuments = false;
      },
      error: () => {
        this.toastService.error('No se pudieron cargar los documentos del espacio.');
        this.loadingDocuments = false;
      }
    });
  }

  private loadMediaReferences(): void {
    if (!this.space) return;
    this.loadingMedia = true;

    const localSessionReferences = this.mediaSessionService
      .getReferences(this.space.id)
      .filter(reference => reference.type === 'local-session-file');

    this.creativeSpaceService.getMediaReferences(this.space.id).subscribe({
      next: (persistedReferences) => {
        this.mediaReferences = [...persistedReferences, ...localSessionReferences];
        this.mediaReferences.forEach(reference => this.ensureVisualLayout(reference));
        this.loadingMedia = false;
      },
      error: () => {
        this.mediaReferences = [...localSessionReferences];
        this.mediaReferences.forEach(reference => this.ensureVisualLayout(reference));
        this.toastService.error('No se pudieron cargar los enlaces multimedia guardados.');
        this.loadingMedia = false;
      }
    });
  }

  private isVisualReference(item: SpaceMediaReference): boolean {
    return this.isEmbedReference(item) || (
      item.type === 'local-session-file' && (
        !!item.mimeType?.startsWith('video/') || !!item.mimeType?.startsWith('image/')
      )
    );
  }

  private ensureVisualLayout(reference: SpaceMediaReference): void {
    if (!this.isVisualReference(reference) || this.visualLayouts.has(reference.id)) {
      return;
    }

    const offset = this.visualLayouts.size * 24;
    this.visualLayouts.set(reference.id, {
      x: 24 + Math.min(offset, 240),
      y: 24 + Math.min(offset, 180),
      width: reference.type === 'external-embed' ? 360 : (reference.mimeType?.startsWith('image/') ? 260 : 320),
      zIndex: ++this.zIndexCounter
    });
  }

  private bringVisualToFront(id: string): void {
    const layout = this.visualLayouts.get(id);
    if (!layout) return;
    layout.zIndex = ++this.zIndexCounter;
  }

  private applySpaceState(space: CreativeSpace): void {
    this.space = space;
    this.editSpaceForm.patchValue({
      name: space.name,
      description: space.description,
      privacy: space.privacy,
      isPublicProfileVisible: space.isPublicProfileVisible
    });
    this.layoutHeaderStateService.setOverride({
      title: space.name,
      description: space.description?.trim() || 'Editor y multimedia del espacio seleccionado',
      meta: [
        this.getPrivacyText(space.privacy),
        ...(space.isPublicProfileVisible ? ['Visible en perfil'] : []),
        `Actualizado ${new Intl.DateTimeFormat('es-ES', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(space.updatedAt))}`
      ],
      actions: [
        ...(this.isOwner() ? [{
          label: 'Ajustes',
          variant: 'ghost' as const,
          action: () => this.openSettingsModal()
        }] : []),
        {
          label: 'Volver a espacios',
          variant: 'secondary' as const,
          route: '/spaces'
        }
      ]
    });
  }

  private ensurePermissionForm(): void {
    if (this.permissionForm) return;
    this.permissionForm = this.fb.group({
      userId: ['', Validators.required],
      permissionLevel: [SpacePermissionLevel.Viewer, Validators.required]
    });
  }

  private loadPermissions(spaceId: number): void {
    this.permissionLoading = true;
    this.creativeSpaceService.getPermissions(spaceId).subscribe({
      next: permissions => {
        this.permissions = permissions;
        this.permissionLoading = false;
      },
      error: (err) => {
        this.toastService.error(err?.error?.message || 'No se pudieron cargar los permisos.');
        this.permissionLoading = false;
      }
    });
  }

  private loadAcceptedFriends(): void {
    this.friendsLoading = true;
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

  private updateRenderedPreview(): void {
    const content = String(this.editDocumentForm.get('content')?.value || '').trim();

    if (!content) {
      this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, '<p>(sin contenido)</p>') || '';
      return;
    }

    const html = this.renderMarkdownToHtml(content);

    this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, html) || '';
  }

  private renderMarkdownToHtml(markdown: string): string {
    // Pass raw markdown to marked; task list symbols are handled in postprocessing
    // to avoid raw HTML being intercepted by the security renderer.html callback.
    const rendered = marked.parse(markdown, {
      async: false,
      breaks: true,
      gfm: true,
      renderer: this.markdownRenderer
    });

    if (typeof rendered !== 'string') {
      return '';
    }

    // Neutralize javascript: links (defense-in-depth before Angular sanitizer).
    const withoutJavascriptLinks = rendered.replace(
      /(<a\b[^>]*\bhref\s*=\s*["'])\s*javascript:[^"']*(["'][^>]*>)/gi,
      '$1#$2'
    );

    // Replace GFM checkbox inputs (stripped by Angular sanitizer) with visual symbols.
    return withoutJavascriptLinks
      .replace(
        /<li>\s*<input\s[^>]*\bchecked\b[^>]*>\s*/gi,
        '<li class="task-item"><span class="task-box" aria-label="completada">☑</span> '
      )
      .replace(
        /<li>\s*<input\s[^>]*type="checkbox"[^>]*>\s*/gi,
        '<li class="task-item"><span class="task-box" aria-label="pendiente">☐</span> '
      );
  }

  private escapeHtml(value: string): string {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private parseAndValidateEmbedUrl(inputUrl: string): { provider: string; embedUrl: string } | null {
    let url: URL;

    try {
      url = new URL(inputUrl);
    } catch {
      return null;
    }

    if (url.protocol !== 'https:') {
      return null;
    }

    const host = url.hostname.toLowerCase().replace(/^www\./, '');

    if (host === 'youtube.com' || host === 'youtu.be') {
      const videoId = host === 'youtu.be'
        ? url.pathname.replace('/', '')
        : (url.searchParams.get('v') || '');
      if (!videoId) return null;
      return { provider: 'YouTube', embedUrl: `https://www.youtube.com/embed/${videoId}` };
    }

    if (host === 'spotify.com' || host.endsWith('.spotify.com')) {
      const path = url.pathname;
      if (!path.includes('/track/') && !path.includes('/album/') && !path.includes('/playlist/') && !path.includes('/episode/')) {
        return null;
      }
      return { provider: 'Spotify', embedUrl: `https://open.spotify.com/embed${path}` };
    }

    if (host === 'vimeo.com' || host.endsWith('.vimeo.com')) {
      const id = url.pathname.split('/').filter(Boolean)[0];
      if (!id) return null;
      return { provider: 'Vimeo', embedUrl: `https://player.vimeo.com/video/${id}` };
    }

    if (host === 'dailymotion.com' || host.endsWith('.dailymotion.com')) {
      const parts = url.pathname.split('/').filter(Boolean);
      const videoPart = parts.find(part => part.startsWith('video'));
      if (!videoPart) return null;
      const id = videoPart.split('_')[1];
      if (!id) return null;
      return { provider: 'Dailymotion', embedUrl: `https://www.dailymotion.com/embed/video/${id}` };
    }

    if (host === 'twitch.tv' || host.endsWith('.twitch.tv')) {
      const channel = url.pathname.split('/').filter(Boolean)[0];
      if (!channel) return null;
      const parentHost = window.location.hostname;
      return {
        provider: 'Twitch',
        embedUrl: `https://player.twitch.tv/?channel=${encodeURIComponent(channel)}&parent=${encodeURIComponent(parentHost)}`
      };
    }

    // Support any additional allowed domain without requiring a code change.
    // If no provider-specific embed conversion exists, keep the original URL.
    return { provider: 'Embed', embedUrl: inputUrl };
  }

  private newReferenceId(): string {
    return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }
}
