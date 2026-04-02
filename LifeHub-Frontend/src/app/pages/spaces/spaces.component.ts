import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CreativeSpace, SpacePrivacy } from '../../models/creative-space.model';
import { CreativeSpaceService } from '../../services/creative-space.service';

@Component({
  selector: 'app-spaces',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="spaces-container">
      <div class="header-row">
        <div>
          <h1>Espacios creativos</h1>
          <p>Organiza tus ideas en espacios privados o compartidos.</p>
        </div>
        <button class="btn-primary" (click)="toggleCreate()">{{ showCreate ? 'Cancelar' : 'Nuevo espacio' }}</button>
      </div>

      <div *ngIf="showCreate" class="card form-card">
        <h2>Crear espacio</h2>
        <form [formGroup]="createForm" (ngSubmit)="createSpace()">
          <label>Nombre</label>
          <input type="text" formControlName="name" />

          <label>Descripción</label>
          <textarea rows="3" formControlName="description"></textarea>

          <label>Privacidad</label>
          <select formControlName="privacy">
            <option [value]="SpacePrivacy.Private">Privado</option>
            <option [value]="SpacePrivacy.Shared">Compartido</option>
          </select>

          <label class="checkbox">
            <input type="checkbox" formControlName="isPublicProfileVisible" />
            Visible en perfil público
          </label>

          <button class="btn-primary" type="submit" [disabled]="createForm.invalid || loading">Crear</button>
        </form>
      </div>

      <div *ngIf="error" class="error">{{ error }}</div>
      <div *ngIf="loading" class="loading">Cargando...</div>

      <div *ngIf="!loading && spaces.length === 0" class="empty">
        Aún no tienes espacios creativos.
      </div>

      <div class="grid" *ngIf="!loading && spaces.length > 0">
        <article class="card" *ngFor="let space of spaces">
          <h3>{{ space.name }}</h3>
          <p>{{ space.description || 'Sin descripción' }}</p>
          <small>{{ privacyText(space.privacy) }} · {{ space.updatedAt | date:'short' }}</small>

          <div class="actions">
            <button class="btn-secondary" (click)="startEdit(space)">Editar</button>
            <button class="btn-danger" (click)="deleteSpace(space.id)">Eliminar</button>
            <a class="btn-primary" routerLink="/documents">Abrir documentos</a>
          </div>

          <form *ngIf="editingId === space.id" [formGroup]="editForm" (ngSubmit)="saveEdit(space.id)" class="edit-form">
            <label>Nombre</label>
            <input type="text" formControlName="name" />

            <label>Descripción</label>
            <textarea rows="3" formControlName="description"></textarea>

            <label>Privacidad</label>
            <select formControlName="privacy">
              <option [value]="SpacePrivacy.Private">Privado</option>
              <option [value]="SpacePrivacy.Shared">Compartido</option>
            </select>

            <label class="checkbox">
              <input type="checkbox" formControlName="isPublicProfileVisible" />
              Visible en perfil público
            </label>

            <div class="actions">
              <button class="btn-primary" type="submit" [disabled]="editForm.invalid || loading">Guardar</button>
              <button class="btn-secondary" type="button" (click)="cancelEdit()">Cancelar</button>
            </div>
          </form>
        </article>
      </div>
    </div>
  `,
  styles: [`
    .spaces-container { max-width: 980px; margin: 0 auto; }
    .header-row { display: flex; justify-content: space-between; gap: 1rem; align-items: center; margin-bottom: 1rem; }
    .grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); }
    .card { border: 1px solid #e3e3e3; border-radius: 12px; padding: 1rem; background: #fff; }
    .form-card form, .edit-form { display: grid; gap: 0.5rem; }
    input, textarea, select { width: 100%; padding: 0.5rem; border: 1px solid #ccc; border-radius: 6px; }
    .checkbox { display: flex; gap: 0.5rem; align-items: center; margin-top: 0.25rem; }
    .checkbox input { width: auto; }
    .actions { display: flex; gap: 0.5rem; margin-top: 0.75rem; flex-wrap: wrap; }
    .btn-primary, .btn-secondary, .btn-danger { border: 0; border-radius: 6px; padding: 0.45rem 0.75rem; cursor: pointer; text-decoration: none; }
    .btn-primary { background: #1f7a8c; color: #fff; }
    .btn-secondary { background: #e5e7eb; color: #111827; }
    .btn-danger { background: #b91c1c; color: #fff; }
    .error { color: #b91c1c; margin: 0.5rem 0; }
    .loading, .empty { margin: 0.75rem 0; color: #374151; }
  `]
})
export class SpacesComponent implements OnInit {
  spaces: CreativeSpace[] = [];
  createForm!: FormGroup;
  editForm!: FormGroup;
  editingId: number | null = null;
  loading = false;
  error = '';
  showCreate = false;
  SpacePrivacy = SpacePrivacy;

  constructor(
    private fb: FormBuilder,
    private creativeSpaceService: CreativeSpaceService
  ) {}

  ngOnInit(): void {
    this.createForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      privacy: [SpacePrivacy.Private],
      isPublicProfileVisible: [false]
    });

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      privacy: [SpacePrivacy.Private],
      isPublicProfileVisible: [false]
    });

    this.loadSpaces();
  }

  toggleCreate(): void {
    this.showCreate = !this.showCreate;
  }

  createSpace(): void {
    if (this.createForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.createSpace(this.createForm.value).subscribe({
      next: (space) => {
        this.spaces = [space, ...this.spaces];
        this.showCreate = false;
        this.createForm.reset({
          name: '',
          description: '',
          privacy: SpacePrivacy.Private,
          isPublicProfileVisible: false
        });
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo crear el espacio.';
        this.loading = false;
      }
    });
  }

  startEdit(space: CreativeSpace): void {
    this.editingId = space.id;
    this.editForm.patchValue(space);
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(spaceId: number): void {
    if (this.editForm.invalid) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.updateSpace(spaceId, this.editForm.value).subscribe({
      next: (updated) => {
        this.spaces = this.spaces.map(space => space.id === spaceId ? updated : space);
        this.editingId = null;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo actualizar el espacio.';
        this.loading = false;
      }
    });
  }

  deleteSpace(spaceId: number): void {
    if (!confirm('¿Seguro que quieres eliminar este espacio creativo?')) return;

    this.loading = true;
    this.error = '';
    this.creativeSpaceService.deleteSpace(spaceId).subscribe({
      next: () => {
        this.spaces = this.spaces.filter(space => space.id !== spaceId);
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo eliminar el espacio.';
        this.loading = false;
      }
    });
  }

  privacyText(privacy: SpacePrivacy): string {
    return privacy === SpacePrivacy.Shared ? 'Compartido' : 'Privado';
  }

  private loadSpaces(): void {
    this.loading = true;
    this.error = '';
    this.creativeSpaceService.getSpaces().subscribe({
      next: (spaces) => {
        this.spaces = spaces;
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudieron cargar los espacios.';
        this.loading = false;
      }
    });
  }
}
