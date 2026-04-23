import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/auth.model';
import { ConfirmationService } from '../../services/confirmation.service';
import { AllowedWebsiteService } from '../../services/allowed-website.service';
import { AllowedWebsite } from '../../models/allowed-website.model';
import { LayoutHeaderStateService } from '../../services/layout-header-state.service';
import { ModalComponent } from '../../components/modal/modal.component';

type Tab = 'websites' | 'users';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModalComponent],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.scss']
})
export class AdminComponent implements OnInit, OnDestroy {

  // ── Tab ────────────────────────────────────────────────────────────────────

  private _activeTab: Tab = 'websites';

  get activeTab(): Tab { return this._activeTab; }

  setTab(tab: Tab): void {
    this._activeTab = tab;
    this.showModal = false;
    this.setHeaderState();
  }

  // ── Modal ──────────────────────────────────────────────────────────────────

  showModal = false;

  openModal(): void  { this.showModal = true; }
  closeModal(): void { this.showModal = false; }

  get modalTitle(): string {
    return this._activeTab === 'users' ? 'Nuevo usuario' : 'Nuevo dominio';
  }

  // ── Data ───────────────────────────────────────────────────────────────────

  users: User[] = [];
  allowedWebsites: AllowedWebsite[] = [];

  usersLoading = false;
  usersError   = '';
  createUserError  = '';
  createUserLoading = false;

  websitesLoading = false;
  websitesError   = '';
  createWebsiteError  = '';
  createWebsiteLoading = false;

  // ── Filter forms ───────────────────────────────────────────────────────────

  usersFilterForm = this.fb.group({ search: [''], role: ['all'] });
  websitesFilterForm = this.fb.group({ search: [''], status: ['all'] });

  showUsersFilters    = false;
  showWebsitesFilters = false;

  private filterSubs: Subscription[] = [];

  // ── Create forms ───────────────────────────────────────────────────────────

