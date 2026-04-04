import { Component, HostListener, OnDestroy, OnInit, SecurityContext } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreativeSpace, SpacePrivacy, UpdateCreativeSpaceRequest } from '../../../models/creative-space.model';
import { CreateDocumentRequest, Document, DocumentType, UpdateDocumentRequest } from '../../../models/document.model';
import { SpaceMediaReference } from '../../../models/space-media-reference.model';
import { MEDIA_EMBED_ALLOWED_DOMAINS } from '../../../config/media-allowlist.config';
import { AllowedWebsiteService } from '../../../services/allowed-website.service';
import { CreativeSpaceService } from '../../../services/creative-space.service';
import { DocumentService } from '../../../services/document.service';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';
import { SpaceMediaSessionService } from '../../../services/space-media-session.service';
import { SpaceSettingsPanelComponent } from '../components/space-settings-panel/space-settings-panel.component';
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
    SpaceSettingsPanelComponent,
    SpaceDocumentsSidebarComponent,
    SpaceEditorMainComponent,
    SpaceMediaSidebarComponent
  ],
  templateUrl: './space-workspace.component.html',
  styleUrls: ['./space-workspace.component.scss']
})
export class SpaceWorkspaceComponent implements OnInit, OnDestroy {
  space: CreativeSpace | null = null;
  documents: Document[] = [];
  mediaReferences: SpaceMediaReference[] = [];
  selectedDocument: Document | null = null;
  activeTab: 'code' | 'preview' = 'code';

  loading = true;
  loadingDocuments = false;
  loadingMedia = false;
  error = '';
  mediaError = '';
  renderedPreview = '';

