import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { Document, DocumentType } from '../../../models/document.model';
import { User } from '../../../models/auth.model';
import { AuthService } from '../../../services/auth.service';
import { ConfirmationService } from '../../../services/confirmation.service';
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
  filterForm!: FormGroup;
  canFilterByCreator = false;
  showFiltersDropdown = false;
  loading = false;
  error = '';
  showForm = false;
  readonly DocumentType = DocumentType;
  readonly pageSizeOptions = [5, 10, 20, 50];
  pageSize = 5;
  pageSizeControl = new FormControl<number>(5, { nonNullable: true });
  currentPage = 1;
  private filterChangesSub?: Subscription;
  private userChangesSub?: Subscription;
  private currentUserId: string | null = null;
  private currentUserDisplayName = 'Usuario actual';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private confirmationService: ConfirmationService,
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

    this.filterForm = this.fb.group({
      search: [''],
      creatorName: [''],
      type: ['all'],
      publication: ['all']
    });

    this.filterChangesSub = this.filterForm.valueChanges.subscribe(() => {
      this.currentPage = 1;
    });

    this.userChangesSub = this.authService.getCurrentUser().subscribe((user) => {
      this.currentUserId = user?.id ?? null;
      this.currentUserDisplayName = this.resolveUserDisplayName(user);
      this.canFilterByCreator = this.hasViewAllDocumentsPermission(user);

      if (!this.canFilterByCreator && this.filterForm.get('creatorName')?.value) {
        this.filterForm.patchValue({ creatorName: '' }, { emitEvent: false });
      }
    });

    this.pageSizeControl.valueChanges.subscribe((size) => {
      const parsed = Number(size);
      if (!Number.isFinite(parsed) || parsed <= 0) {
        return;
      }

      this.pageSize = parsed;
      this.currentPage = 1;
    });

    this.setHeaderState();
    this.loadDocuments();
  }

  ngOnDestroy(): void {
    this.filterChangesSub?.unsubscribe();
    this.userChangesSub?.unsubscribe();
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
    if (!this.confirmationService.confirmDelete('este documento')) {
      return;
    }

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

  get filteredDocuments(): Document[] {
    const search = String(this.filterForm?.get('search')?.value ?? '').trim().toLowerCase();
    const creatorName = String(this.filterForm?.get('creatorName')?.value ?? '').trim().toLowerCase();
    const type = String(this.filterForm?.get('type')?.value ?? 'all');
    const publication = String(this.filterForm?.get('publication')?.value ?? 'all');

    return [...this.documents]
      .filter((doc) => {
        if (type !== 'all' && String(doc.type) !== type) {
          return false;
        }

        if (publication === 'public' && !doc.isPublic) {
          return false;
        }

        if (publication === 'private' && doc.isPublic) {
          return false;
        }

        if (this.canFilterByCreator && creatorName) {
          const creatorValue = this.getCreatorName(doc).toLowerCase();
          if (!creatorValue.includes(creatorName)) {
            return false;
          }
        }

        if (!search) {
          return true;
        }

        const searchableFields = [doc.title, doc.description, doc.content]
          .filter(Boolean)
          .join(' ')
          .toLowerCase();

        return searchableFields.includes(search);
      })
      .sort((left, right) => {
        return new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime();
      });
  }

  get paginatedDocuments(): Document[] {
    const start = (this.normalizedCurrentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filteredDocuments.slice(start, end);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredDocuments.length / this.pageSize));
  }

  get normalizedCurrentPage(): number {
    return Math.min(Math.max(this.currentPage, 1), this.totalPages);
  }

  get startItemIndex(): number {
    if (this.filteredDocuments.length === 0) {
      return 0;
    }
    return (this.normalizedCurrentPage - 1) * this.pageSize + 1;
  }

  get endItemIndex(): number {
    return Math.min(this.normalizedCurrentPage * this.pageSize, this.filteredDocuments.length);
  }

  get hasActiveFilters(): boolean {
    if (!this.filterForm) {
      return false;
    }

    const { search, creatorName, type, publication } = this.filterForm.getRawValue();
    return !!String(search ?? '').trim()
      || (this.canFilterByCreator && !!String(creatorName ?? '').trim())
      || type !== 'all'
      || publication !== 'all';
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      creatorName: '',
      type: 'all',
      publication: 'all'
    });
    this.currentPage = 1;
  }

  toggleFiltersDropdown(): void {
    this.showFiltersDropdown = !this.showFiltersDropdown;
  }

  closeFiltersDropdown(): void {
    this.showFiltersDropdown = false;
  }

  goToPreviousPage(): void {
    if (this.normalizedCurrentPage > 1) {
      this.currentPage = this.normalizedCurrentPage - 1;
    }
  }

  goToNextPage(): void {
    if (this.normalizedCurrentPage < this.totalPages) {
      this.currentPage = this.normalizedCurrentPage + 1;
    }
  }

  getPublicationText(isPublic?: boolean): string {
    return isPublic ? 'Publicado' : 'Privado';
  }

  getCreatorName(doc: Document): string {
    const creatorName = doc.creatorName?.trim();
    if (creatorName) {
      return creatorName;
    }

    const userId = doc.userId;
    if (!userId) {
      return 'Sin usuario';
    }

    if (this.currentUserId && userId === this.currentUserId) {
      return this.currentUserDisplayName;
    }

    return userId;
  }

  private resolveUserDisplayName(user: User | null): string {
    if (!user) {
      return 'Usuario actual';
    }

    const fullName = user.fullName?.trim();
    if (fullName) {
      return fullName;
    }

    return user.email || user.id;
  }

  private hasViewAllDocumentsPermission(user: User | null): boolean {
    if (!user?.claims?.length) {
      return false;
    }

    return user.claims.includes('permission:documents.view.all');
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