  createUserForm = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    fullName: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  websiteForm = this.fb.group({
    domain:   ['', Validators.required],
    isActive: [true]
  });

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private authService: AuthService,
    private confirmationService: ConfirmationService,
    private allowedWebsiteService: AllowedWebsiteService,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.filterSubs.push(
      this.usersFilterForm.valueChanges.subscribe(() => {}),
      this.websitesFilterForm.valueChanges.subscribe(() => {})
    );
    this.setHeaderState();
    this.loadUsers();
    this.loadWebsites();
  }

  ngOnDestroy(): void {
    this.filterSubs.forEach(s => s.unsubscribe());
    this.layoutHeaderStateService.clearOverride();
  }

  // ── Filtered data ──────────────────────────────────────────────────────────

  get filteredUsers(): User[] {
    const search = (this.usersFilterForm.get('search')?.value ?? '').trim().toLowerCase();
    const role   = this.usersFilterForm.get('role')?.value ?? 'all';

    return this.users.filter(u => {
      if (role !== 'all' && !u.roles?.includes(role)) return false;
      if (!search) return true;
      return [u.fullName, u.email].filter(Boolean).join(' ').toLowerCase().includes(search);
    });
  }

  get filteredWebsites(): AllowedWebsite[] {
    const search = (this.websitesFilterForm.get('search')?.value ?? '').trim().toLowerCase();
    const status = this.websitesFilterForm.get('status')?.value ?? 'all';

    return this.allowedWebsites.filter(w => {
      if (status === 'active'   && !w.isActive) return false;
      if (status === 'inactive' &&  w.isActive) return false;
      if (!search) return true;
      return w.domain.toLowerCase().includes(search);
    });
  }

  get hasActiveUsersFilters(): boolean {
    const { search, role } = this.usersFilterForm.getRawValue();
    return !!search?.trim() || role !== 'all';
  }

  get hasActiveWebsitesFilters(): boolean {
    const { search, status } = this.websitesFilterForm.getRawValue();
    return !!search?.trim() || status !== 'all';
  }

  clearUsersFilters(): void {
    this.usersFilterForm.reset({ search: '', role: 'all' });
  }

  clearWebsitesFilters(): void {
    this.websitesFilterForm.reset({ search: '', status: 'all' });
  }

  // ── Users CRUD ─────────────────────────────────────────────────────────────

  createUser(): void {
    if (this.createUserForm.invalid) return;
    this.createUserLoading = true;
    this.createUserError   = '';

    const { email, fullName, password } = this.createUserForm.value;
    this.authService.register(email!, fullName!, password!).subscribe({
      next: () => {
        this.createUserForm.reset();
        this.closeModal();
        this.createUserLoading = false;
        this.loadUsers();
      },
      error: err => {
        this.createUserError   = err?.error?.message || 'No se pudo crear el usuario.';
        this.createUserLoading = false;
      }
    });
  }

  deleteUser(userId: string): void {
    if (!this.confirmationService.confirmDelete('este usuario')) return;
    this.usersError = '';
    this.userService.deleteUser(userId).subscribe({
      next: () => { this.users = this.users.filter(u => u.id !== userId); },
      error: err => { this.usersError = err?.error?.message || 'No se pudo eliminar el usuario.'; }
    });
  }

  // ── Websites CRUD ──────────────────────────────────────────────────────────

  createWebsite(): void {
    if (this.websiteForm.invalid) return;
    this.createWebsiteLoading = true;
    this.createWebsiteError   = '';

    this.allowedWebsiteService.createAllowedWebsite({
      domain:   this.websiteForm.value.domain!,
      isActive: Boolean(this.websiteForm.value.isActive)
    }).subscribe({
      next: w => {
        this.allowedWebsites = [...this.allowedWebsites, w].sort((a, b) => a.domain.localeCompare(b.domain));
        this.websiteForm.reset({ domain: '', isActive: true });
        this.closeModal();
        this.createWebsiteLoading = false;
      },
      error: err => {
        this.createWebsiteError   = err?.error?.message || 'No se pudo crear el dominio.';
        this.createWebsiteLoading = false;
      }
    });
  }

  toggleWebsite(website: AllowedWebsite): void {
    this.websitesError = '';
    this.allowedWebsiteService.updateAllowedWebsite(website.id, !website.isActive).subscribe({
      next: updated => {
        this.allowedWebsites = this.allowedWebsites.map(w => w.id === updated.id ? updated : w);
      },
      error: err => { this.websitesError = err?.error?.message || 'No se pudo actualizar el dominio.'; }
    });
  }

  deleteWebsite(websiteId: number): void {
    if (!this.confirmationService.confirmDelete('este dominio')) return;
    this.websitesError = '';
    this.allowedWebsiteService.deleteAllowedWebsite(websiteId).subscribe({
      next: () => { this.allowedWebsites = this.allowedWebsites.filter(w => w.id !== websiteId); },
      error: err => { this.websitesError = err?.error?.message || 'No se pudo eliminar el dominio.'; }
    });
  }

  // ── Private ────────────────────────────────────────────────────────────────

  private loadUsers(): void {
    this.usersLoading = true;
    this.usersError   = '';
    this.userService.getUsers().subscribe({
      next: users => { this.users = users; this.usersLoading = false; },
      error: err  => { this.usersError = err?.error?.message || 'No se pudo cargar usuarios.'; this.usersLoading = false; }
    });
  }

  private loadWebsites(): void {
    this.websitesLoading = true;
    this.websitesError   = '';
    this.allowedWebsiteService.getAllowedWebsites().subscribe({
      next: ws  => { this.allowedWebsites = ws; this.websitesLoading = false; },
      error: err => { this.websitesError = err?.error?.message || 'No se pudo cargar dominios.'; this.websitesLoading = false; }
    });
  }

  private setHeaderState(): void {
    const isUsers = this._activeTab === 'users';
    this.layoutHeaderStateService.setOverride({
      description: isUsers
        ? 'Gestión de usuarios de la plataforma'
        : 'Dominios permitidos para recursos embebidos',
      actions: [{
        label:   isUsers ? 'Nuevo usuario' : 'Nuevo dominio',
        variant: 'primary',
        action:  () => this.openModal()
      }]
    });
  }
}
