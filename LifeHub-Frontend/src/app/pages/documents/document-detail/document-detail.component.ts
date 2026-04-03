import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';

import { Document, DocumentType } from '../../../models/document.model';
import { DocumentVersion } from '../../../models/document-version.model';
import { DocumentPublication, MediaReference } from '../../../models/document-publication.model';
import { SpaceMediaReference } from '../../../models/space-media-reference.model';
import { DocumentService } from '../../../services/document.service';
import { DocumentVersionService } from '../../../services/document-version.service';
import { DocumentPublicationService } from '../../../services/document-publication.service';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';
import { SpaceMediaSessionService } from '../../../services/space-media-session.service';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './document-detail.component.html',
  styleUrls: ['./document-detail.component.scss']
})
export class DocumentDetailComponent implements OnInit, OnDestroy {
  document: Document | null = null;
  editForm!: FormGroup;
  publicationForm!: FormGroup;
  versions: DocumentVersion[] = [];
  versionNote = '';
  publication: DocumentPublication | null = null;
  publicationLinksText = '';
  publicationMessage = '';
  publicUrl = '';
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private documentService: DocumentService,
    private documentVersionService: DocumentVersionService,
    private publicationService: DocumentPublicationService,
    private layoutHeaderStateService: LayoutHeaderStateService,
    private mediaSessionService: SpaceMediaSessionService
  ) {}

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
      mediaReferences: [[]]
    });

    this.route.data.subscribe({
      next: (data) => {
        const document = data['document'] as Document;
        this.setDocumentState(document);
      },
      error: () => {
        this.error = 'No se pudo cargar el documento.';
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  get backLink(): string[] {
    return this.document?.creativeSpaceId
      ? ['/spaces', String(this.document.creativeSpaceId)]
      : ['/documents'];
  }

  get backLabel(): string {
    return this.document?.creativeSpaceId ? 'Volver al espacio' : 'Volver a documentos';
  }

  saveDocument(): void {
    if (!this.document || this.editForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.documentService.updateDocument(this.document.id, this.editForm.value).subscribe({
      next: (updatedDocument) => {
        this.setDocumentState(updatedDocument, false);
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo actualizar el documento.';
        this.loading = false;
      }
    });
  }

  deleteDocument(): void {
    if (!this.document) return;
    if (!confirm('¿Seguro que quieres eliminar este documento?')) return;

    this.loading = true;
    this.error = '';
    this.documentService.deleteDocument(this.document.id).subscribe({
      next: () => {
        const target = this.document?.creativeSpaceId
          ? ['/spaces', String(this.document.creativeSpaceId)]
          : ['/documents'];
        this.router.navigate(target);
      },
      error: () => {
        this.error = 'No se pudo eliminar el documento.';
        this.loading = false;
      }
    });
  }

  createVersion(): void {
    if (!this.document) return;

    this.loading = true;
    this.error = '';
    this.documentVersionService.createSnapshot(this.document.id, { note: this.versionNote || undefined }).subscribe({
      next: () => {
        this.versionNote = '';
        this.loadVersions(this.document!.id);
      },
      error: () => {
        this.error = 'No se pudo crear la versión del documento.';
        this.loading = false;
      }
    });
  }

  restoreVersion(versionId: number): void {
    if (!this.document) return;
    if (!confirm('¿Seguro que quieres restaurar esta versión? Se sobrescribirá el contenido actual.')) return;

    this.loading = true;
    this.error = '';
    this.documentVersionService.restoreVersion(versionId).subscribe({
      next: () => {
        this.refreshDocument();
      },
      error: () => {
        this.error = 'No se pudo restaurar la versión.';
        this.loading = false;
      }
    });
  }

  copyPublicUrl(): void {
    if (!this.publicUrl) return;
    navigator.clipboard.writeText(this.publicUrl);
    this.publicationMessage = 'Enlace público copiado al portapapeles.';
  }

  loadSpaceMediaReferences(): void {
    if (!this.document?.creativeSpaceId) {
      this.publicationMessage = 'Este documento no está asociado a un espacio con referencias multimedia.';
      return;
    }

    const refs = this.mediaSessionService.getReferences(Number(this.document.creativeSpaceId));
    const publicationRefs = refs.map(ref => this.toPublicationMediaReference(ref));
    this.publicationForm.patchValue({ mediaReferences: publicationRefs });
    this.publicationMessage = `${publicationRefs.length} referencia(s) multimedia cargadas desde la sesión del espacio.`;
  }

  savePublication(): void {
    if (!this.document) return;

    this.loading = true;
    this.publicationMessage = '';

    const externalLinks = this.publicationLinksText
      .split('\n')
      .map(link => link.trim())
      .filter(link => !!link);

    this.publicationService.upsertPublication(this.document.id, {
      isPublic: Boolean(this.publicationForm.value.isPublic),
      publicTitle: this.publicationForm.value.publicTitle || undefined,
      publicDescription: this.publicationForm.value.publicDescription || undefined,
      mediaReferences: (this.publicationForm.value.mediaReferences || []) as MediaReference[],
      externalLinks
    }).subscribe({
      next: publication => {
        this.publication = publication;
        this.document = {
          ...this.document!,
          isPublic: publication.isPublic,
          publishedAt: publication.publishedAt as any
        };
        this.loading = false;
        this.publicationMessage = publication.isPublic
          ? 'Documento publicado correctamente.'
          : 'Documento despublicado correctamente.';
      },
      error: err => {
        this.loading = false;
        this.publicationMessage = err?.error?.message || 'No se pudo guardar la publicación.';
      }
    });
  }

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };

    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }

  private setDocumentState(document: Document, loadRelated = true): void {
    this.document = document;
    this.publicUrl = `${window.location.origin}/public/documents/${document.id}`;
    this.layoutHeaderStateService.setOverride({
      title: `${document.title}`,
      description: document.description?.trim() || 'Edición, versiones y publicación del documento'
    });
    this.editForm.patchValue({
      title: document.title,
      description: document.description,
      content: document.content
    });

    if (loadRelated) {
      this.loadVersions(document.id);
      this.loadPublication(document.id);
    }
  }

  private refreshDocument(): void {
    if (!this.document) return;

    this.documentService.getDocument(this.document.id).subscribe({
      next: (updatedDocument) => {
        this.setDocumentState(updatedDocument, false);
        this.loadVersions(updatedDocument.id);
        this.loading = false;
      },
      error: () => {
        this.error = 'La versión se restauró, pero no se pudo refrescar el documento.';
        this.loading = false;
      }
    });
  }

  private loadVersions(documentId: number): void {
    this.documentVersionService.getDocumentVersions(documentId).subscribe({
      next: (versions) => {
        this.versions = versions;
        this.loading = false;
      },
      error: () => {
        this.versions = [];
        this.loading = false;
      }
    });
  }

  private loadPublication(documentId: number): void {
    this.publication = null;
    this.publicationLinksText = '';
    this.publicationMessage = '';

    this.publicationService.getPublication(documentId).subscribe({
      next: publication => {
        this.publication = publication;
        this.publicationForm.patchValue({
          isPublic: publication.isPublic,
          publicTitle: publication.publicTitle || '',
          publicDescription: publication.publicDescription || '',
          mediaReferences: publication.mediaReferences || []
        });
        this.publicationLinksText = (publication.externalLinks || []).join('\n');
      },
      error: () => {
        this.publication = null;
      }
    });
  }

  private toPublicationMediaReference(reference: SpaceMediaReference): MediaReference {
    return {
      type: reference.type,
      label: reference.label,
      source: reference.source,
      provider: reference.provider,
      embedUrl: reference.embedUrl
    };
  }
}