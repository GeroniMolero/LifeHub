import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

import { Document, DocumentType } from '../../../../models/document.model';
import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

interface VisualMediaLayout {
  x: number;
  y: number;
  width: number;
  zIndex: number;
}

@Component({
  selector: 'app-space-editor-main',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './space-editor-main.component.html',
  styleUrls: ['./space-editor-main.component.scss']
})
export class SpaceEditorMainComponent {
  @Input({ required: true }) selectedDocument: Document | null = null;
  @Input({ required: true }) editDocumentForm!: FormGroup;
  @Input({ required: true }) activeTab: 'code' | 'preview' = 'code';
  @Input({ required: true }) renderedPreview = '';
  @Input({ required: true }) loadingDocuments = false;
  @Input({ required: true }) activeVisualMediaReferences: SpaceMediaReference[] = [];
  @Input() localFileBlobUrls: Map<string, string> = new Map();
  @Input() visualLayouts: Map<string, VisualMediaLayout> = new Map();

  @Output() setActiveTab = new EventEmitter<'code' | 'preview'>();
  @Output() saveDocument = new EventEmitter<void>();
  @Output() deleteDocument = new EventEmitter<number>();
  @Output() startDraggingMedia = new EventEmitter<{ event: PointerEvent; id: string }>();

  private trustedEmbedCache = new Map<string, SafeResourceUrl>();

  constructor(private sanitizer: DomSanitizer) {}

  isEmbedReference(item: SpaceMediaReference): boolean {
    return item.type === 'external-embed' && !!item.embedUrl;
  }

  isVideoReference(item: SpaceMediaReference): boolean {
    return item.type === 'local-session-file' && !!item.mimeType?.startsWith('video/');
  }

  isImageReference(item: SpaceMediaReference): boolean {
    return item.type === 'local-session-file' && !!item.mimeType?.startsWith('image/');
  }

  getLocalMediaUrl(id: string): string | null {
    return this.localFileBlobUrls.get(id) ?? null;
  }

  toTrustedResource(url: string): SafeResourceUrl {
    if (this.trustedEmbedCache.has(url)) {
      return this.trustedEmbedCache.get(url)!;
    }
    const value = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    this.trustedEmbedCache.set(url, value);
    return value;
  }

  getVisualStyle(id: string): { [key: string]: string } {
    const layout = this.visualLayouts.get(id);
    if (!layout) {
      return { left: '0px', top: '0px', width: '300px', zIndex: '1' };
    }
    return {
      left: `${layout.x}px`,
      top: `${layout.y}px`,
      width: `${layout.width}px`,
      zIndex: `${layout.zIndex}`
    };
  }

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }
}
