import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { Document, DocumentType } from '../../../../models/document.model';

@Component({
  selector: 'app-space-documents-sidebar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './space-documents-sidebar.component.html',
  styleUrls: ['./space-documents-sidebar.component.scss']
})
export class SpaceDocumentsSidebarComponent {
  @Input({ required: true }) showCreateDocument = false;
  @Input({ required: true }) createDocumentForm!: FormGroup;
  @Input({ required: true }) loadingDocuments = false;
  @Input({ required: true }) documents: Document[] = [];
  @Input() selectedDocument: Document | null = null;

  @Output() toggleCreateDocument = new EventEmitter<void>();
  @Output() createDocument = new EventEmitter<void>();
  @Output() selectDocument = new EventEmitter<Document>();

  readonly DocumentType = DocumentType;

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }
}
