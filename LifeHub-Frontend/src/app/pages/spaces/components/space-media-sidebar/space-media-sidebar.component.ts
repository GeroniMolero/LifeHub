import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

@Component({
  selector: 'app-space-media-sidebar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './space-media-sidebar.component.html',
  styleUrls: ['./space-media-sidebar.component.scss']
})
export class SpaceMediaSidebarComponent {
  @Input({ required: true }) loadingMedia = false;
  @Input({ required: true }) mediaReferences: SpaceMediaReference[] = [];
  @Input({ required: true }) audioMediaReferences: SpaceMediaReference[] = [];
  @Input() activeVisualMediaIds: Set<string> = new Set();
  @Input() localFileBlobUrls: Map<string, string> = new Map();

  @Output() openCreateMedia = new EventEmitter<void>();
  @Output() onMediaReferenceClick = new EventEmitter<SpaceMediaReference>();
  @Output() removeMediaReference = new EventEmitter<string>();

  isMediaActiveInMain(id: string): boolean {
    return this.activeVisualMediaIds.has(id);
  }

  getLocalMediaUrl(id: string): string | null {
    return this.localFileBlobUrls.get(id) ?? null;
  }

  isAudioItem(item: SpaceMediaReference): boolean {
    return this.audioMediaReferences.some(a => a.id === item.id);
  }

  get visualMediaReferences(): SpaceMediaReference[] {
    return this.mediaReferences.filter(item => !this.isAudioItem(item));
  }

  mediaTypeLabel(item: SpaceMediaReference): string {
    if (item.type === 'external-embed') return item.provider || 'Enlace';
    if (item.mimeType?.startsWith('video/')) return 'Vídeo';
    if (item.mimeType?.startsWith('image/')) return 'Imagen';
    return 'Local';
  }
}
