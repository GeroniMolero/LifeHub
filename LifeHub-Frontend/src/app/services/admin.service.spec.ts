import { of } from 'rxjs';
import { AdminService } from './admin.service';
import { AdminUser, UserUsage } from '../models/auth.model';

describe('AdminService', () => {
  let service: AdminService;
  let httpSpy: { get: jasmine.Spy; post: jasmine.Spy; put: jasmine.Spy };

  const makeUsage = (): UserUsage => ({
    documentsCount: 2, spacesCount: 1, publishedDocumentsCount: 0,
    profileVisibleDocumentsCount: 0, profileVisibleSpacesCount: 0,
    maxDocuments: 20, maxSpaces: 10, maxPublishedDocuments: 10,
    maxProfileVisibleDocuments: 3, maxProfileVisibleSpaces: 3
  });

  const makeAdminUser = (overrides: object = {}): AdminUser => ({
    id: 'u1', email: 'u1@test.com', isActive: true,
    createdAt: '2026-01-01T00:00:00Z', roles: ['User'], claims: [],
    usage: makeUsage(), ...overrides
  });

  beforeEach(() => {
    httpSpy = {
      get:  jasmine.createSpy('get').and.returnValue(of([])),
      post: jasmine.createSpy('post').and.returnValue(of({})),
      put:  jasmine.createSpy('put').and.returnValue(of({}))
    };
    service = new AdminService(httpSpy as any);
  });

  // ── getAdminUsers ─────────────────────────────────────────────────────────

  it('getAdminUsers calls GET /admin/users', () => {
    service.getAdminUsers().subscribe();
    expect(httpSpy.get).toHaveBeenCalledWith(jasmine.stringContaining('/admin/users'));
  });

  it('getAdminUsers returns array from response', (done) => {
    const users = [makeAdminUser(), makeAdminUser({ id: 'u2', email: 'u2@test.com' })];
    httpSpy.get.and.returnValue(of(users));

    service.getAdminUsers().subscribe(result => {
      expect(result.length).toBe(2);
      expect(result[0].usage).toBeDefined();
      done();
    });
  });

  // ── toggleUserActive ──────────────────────────────────────────────────────

  it('toggleUserActive calls PUT /admin/users/{id}/toggle-active', () => {
    service.toggleUserActive('u1').subscribe();
    expect(httpSpy.put).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/users/u1/toggle-active'),
      {}
    );
  });

  // ── adminUpdateUser ───────────────────────────────────────────────────────

  it('adminUpdateUser calls PUT /admin/users/{id} with email and fullName', () => {
    service.adminUpdateUser('u1', { email: 'new@test.com', fullName: 'Nuevo' }).subscribe();
    expect(httpSpy.put).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/users/u1'),
      { email: 'new@test.com', fullName: 'Nuevo' }
    );
  });

  it('adminUpdateUser accepts undefined fullName', () => {
    service.adminUpdateUser('u1', { email: 'new@test.com' }).subscribe();
    expect(httpSpy.put).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/users/u1'),
      { email: 'new@test.com' }
    );
  });

  // ── adminSetPassword ──────────────────────────────────────────────────────

  it('adminSetPassword calls POST /admin/users/{id}/set-password with newPassword', () => {
    service.adminSetPassword('u1', 'NuevaClave123!').subscribe();
    expect(httpSpy.post).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/users/u1/set-password'),
      { newPassword: 'NuevaClave123!' }
    );
  });

  // ── adminUpdateRole ───────────────────────────────────────────────────────

  it('adminUpdateRole calls PUT /admin/users/{id}/roles with role', () => {
    service.adminUpdateRole('u1', 'Admin').subscribe();
    expect(httpSpy.put).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/users/u1/roles'),
      { role: 'Admin' }
    );
  });

  // ── getActivityLogs ───────────────────────────────────────────────────────

  it('getActivityLogs calls GET /admin/activity-logs', () => {
    service.getActivityLogs({}).subscribe();
    expect(httpSpy.get).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/activity-logs'),
      jasmine.any(Object)
    );
  });

  it('getActivityLogs builds params from query object', () => {
    service.getActivityLogs({
      userId: 'u1', userEmail: 'u@test.com',
      entityType: 'Document', page: 2, pageSize: 25
    }).subscribe();

    const [, options] = httpSpy.get.calls.mostRecent().args;
    const params: URLSearchParams = options.params;
    expect(params.get('userId')).toBe('u1');
    expect(params.get('userEmail')).toBe('u@test.com');
    expect(params.get('entityType')).toBe('Document');
    expect(params.get('page')).toBe('2');
    expect(params.get('pageSize')).toBe('25');
  });

  it('getActivityLogs omits undefined params', () => {
    service.getActivityLogs({ entityType: 'CreativeSpace' }).subscribe();

    const [, options] = httpSpy.get.calls.mostRecent().args;
    const params: URLSearchParams = options.params;
    expect(params.has('userId')).toBeFalse();
    expect(params.get('entityType')).toBe('CreativeSpace');
  });

  it('getActivityLogs includes from/to when provided', () => {
    service.getActivityLogs({ from: '2026-01-01', to: '2026-05-01' }).subscribe();

    const [, options] = httpSpy.get.calls.mostRecent().args;
    const params: URLSearchParams = options.params;
    expect(params.get('from')).toBe('2026-01-01');
    expect(params.get('to')).toBe('2026-05-01');
  });

  // ── triggerBackup ─────────────────────────────────────────────────────────

  it('triggerBackup calls POST /admin/backup with empty body', () => {
    service.triggerBackup().subscribe();
    expect(httpSpy.post).toHaveBeenCalledWith(
      jasmine.stringContaining('/admin/backup'),
      {}
    );
  });

  it('triggerBackup returns message and backupFile from response', (done) => {
    httpSpy.post.and.returnValue(of({ message: 'Backup OK', backupFile: '/var/opt/mssql/backup/db.bak' }));

    service.triggerBackup().subscribe(result => {
      expect(result.message).toBe('Backup OK');
      expect(result.backupFile).toBe('/var/opt/mssql/backup/db.bak');
      done();
    });
  });
});
