import { Component, DestroyRef, OnDestroy, OnInit, SecurityContext, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ModalComponent } from '../../../components/modal/modal.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { DomSanitizer } from '@angular/platform-browser';
import { marked } from 'marked';
import { debounceTime } from 'rxjs/operators';

import { Document, DocumentType } from '../../../models/document.model';
import { DocumentVersion } from '../../../models/document-version.model';
import { DocumentPublication } from '../../../models/document-publication.model';
import { DocumentService } from '../../../services/document.service';
import { ConfirmationService } from '../../../services/confirmation.service';
import { DocumentVersionService } from '../../../services/document-version.service';
import { DocumentPublicationService } from '../../../services/document-publication.service';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { AllowedWebsiteService } from '../../../services/allowed-website.service';
import { MEDIA_EMBED_ALLOWED_DOMAINS } from '../../../config/media-allowlist.config';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ModalComponent],
  templateUrl: './document-detail.component.html',
  styleUrls: ['./document-detail.component.scss']
})
export class DocumentDetailComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);

  document: Document | null = null;
  editForm!: FormGroup;
  publicationForm!: FormGroup;
  versions: DocumentVersion[] = [];
  versionNote = '';
  publication: DocumentPublication | null = null;
  publicationMessage = '';
  publicUrl = '';
  loading = true;
  error = '';
  currentUserId: string | null = null;

  activeTab: 'code' | 'preview' | 'split' = 'preview';
  renderedPreview = '';
  showPublicationModal = false;
  isEditingPublication = false;

  private currentUserName: string | null = null;

  private readonly markdownRenderer = new marked.Renderer();
  private allowedEmbedDomains: string[] = [...MEDIA_EMBED_ALLOWED_DOMAINS];

  get isDocumentOwner(): boolean {
    return !!this.currentUserId && this.document?.userId === this.currentUserId;
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private sanitizer: DomSanitizer,
    private documentService: DocumentService,
    private confirmationService: ConfirmationService,
    private documentVersionService: DocumentVersionService,
    private publicationService: DocumentPublicationService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private authService: AuthService,
    private toastService: ToastService,
    private allowedWebsiteService: AllowedWebsiteService
  ) {
    this.markdownRenderer.html = ({ text }) => this.escapeHtml(text);

    this.markdownRenderer.link = ({ href, title, text }) => {
      if (!href?.startsWith('https://')) return text;
      const titleAttr = title ? ` title="${title}"` : '';
      return `<a href="${href}"${titleAttr} target="_blank" rel="noopener noreferrer">${text}</a>`;
    };

    this.markdownRenderer.image = ({ href, title, text }) => {
      if (!this.isDomainAllowed(href ?? '')) {
        return `<span class="blocked-image" title="Imagen bloqueada: dominio no permitido">[imagen bloqueada: ${text || href}]</span>`;
      }
      const titleAttr = title ? ` title="${title}"` : '';
      return `<img src="${href}" alt="${text}"${titleAttr}>`;
    };
  }

  ngOnInit(): void {
    this.editForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: ['']
    });

    this.publicationForm = this.fb.group({
      isPublic: [false],
      publicTitle: [''],
      publicDescription: [''],
      author: ['']
    });

    this.editForm.get('content')!.valueChanges
      .pipe(debounceTime(50), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateRenderedPreview());

    this.allowedWebsiteService.getEmbedAllowlist().subscribe({
      next: domains => { if (domains?.length) this.allowedEmbedDomains = domains; },
      error: () => {}
    });

    this.authService.getCurrentUser()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => {
        this.currentUserId = user?.id ?? null;
        this.currentUserName = user?.fullName || user?.email || null;
      });

    this.route.data
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          const document = data['document'] as Document;
          this.setDocumentState(document);
        },
        error: () => {
          this.toastService.error('No se pudo cargar el documento.');
          this.loading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  setActiveTab(tab: 'code' | 'preview' | 'split'): void {
    this.activeTab = tab;
    if (tab === 'preview' || tab === 'split') this.updateRenderedPreview();
  }

  saveDocument(): void {
    if (!this.document || this.editForm.invalid) return;
    if (this.versions.length >= 30) {
      this.toastService.error('Este documento ya tiene 30 versiones. Debes borrar una antes de guardar.');
      return;
    }

    this.loading = true;
    this.documentService.updateDocument(this.document.id, this.editForm.value).subscribe({
      next: (updatedDocument) => {
        this.setDocumentState(updatedDocument, false);
        this.loadVersions(updatedDocument.id);
        this.loading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.toastService.error(err?.error?.message ?? 'No se pudo actualizar el documento.');
        this.loading = false;
      }
    });
  }

  deleteDocument(): void {
    if (!this.document) return;
    if (!this.confirmationService.confirmDelete('este documento')) return;

    this.loading = true;
    this.documentService.deleteDocument(this.document.id).subscribe({
      next: () => {
        const target = this.document?.creativeSpaceId
          ? ['/spaces', String(this.document.creativeSpaceId)]
          : ['/documents'];
        this.router.navigate(target);
      },
      error: () => {
        this.toastService.error('No se pudo eliminar el documento.');
        this.loading = false;
      }
    });
  }

  createVersion(): void {
    if (!this.document) return;
    if (this.versions.length >= 30) {
      this.toastService.error('Límite de 30 versiones alcanzado. Elimina una antes de crear otra.');
      return;
    }

    this.loading = true;
    this.documentVersionService.createSnapshot(this.document.id, { note: this.versionNote || undefined }).subscribe({
      next: () => {
        this.versionNote = '';
        this.loadVersions(this.document!.id);
      },
      error: (err: HttpErrorResponse) => {
        this.toastService.error(err?.error?.message ?? 'No se pudo crear la versión.');
        this.loading = false;
      }
    });
  }

  restoreVersion(versionId: number): void {
    if (!this.document) return;
    if (!confirm('¿Seguro que quieres restaurar esta versión? Se sobrescribirá el contenido actual.')) return;

    this.loading = true;
    this.documentVersionService.restoreVersion(versionId).subscribe({
      next: () => this.refreshDocument(),
      error: () => {
        this.toastService.error('No se pudo restaurar la versión.');
        this.loading = false;
      }
    });
  }

  deleteVersion(versionId: number): void {
    if (!this.confirmationService.confirmDelete('esta versión')) return;
    this.loading = true;
    this.documentVersionService.deleteVersion(versionId).subscribe({
      next: () => this.loadVersions(this.document!.id),
      error: (err: HttpErrorResponse) => {
        this.toastService.error(err?.error?.message ?? 'No se pudo eliminar la versión.');
        this.loading = false;
      }
    });
  }

  copyPublicUrl(): void {
    if (!this.publicUrl) return;
    navigator.clipboard.writeText(this.publicUrl);
    this.publicationMessage = 'Enlace público copiado al portapapeles.';
  }

  onPublicationModalClose(): void {
    this.showPublicationModal = false;
    this.isEditingPublication = false;
    this.publicationMessage = '';
  }

  onAccessToggle(): void {
    this.savePublicationData();
  }

  toggleEditPublication(): void {
    if (this.isEditingPublication) this.savePublicationData();
    this.isEditingPublication = !this.isEditingPublication;
  }

  private savePublicationData(): void {
    if (!this.document) return;
    this.loading = true;
    this.publicationMessage = '';

    this.publicationService.upsertPublication(this.document.id, {
      isPublic: Boolean(this.publicationForm.value.isPublic),
      publicTitle: this.publicationForm.value.publicTitle || undefined,
      publicDescription: this.publicationForm.value.publicDescription || undefined,
      author: this.publicationForm.value.author || undefined,
      mediaReferences: [],
      externalLinks: []
    }).subscribe({
      next: publication => {
        this.publication = publication;
        this.document = { ...this.document!, isPublic: publication.isPublic, publishedAt: publication.publishedAt as any };
        this.setHeaderState(this.document);
        this.loading = false;
        this.publicationMessage = publication.isPublic ? 'Documento publicado.' : 'Acceso desactivado.';
      },
      error: err => {
        this.loading = false;
        this.publicationMessage = err?.error?.message || 'No se pudo guardar la publicación.';
      }
    });
  }

  getTypeText(type?: DocumentType | string | number): string {
    return DocumentService.getTypeText(type);
  }

  // ─── Toolbar ───────────────────────────────────────────────────────────────

  onEditorKeyDown(event: KeyboardEvent, editor: HTMLTextAreaElement): void {
    const mod = event.ctrlKey || event.metaKey;

    if (event.key === 'Tab') { event.preventDefault(); this.insertAtCursor(editor, '  '); return; }
    if (mod && event.key === 'b') { event.preventDefault(); this.applyBold(editor); return; }
    if (mod && event.key === 'i') { event.preventDefault(); this.applyItalic(editor); return; }
    if (mod && event.key === 's') { event.preventDefault(); this.saveDocument(); }
  }

  applyBold(editor: HTMLTextAreaElement): void { this.wrapSelection(editor, '**', '**', 'texto en negrita'); }
  applyItalic(editor: HTMLTextAreaElement): void { this.wrapSelection(editor, '*', '*', 'texto en cursiva'); }
  applyStrikethrough(editor: HTMLTextAreaElement): void { this.wrapSelection(editor, '~~', '~~', 'texto tachado'); }

  clearFormatting(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;

    let outerStart = start;
    while (outerStart > 0 && (value[outerStart - 1] === '*' || value[outerStart - 1] === '~')) outerStart--;
    let outerEnd = end;
    while (outerEnd < value.length && (value[outerEnd] === '*' || value[outerEnd] === '~')) outerEnd++;

    const region = value.slice(outerStart, outerEnd);
    const inner = region
      .replace(/^[*~]+|[*~]+$/g, '')
      .split('\n')
      .map(line => line.replace(/^#{1,6} /, '').replace(/^> /, '').replace(/^- /, '').replace(/^\d+\. /, ''))
      .join('\n');

    if (inner === region) return;
    this.setContentValue(value.slice(0, outerStart) + inner + value.slice(outerEnd));
    this.restoreSelection(editor, outerStart, outerStart + inner.length);
  }

  applyHeading(editor: HTMLTextAreaElement, level: 1 | 2 | 3): void {
    this.prefixSelectedLines(editor, () => '#'.repeat(level) + ' ');
  }

  applyBlockquote(editor: HTMLTextAreaElement): void { this.prefixSelectedLines(editor, () => '> '); }
  applyBulletList(editor: HTMLTextAreaElement): void { this.prefixSelectedLines(editor, () => '- '); }
  applyOrderedList(editor: HTMLTextAreaElement): void { this.prefixSelectedLines(editor, i => `${i + 1}. `); }

  insertLink(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'texto del enlace';
    const urlPlaceholder = 'https://ejemplo.com';
    const replacement = `[${selected}](${urlPlaceholder})`;
    this.setContentValue(value.slice(0, start) + replacement + value.slice(end));
    const urlStart = start + selected.length + 3;
    this.restoreSelection(editor, urlStart, urlStart + urlPlaceholder.length);
  }

  insertImage(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'descripción';
    const urlPlaceholder = 'https://url-de-imagen.com';
    const replacement = `![${selected}](${urlPlaceholder})`;
    this.setContentValue(value.slice(0, start) + replacement + value.slice(end));
    const urlStart = start + selected.length + 4;
    this.restoreSelection(editor, urlStart, urlStart + urlPlaceholder.length);
  }

  insertCodeBlock(editor: HTMLTextAreaElement): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || 'codigo';
    const replacement = `\n\`\`\`\n${selected}\n\`\`\`\n`;
    this.setContentValue(value.slice(0, start) + replacement + value.slice(end));
    this.restoreSelection(editor, start + 5, start + 5 + selected.length);
  }

  // ─── Private helpers ───────────────────────────────────────────────────────

  private wrapSelection(editor: HTMLTextAreaElement, before: string, after: string, placeholder: string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    const selected = value.slice(start, end);

    const innerWrapped =
      selected.startsWith(before) &&
      selected.endsWith(after) &&
      selected.length > before.length + after.length &&
      !selected.startsWith(before + before[0]);

    const outerStart = start - before.length;
    const outerEnd = end + after.length;
    const surroundedBy =
      outerStart >= 0 &&
      value.slice(outerStart, start) === before &&
      value.slice(end, outerEnd) === after &&
      (before.length === 1 ? value[outerStart - 1] !== before[0] : true) &&
      (after.length === 1 ? value[outerEnd] !== after[0] : true);

    if (innerWrapped) {
      const unwrapped = selected.slice(before.length, selected.length - after.length);
      this.setContentValue(value.slice(0, start) + unwrapped + value.slice(end));
      this.restoreSelection(editor, start, start + unwrapped.length);
      return;
    }

    if (surroundedBy) {
      this.setContentValue(value.slice(0, outerStart) + selected + value.slice(outerEnd));
      this.restoreSelection(editor, outerStart, outerStart + selected.length);
      return;
    }

    const text = selected || placeholder;
    const replacement = `${before}${text}${after}`;
    this.setContentValue(value.slice(0, start) + replacement + value.slice(end));
    const selStart = start + before.length;
    this.restoreSelection(editor, selStart, selStart + text.length);
  }

  private prefixSelectedLines(editor: HTMLTextAreaElement, getPrefix: (i: number) => string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;

    const blockStart = value.lastIndexOf('\n', Math.max(0, start - 1)) + 1;
    const nextBreak = value.indexOf('\n', end);
    const blockEnd = nextBreak === -1 ? value.length : nextBreak;

    const lines = value.slice(blockStart, blockEnd).split('\n');
    const allPrefixed = lines.every((line, i) => !line.trim() || line.startsWith(getPrefix(i)));

    const processedBlock = lines
      .map((line, i) => {
        if (!line.trim()) return line;
        const prefix = getPrefix(i);
        return allPrefixed ? line.slice(prefix.length) : `${prefix}${line}`;
      })
      .join('\n');

    this.setContentValue(value.slice(0, blockStart) + processedBlock + value.slice(blockEnd));
    this.restoreSelection(editor, blockStart, blockStart + processedBlock.length);
  }

  private insertAtCursor(editor: HTMLTextAreaElement, text: string): void {
    const value = this.getContentValue();
    const start = editor.selectionStart ?? value.length;
    const end = editor.selectionEnd ?? value.length;
    this.setContentValue(value.slice(0, start) + text + value.slice(end));
    this.restoreSelection(editor, start + text.length, start + text.length);
  }

  private getContentValue(): string {
    return String(this.editForm.get('content')?.value || '');
  }

  private setContentValue(value: string): void {
    const control = this.editForm.get('content');
    if (!control) return;
    control.setValue(value);
    control.markAsDirty();
    control.markAsTouched();
  }

  private restoreSelection(editor: HTMLTextAreaElement, start: number, end: number): void {
    requestAnimationFrame(() => {
      editor.focus();
      editor.setSelectionRange(start, end);
    });
  }

  private updateRenderedPreview(): void {
    const content = String(this.editForm.get('content')?.value || '').trim();
    if (!content) {
      this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, '<p>(sin contenido)</p>') || '';
      return;
    }
    this.renderedPreview = this.sanitizer.sanitize(SecurityContext.HTML, this.renderMarkdownToHtml(content)) || '';
  }

  private renderMarkdownToHtml(markdown: string): string {
    const rendered = marked.parse(markdown, { async: false, breaks: true, gfm: true, renderer: this.markdownRenderer });
    if (typeof rendered !== 'string') return '';

    const withoutJs = rendered.replace(/(<a\b[^>]*\bhref\s*=\s*["'])\s*javascript:[^"']*(["'][^>]*>)/gi, '$1#$2');

    return withoutJs
      .replace(/<li>\s*<input\s[^>]*\bchecked\b[^>]*>\s*/gi, '<li class="task-item"><span class="task-box" aria-label="completada">☑</span> ')
      .replace(/<li>\s*<input\s[^>]*type="checkbox"[^>]*>\s*/gi, '<li class="task-item"><span class="task-box" aria-label="pendiente">☐</span> ');
  }

  private isDomainAllowed(url: string): boolean {
    try {
      const { hostname } = new URL(url);
      return this.allowedEmbedDomains.some(d => hostname === d || hostname.endsWith('.' + d));
    } catch { return false; }
  }

  private escapeHtml(value: string): string {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private setDocumentState(document: Document, loadRelated = true): void {
    this.document = document;
    this.publicUrl = `${window.location.origin}/public/documents/${document.id}`;
    this.setHeaderState(document);
    this.editForm.patchValue({
      title: document.title,
      description: document.description,
      content: document.content
    });
    this.updateRenderedPreview();

    if (loadRelated) {
      this.loadVersions(document.id);
      this.loadPublication(document.id);
    }
  }

  private setHeaderState(document: Document): void {
    this.layoutHeaderStateService.setOverride({
      title: document.title,
      description: document.description?.trim() || 'Edición, versiones y publicación del documento',
      meta: [
        this.getTypeText(document.type),
        `Guardado ${new Intl.DateTimeFormat('es-ES', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(document.updatedAt))}`,
        ...(document.isPublic ? ['Publicado'] : [])
      ],
      actions: [
        {
          label: 'Publicación',
          variant: 'ghost',
          action: () => { this.showPublicationModal = true; }
        },
        {
          label: 'Volver a documentos',
          variant: 'secondary',
          route: '/documents'
        }
      ]
    });
  }

  private refreshDocument(): void {
    if (!this.document) return;
    this.documentService.getDocument(this.document.id).subscribe({
      next: (doc) => { this.setDocumentState(doc, false); this.loadVersions(doc.id); this.loading = false; },
      error: () => { this.toastService.error('La versión se restauró, pero no se pudo refrescar el documento.'); this.loading = false; }
    });
  }

  private loadVersions(documentId: number): void {
    this.documentVersionService.getDocumentVersions(documentId).subscribe({
      next: (versions) => { this.versions = versions; this.loading = false; },
      error: () => { this.versions = []; this.loading = false; }
    });
  }

  private loadPublication(documentId: number): void {
    this.publication = null;
    this.publicationMessage = '';

    this.publicationService.getPublication(documentId).subscribe({
      next: publication => {
        this.publication = publication;
        this.publicationForm.patchValue({
          isPublic: publication.isPublic,
          publicTitle: publication.publicTitle || '',
          publicDescription: publication.publicDescription || '',
          author: publication.author || this.currentUserName || ''
        });
      },
      error: () => { this.publication = null; }
    });
  }

}
