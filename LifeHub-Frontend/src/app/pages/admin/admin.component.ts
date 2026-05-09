import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { AdminService, ActivityLogQuery } from '../../services/admin.service';
import { AdminUser, ActivityLogEntry } from '../../models/auth.model';
import { ConfirmationService } from '../../services/confirmation.service';
import { AllowedWebsiteService } from '../../services/allowed-website.service';
import { AllowedWebsite } from '../../models/allowed-website.model';
import { LayoutHeaderStateService } from '../../services/layout-header-state.service';
import { ModalComponent } from '../../components/modal/modal.component';

type Tab = 'websites' | 'users' | 'logs' | 'sistema';
type EditUserTab = 'data' | 'password' | 'role';

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
    this.closeEditUserModal();
    this.setHeaderState();
    if (tab === 'logs' && this.activityLogs.length === 0) {
      this.loadActivityLogs();
    }
  }

  // ── Modal (crear dominio / usuario) ────────────────────────────────────────

  showModal = false;

  openModal(): void  { this.showModal = true; }
  closeModal(): void { this.showModal = false; }

  get modalTitle(): string {
    return this._activeTab === 'users' ? 'Nuevo usuario' : 'Nuevo dominio';
  }

  // ── Edit user modal ────────────────────────────────────────────────────────

  showEditUserModal = false;
  editingUser: AdminUser | null = null;
  editUserTab: EditUserTab = 'data';

  editUserForm = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    fullName: ['', Validators.maxLength(100)]
  });

  setPasswordForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(128)]]
  });

  roleForm = this.fb.group({
    role: ['', Validators.required]
  });

  editUserLoading = false;
  editUserError   = '';

  openEditUserModal(user: AdminUser): void {
    this.editingUser = user;
    this.editUserTab = 'data';
    this.editUserError = '';
    this.editUserForm.reset({ email: user.email, fullName: user.fullName ?? '' });
    this.setPasswordForm.reset({ newPassword: '' });
    this.roleForm.reset({ role: user.roles[0] ?? 'User' });
    this.showEditUserModal = true;
  }

  closeEditUserModal(): void {
    this.showEditUserModal = false;
    this.editingUser = null;
    this.editUserError = '';
  }

  setEditUserTab(tab: EditUserTab): void {
    this.editUserTab = tab;
    this.editUserError = '';
  }

  saveEditUser(): void {
    if (this.editUserForm.invalid || !this.editingUser) return;
    this.editUserLoading = true;
    this.editUserError   = '';
    const { email, fullName } = this.editUserForm.value;
    this.adminService.adminUpdateUser(this.editingUser.id, { email: email!, fullName: fullName ?? undefined }).subscribe({
      next: updated => {
        this.adminUsers = this.adminUsers.map(u => u.id === updated.id ? updated : u);
        this.editingUser = updated;
        this.editUserLoading = false;
      },
      error: err => {
        this.editUserError   = err?.error?.message || 'No se pudo actualizar el usuario.';
        this.editUserLoading = false;
      }
    });
  }

  savePassword(): void {
    if (this.setPasswordForm.invalid || !this.editingUser) return;
    this.editUserLoading = true;
    this.editUserError   = '';
    const { newPassword } = this.setPasswordForm.value;
    this.adminService.adminSetPassword(this.editingUser.id, newPassword!).subscribe({
      next: () => {
        this.setPasswordForm.reset({ newPassword: '' });
        this.editUserLoading = false;
      },
      error: err => {
        this.editUserError   = err?.error?.message || 'No se pudo actualizar la contraseña.';
        this.editUserLoading = false;
      }
    });
  }

  saveRole(): void {
    if (this.roleForm.invalid || !this.editingUser) return;
    this.editUserLoading = true;
    this.editUserError   = '';
    const { role } = this.roleForm.value;
    this.adminService.adminUpdateRole(this.editingUser.id, role!).subscribe({
      next: updated => {
        this.adminUsers = this.adminUsers.map(u => u.id === updated.id ? updated : u);
        this.editingUser = updated;
        this.editUserLoading = false;
      },
      error: err => {
        this.editUserError   = err?.error?.message || 'No se pudo cambiar el rol.';
        this.editUserLoading = false;
      }
    });
  }

  toggleActive(user: AdminUser): void {
    this.usersError = '';
    this.adminService.toggleUserActive(user.id).subscribe({
      next: updated => {
        this.adminUsers = this.adminUsers.map(u => u.id === updated.id ? updated : u);
        if (this.editingUser?.id === updated.id) this.editingUser = updated;
      },
      error: err => { this.usersError = err?.error?.message || 'No se pudo cambiar el estado.'; }
    });
  }

  viewUserLogs(user: AdminUser): void {
    this.logsFilterForm.reset({ userId: user.id, userEmail: user.email, entityType: '', from: '', to: '' });
    this.logsPage = 1;
    this.setTab('logs');
  }

  // ── Data ───────────────────────────────────────────────────────────────────

  adminUsers: AdminUser[] = [];
  allowedWebsites: AllowedWebsite[] = [];

  usersLoading = false;
  usersError   = '';
  createUserError  = '';
  createUserLoading = false;

  websitesLoading = false;
  websitesError   = '';
  createWebsiteError  = '';
  createWebsiteLoading = false;

  // ── Activity logs ──────────────────────────────────────────────────────────

  activityLogs: ActivityLogEntry[] = [];
  logsLoading   = false;
  logsError     = '';
  logsTotalCount = 0;
  logsPage       = 1;
  readonly logsPageSize = 50;

  logsFilterForm = this.fb.group({
    userId:     [''],
    userEmail:  [''],
    entityType: [''],
    from:       [''],
    to:         ['']
  });

  // ── Sistema / Backup ───────────────────────────────────────────────────────

  backupLoading = false;
  backupResult  = '';
  backupError   = '';

  // ── Filter forms ───────────────────────────────────────────────────────────

  usersFilterForm    = this.fb.group({ search: [''], role: ['all'] });
  websitesFilterForm = this.fb.group({ search: [''], status: ['all'] });

  showUsersFilters    = false;
  showWebsitesFilters = false;

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

  // ── Pagination ─────────────────────────────────────────────────────────────

  get logsTotalPages(): number {
    return Math.max(1, Math.ceil(this.logsTotalCount / this.logsPageSize));
  }

  get logsPages(): number[] {
    const pages: number[] = [];
    for (let i = 1; i <= this.logsTotalPages; i++) pages.push(i);
    return pages;
  }

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private authService: AuthService,
    private adminService: AdminService,
    private confirmationService: ConfirmationService,
    private allowedWebsiteService: AllowedWebsiteService,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.setHeaderState();
    this.loadAdminUsers();
    this.loadWebsites();
  }

  ngOnDestroy(): void {
    this.layoutHeaderStateService.clearOverride();
  }

  // ── Filtered data ──────────────────────────────────────────────────────────

  get filteredUsers(): AdminUser[] {
    const search = (this.usersFilterForm.get('search')?.value ?? '').trim().toLowerCase();
    const role   = this.usersFilterForm.get('role')?.value ?? 'all';

    return this.adminUsers.filter(u => {
      if (role !== 'all' && !u.roles.includes(role)) return false;
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

  // ── Usage helpers ──────────────────────────────────────────────────────────

  isUsageWarn(value: number, max: number): boolean {
    return max > 0 && value / max >= 0.9;
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
        this.loadAdminUsers();
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
      next: () => { this.adminUsers = this.adminUsers.filter(u => u.id !== userId); },
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

  // ── Activity logs ──────────────────────────────────────────────────────────

  loadActivityLogs(): void {
    this.logsLoading = true;
    this.logsError   = '';
    const v = this.logsFilterForm.value;
    const query: ActivityLogQuery = {
      userId:     v.userId || undefined,
      userEmail:  v.userEmail || undefined,
      entityType: v.entityType || undefined,
      from:       v.from || undefined,
      to:         v.to || undefined,
      page:       this.logsPage,
      pageSize:   this.logsPageSize
    };
    this.adminService.getActivityLogs(query).subscribe({
      next: result => {
        this.activityLogs  = result.items;
        this.logsTotalCount = result.totalCount;
        this.logsLoading   = false;
      },
      error: err => {
        this.logsError   = err?.error?.message || 'No se pudo cargar los logs.';
        this.logsLoading = false;
      }
    });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.logsTotalPages) return;
    this.logsPage = page;
    this.loadActivityLogs();
  }

  clearLogsFilters(): void {
    this.logsFilterForm.reset({ userId: '', userEmail: '', entityType: '', from: '', to: '' });
    this.logsPage = 1;
    this.loadActivityLogs();
  }

  // ── Backup ─────────────────────────────────────────────────────────────────

  triggerBackup(): void {
    this.backupLoading = true;
    this.backupResult  = '';
    this.backupError   = '';
    this.adminService.triggerBackup().subscribe({
      next: res => {
        this.backupResult  = res.backupFile ? `Backup guardado: ${res.backupFile}` : res.message;
        this.backupLoading = false;
      },
      error: err => {
        this.backupError   = err?.error?.message || 'Error al lanzar el backup.';
        this.backupLoading = false;
      }
    });
  }

  // ── Private ────────────────────────────────────────────────────────────────

  private loadAdminUsers(): void {
    this.usersLoading = true;
    this.usersError   = '';
    this.adminService.getAdminUsers().subscribe({
      next: users => { this.adminUsers = users; this.usersLoading = false; },
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
    const tab = this._activeTab;
    if (tab === 'logs' || tab === 'sistema') {
      this.layoutHeaderStateService.setOverride({
        description: tab === 'logs' ? 'Registro de actividad de la plataforma' : 'Herramientas del sistema',
        actions: []
      });
      return;
    }
    const isUsers = tab === 'users';
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