  showCreateDocument = false;
  showCreateMedia = false;
  showEditSpace = false;
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

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer,
    private allowedWebsiteService: AllowedWebsiteService,
    private creativeSpaceService: CreativeSpaceService,
    private documentService: DocumentService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private mediaSessionService: SpaceMediaSessionService
  ) {}

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

    this.route.data.subscribe({
      next: (space) => {
        this.applySpaceState(space['space'] as CreativeSpace);
        this.loading = false;
        this.loadDocuments();
        this.loadMediaReferences();
      },
      error: () => {
        this.error = 'No se pudo cargar el espacio creativo.';
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

  toggleEditSpace(): void {
    this.showEditSpace = !this.showEditSpace;

    if (this.showEditSpace && this.space) {
      this.editSpaceForm.patchValue({
        name: this.space.name,
        description: this.space.description,
        privacy: this.space.privacy,
        isPublicProfileVisible: this.space.isPublicProfileVisible
      });
    }

    if (this.space) {
      this.applySpaceState(this.space);
    }
  }

  saveSpace(): void {
    if (!this.space || this.editSpaceForm.invalid) return;

    const payload: UpdateCreativeSpaceRequest = {
      ...this.editSpaceForm.value
    };

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.updateSpace(this.space.id, payload).subscribe({
      next: (updatedSpace) => {
        this.showEditSpace = false;
        this.applySpaceState(updatedSpace);
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo actualizar el espacio creativo.';
        this.loading = false;
      }
    });
  }

  toggleCreateDocument(): void {
    this.showCreateDocument = !this.showCreateDocument;
  }

  toggleCreateMedia(): void {
    this.showCreateMedia = !this.showCreateMedia;
    this.mediaTab = 'embed';
    this.mediaError = '';
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
        this.showCreateDocument = false;
        this.createDocumentForm.reset({
          title: '',
          description: '',
          content: '',
          type: DocumentType.Note
        });
        this.loadingDocuments = false;
      },
      error: () => {
        this.error = 'No se pudo crear el documento en este espacio.';
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

  setActiveTab(tab: 'code' | 'preview'): void {
    this.activeTab = tab;
    if (tab === 'preview') {
      this.updateRenderedPreview();
    }
  }

  saveDocument(): void {
    if (!this.selectedDocument || this.editDocumentForm.invalid || !this.space) return;

    const payload: UpdateDocumentRequest = {
      ...this.editDocumentForm.value,
      creativeSpaceId: this.space.id
    };

    this.loadingDocuments = true;
    this.documentService.updateDocument(this.selectedDocument.id, payload).subscribe({
      next: (updated) => {
        this.documents = this.documents.map(doc => doc.id === updated.id ? updated : doc);
        this.selectedDocument = updated;
        this.editDocumentForm.patchValue(updated);
        this.updateRenderedPreview();
        this.loadingDocuments = false;
      },
      error: () => {
        this.error = 'No se pudo guardar el documento.';
        this.loadingDocuments = false;
      }
    });
  }

  deleteDocument(documentId: number): void {
    if (!confirm('¿Seguro que quieres eliminar este documento?')) return;

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
        this.error = 'No se pudo eliminar el documento.';
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
      this.mediaError = 'Enlace no permitido. Usa una URL https de una web autorizada y compatible con embed.';
      return;
    }

    const reference: SpaceMediaReference = {
      id: this.newReferenceId(),
      type: 'external-embed',
      label,
      source: sourceUrl,
      provider: parsed.provider,
      embedUrl: parsed.embedUrl,
      createdAt: new Date().toISOString()
    };

    this.ensureVisualLayout(reference);
    this.mediaReferences = this.mediaSessionService.addReference(this.space.id, reference);
    this.createEmbedForm.reset({ label: '', url: '' });
  }

  onLocalFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (!target.files || target.files.length === 0) {
      this.selectedLocalFile = null;
      return;
    }

    const file = target.files[0];
    if (!(file.type.startsWith('audio/') || file.type.startsWith('video/') || file.type.startsWith('image/'))) {
      this.mediaError = 'Selecciona un archivo de audio, video o imagen válido.';
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

    this.mediaReferences = this.mediaSessionService.addReference(this.space.id, reference);
    this.selectedLocalFile = null;
    this.localFileLabelControl.setValue('');
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
    this.mediaReferences = this.mediaSessionService.removeReference(this.space.id, id);
  }

  getPrivacyText(privacy: SpacePrivacy): string {
    return privacy === SpacePrivacy.Shared ? 'Compartido' : 'Privado';
  }

  private loadDocuments(): void {
    if (!this.space) return;

    this.loadingDocuments = true;
    this.documentService.getDocuments().subscribe({
      next: (documents) => {
        this.documents = documents.filter(doc => Number(doc.creativeSpaceId) === this.space!.id);
        this.loadingDocuments = false;
      },
      error: () => {
        this.error = 'No se pudieron cargar los documentos del espacio.';
        this.loadingDocuments = false;
      }
    });
  }

  private loadMediaReferences(): void {
    if (!this.space) return;
    this.mediaReferences = this.mediaSessionService.getReferences(this.space.id);
    this.mediaReferences.forEach(reference => this.ensureVisualLayout(reference));
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
        {
          label: this.showEditSpace ? 'Cerrar ajustes' : 'Ajustes',
          variant: 'ghost',
          action: () => this.toggleEditSpace()
        },
        {
          label: 'Volver a espacios',
          variant: 'secondary',
          route: '/spaces'
        }
      ]
    });
  }

  private updateRenderedPreview(): void {
    const content = String(this.editDocumentForm.get('content')?.value || '').trim();

    if (!content) {
      this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, '<p>(sin contenido)</p>') || '';
      return;
    }

    const html = this.looksLikeHtml(content)
      ? content
      : this.renderMarkdownToHtml(content);

    this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, html) || '';
  }

  private looksLikeHtml(content: string): boolean {
    return /<\/?[a-z][\s\S]*>/i.test(content);
  }

  private renderMarkdownToHtml(markdown: string): string {
    let html = this.escapeHtml(markdown);

    html = html.replace(/```([\s\S]*?)```/g, '<pre><code>$1</code></pre>');
    html = html.replace(/^###\s+(.+)$/gm, '<h3>$1</h3>');
    html = html.replace(/^##\s+(.+)$/gm, '<h2>$1</h2>');
    html = html.replace(/^#\s+(.+)$/gm, '<h1>$1</h1>');
    html = html.replace(/^>\s+(.+)$/gm, '<blockquote>$1</blockquote>');
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
    html = html.replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
    html = html.replace(/(?:^|\n)(- .+(?:\n- .+)*)/g, (_match, list) => {
      const items = list
        .split('\n')
        .map((item: string) => item.replace(/^-\s+/, '').trim())
        .filter(Boolean)
        .map((item: string) => `<li>${item}</li>`)
        .join('');
      return `\n<ul>${items}</ul>`;
    });

    const blocks = html
      .split(/\n{2,}/)
      .map(block => block.trim())
      .filter(Boolean)
      .map(block => {
        if (/^<(h1|h2|h3|ul|pre|blockquote)/.test(block)) {
          return block;
        }

        return `<p>${block.replace(/\n/g, '<br />')}</p>`;
      });

    return blocks.join('');
  }

  private escapeHtml(value: string): string {
    return value
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
    const isAllowed = this.allowedEmbedDomains.some(domain => host === domain || host.endsWith(`.${domain}`));
    if (!isAllowed) {
      return null;
    }

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

    return null;
  }

  private newReferenceId(): string {
    return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }
}
