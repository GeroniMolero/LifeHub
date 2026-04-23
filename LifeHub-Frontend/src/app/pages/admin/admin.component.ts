import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../../services/user.service';
import { User } from '../../models/auth.model';
import { ConfirmationService } from '../../services/confirmation.service';
import { AllowedWebsiteService } from '../../services/allowed-website.service';
import { AllowedWebsite } from '../../models/allowed-website.model';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss']
})
export class AdminComponent implements OnInit {
  users: User[] = [];
  allowedWebsites: AllowedWebsite[] = [];

  loading = false;
  error = '';
  usersError = '';
  websitesLoading = false;
  websitesError = '';

  websiteForm = this.fb.group({
    domain: ['', Validators.required],
    isActive: [true, Validators.required]
  });

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private confirmationService: ConfirmationService,
    private allowedWebsiteService: AllowedWebsiteService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadAllowedWebsites();
  }

  createWebsite(): void {
    if (this.websiteForm.invalid) return;

    this.websitesLoading = true;
    this.websitesError = '';

    this.allowedWebsiteService.createAllowedWebsite({
      domain: this.websiteForm.value.domain || '',
      isActive: Boolean(this.websiteForm.value.isActive)
    }).subscribe({
      next: website => {
        this.allowedWebsites = [...this.allowedWebsites, website].sort((a, b) => a.domain.localeCompare(b.domain));
        this.websiteForm.reset({ domain: '', isActive: true });
        this.websitesLoading = false;
      },
      error: err => {
        this.websitesError = err?.error?.message || 'No se pudo crear el dominio permitido.';
        this.websitesLoading = false;
      }
    });
  }

  toggleWebsite(website: AllowedWebsite): void {
    this.websitesError = '';
    this.allowedWebsiteService.updateAllowedWebsite(website.id, !website.isActive).subscribe({
      next: updated => {
        this.allowedWebsites = this.allowedWebsites.map(item => item.id === updated.id ? updated : item);
      },
      error: err => {
        this.websitesError = err?.error?.message || 'No se pudo actualizar el dominio.';
      }
    });
  }

  deleteWebsite(websiteId: number): void {
    if (!this.confirmationService.confirmDelete('este dominio permitido')) return;

    this.websitesError = '';
    this.allowedWebsiteService.deleteAllowedWebsite(websiteId).subscribe({
      next: () => {
        this.allowedWebsites = this.allowedWebsites.filter(item => item.id !== websiteId);
      },
      error: err => {
        this.websitesError = err?.error?.message || 'No se pudo eliminar el dominio.';
      }
    });
  }

  deleteUser(userId: string): void {
    if (!this.confirmationService.confirmDelete('este usuario')) return;

    this.usersError = '';
    this.userService.deleteUser(userId).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.id !== userId);
      },
      error: err => {
        this.usersError = err?.error?.message || 'No se pudo eliminar el usuario.';
      }
    });
  }

  private loadUsers(): void {
    this.loading = true;
    this.error = '';

    this.userService.getUsers().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: err => {
        this.error = err?.error?.message || 'No se pudo cargar el listado de usuarios.';
        this.loading = false;
      }
    });
  }

  private loadAllowedWebsites(): void {
    this.websitesLoading = true;
    this.websitesError = '';

    this.allowedWebsiteService.getAllowedWebsites().subscribe({
      next: websites => {
        this.allowedWebsites = websites;
        this.websitesLoading = false;
      },
      error: err => {
        this.websitesError = err?.error?.message || 'No se pudo cargar la allowlist de dominios.';
        this.websitesLoading = false;
      }
    });
  }
}
