import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CreativeSpace, SpacePrivacy } from '../../../models/creative-space.model';
import { CreativeSpaceService } from '../../../services/creative-space.service';

@Component({
  selector: 'app-spaces-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './spaces-list.component.html',
  styleUrls: ['./spaces-list.component.scss']
})
export class SpacesListComponent implements OnInit {
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
