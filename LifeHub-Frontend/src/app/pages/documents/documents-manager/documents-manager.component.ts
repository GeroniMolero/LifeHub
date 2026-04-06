import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Document, DocumentType } from '../../../models/document.model';
import { DocumentService } from '../../../services/document.service';
import { LayoutHeaderStateService } from '../../../services/layout-header-state.service';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './documents-manager.component.html',
  styleUrls: ['./documents-manager.component.scss']
})
export class DocumentsComponent implements OnInit, OnDestroy {
  documents: Document[] = [];
  createForm!: FormGroup;
  loading = false;
  error = '';
  showForm = false;

  constructor(
    private fb: FormBuilder,
    private documentService: DocumentService,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.createForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      content: [''],
      type: [DocumentType.Note]
    });

    this.setHeaderState();
    this.loadDocuments();
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
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
        this.setHeaderState();
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo crear el documento.';
        this.loading = false;
      }
    });
  }

  deleteDocument(id: number): void {
    this.loading = true;
    this.error = '';
    this.documentService.deleteDocument(id).subscribe({
      next: () => {
        this.documents = this.documents.filter(doc => doc.id !== id);
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
    this.setHeaderState();
  }

  closeForm(): void {
    this.showForm = false;
    this.setHeaderState();
  }

  getTypeText(type?: DocumentType | string | number): string {
    const typeMap: { [key: number]: string } = {
      [DocumentType.Note]: 'Nota',
      [DocumentType.TextFile]: 'Archivo de texto',
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

  private setHeaderState(): void {
    this.layoutHeaderStateService.setOverride({
      actions: [
        {
          label: this.showForm ? 'Cancelar' : 'Nuevo documento',
          variant: this.showForm ? 'secondary' : 'primary',
          action: () => this.toggleForm()
        }
      ]
    });
  }
}
