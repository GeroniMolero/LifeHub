import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { Document, DocumentType } from '../../../../models/document.model';

@Component({
  selector: 'app-space-documents-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './space-documents-sidebar.component.html',
  styleUrls: ['./space-documents-sidebar.component.scss']
})
export class SpaceDocumentsSidebarComponent {
  @Input({ required: true }) loadingDocuments = false;
  @Input({ required: true }) documents: Document[] = [];
  @Input() selectedDocument: Document | null = null;

  @Output() openCreateDocument = new EventEmitter<void>();
  @Output() selectDocument = new EventEmitter<Document>();
  @Output() deselectDocument = new EventEmitter<void>();

  searchQuery = '';

  onDocumentClick(doc: Document): void {
    if (doc.id === this.selectedDocument?.id) {
      this.deselectDocument.emit();
    } else {
      this.selectDocument.emit(doc);
    }
  }

  get filteredDocuments(): Document[] {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) return this.documents;
    return this.documents.filter(d => d.title.toLowerCase().includes(q));
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
