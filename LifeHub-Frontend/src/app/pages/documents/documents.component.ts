import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Document, DocumentType } from '../../models/document.model';
import { DocumentService } from '../../services/document.service';
import { DocumentVersion } from '../../models/document-version.model';
import { DocumentVersionService } from '../../services/document-version.service';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss']
})
export class DocumentsComponent implements OnInit {
  documents: Document[] = [];
  createForm!: FormGroup;
  editForm!: FormGroup;
  loading = false;
  error = '';
  showForm = false;
  selectedDocument: Document | null = null;
  versions: DocumentVersion[] = [];
  versionNote = '';

  constructor(
    private fb: FormBuilder,
    private documentService: DocumentService,
    private documentVersionService: DocumentVersionService
  ) {}

  ngOnInit(): void {
    this.createForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: [''],
      type: [DocumentType.Note]
    });

    this.editForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: ['']
    });

    this.loadDocuments();
  }

  onCreate(): void {
    if (this.createForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.documentService.createDocument(this.createForm.value).subscribe({
      next: (createdDocument) => {
        this.documents = [createdDocument, ...this.documents];
        this.createForm.reset({
          title: '',
          description: '',
          content: '',
          type: DocumentType.Note
        });
        this.showForm = false;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo crear el documento.';
        this.loading = false;
      }
    });
  }

  onEdit(): void {
    if (!this.selectedDocument || this.editForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.documentService.updateDocument(this.selectedDocument.id, this.editForm.value).subscribe({
      next: (updatedDocument) => {
        this.documents = this.documents.map(doc =>
          doc.id === updatedDocument.id ? updatedDocument : doc
        );
        this.selectedDocument = updatedDocument;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo actualizar el documento.';
        this.loading = false;
      }
    });
  }

  selectDocument(doc: Document): void {
    this.selectedDocument = doc;
    this.editForm.patchValue(doc);
    this.loadVersions(doc.id);
  }

  deleteDocument(id: number): void {
    this.loading = true;
    this.error = '';
    this.documentService.deleteDocument(id).subscribe({
      next: () => {
        this.documents = this.documents.filter(doc => doc.id !== id);
        if (this.selectedDocument?.id === id) {
          this.selectedDocument = null;
        }
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo eliminar el documento.';
        this.loading = false;
      }
    });
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
  }

  closeForm(): void {
    this.showForm = false;
    this.selectedDocument = null;
    this.versions = [];
    this.versionNote = '';
  }

  createVersion(): void {
    if (!this.selectedDocument) return;

    this.loading = true;
    this.error = '';
    this.documentVersionService.createSnapshot(this.selectedDocument.id, { note: this.versionNote || undefined }).subscribe({
      next: () => {
        this.versionNote = '';
        this.loadVersions(this.selectedDocument!.id);
      },
      error: () => {
        this.error = 'No se pudo crear la versión del documento.';
        this.loading = false;
      }
    });
  }

  restoreVersion(versionId: number): void {
    if (!this.selectedDocument) return;
    if (!confirm('¿Seguro que quieres restaurar esta versión? Se sobrescribirá el contenido actual.')) return;

    this.loading = true;
    this.error = '';
    this.documentVersionService.restoreVersion(versionId).subscribe({
      next: () => {
        this.documentService.getDocument(this.selectedDocument!.id).subscribe({
          next: (updatedDoc) => {
            this.documents = this.documents.map(doc => doc.id === updatedDoc.id ? updatedDoc : doc);
            this.selectedDocument = updatedDoc;
            this.editForm.patchValue(updatedDoc);
            this.loadVersions(updatedDoc.id);
          },
          error: () => {
            this.error = 'La versión se restauró, pero no se pudo refrescar el documento.';
            this.loading = false;
          }
        });
      },
      error: () => {
        this.error = 'No se pudo restaurar la versión.';
        this.loading = false;
      }
    });
  }

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de Texto',
      [DocumentType.List]: 'Lista'
    };
    return type !== undefined ? (typeMap[Number(type)] || 'Nota') : 'Nota';
  }

  private loadDocuments(): void {
    this.loading = true;
    this.error = '';
    this.documentService.getDocuments().subscribe({
      next: (documents) => {
        this.documents = documents;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudieron cargar los documentos.';
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
}
