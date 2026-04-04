import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

@Component({
  selector: 'app-space-media-sidebar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './space-media-sidebar.component.html',
  styleUrls: ['./space-media-sidebar.component.scss']
})
export class SpaceMediaSidebarComponent {
  @Input({ required: true }) showCreateMedia = false;
  @Input({ required: true }) mediaTab: 'embed' | 'local' = 'embed';
  @Input({ required: true }) createEmbedForm!: FormGroup;
  @Input({ required: true }) localFileLabelControl!: FormControl<string>;
  @Input({ required: true }) selectedLocalFile: File | null = null;
  @Input({ required: true }) allowedEmbedDomains: string[] = [];
  @Input({ required: true }) mediaError = '';
  @Input({ required: true }) loadingMedia = false;
  @Input({ required: true }) mediaReferences: SpaceMediaReference[] = [];
  @Input({ required: true }) audioMediaReferences: SpaceMediaReference[] = [];
  @Input() activeVisualMediaIds: Set<string> = new Set();
  @Input() localFileBlobUrls: Map<string, string> = new Map();

  @Output() toggleCreateMedia = new EventEmitter<void>();
  @Output() setMediaTab = new EventEmitter<'embed' | 'local'>();
  @Output() addEmbedReference = new EventEmitter<void>();
  @Output() onLocalFileSelected = new EventEmitter<Event>();
  @Output() addLocalFileReference = new EventEmitter<void>();
  @Output() onMediaReferenceClick = new EventEmitter<SpaceMediaReference>();
  @Output() removeMediaReference = new EventEmitter<string>();

  isMediaActiveInMain(id: string): boolean {
    return this.activeVisualMediaIds.has(id);
  }

  getLocalMediaUrl(id: string): string | null {
    return this.localFileBlobUrls.get(id) ?? null;
  }
}
