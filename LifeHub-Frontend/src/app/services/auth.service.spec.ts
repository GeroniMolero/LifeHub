import { of } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpSpy: { post: jasmine.Spy; get: jasmine.Spy };

  const makeUser = (overrides: object = {}) => ({
    id: 'u1',
    email: 'u1@test.com',
    fullName: 'User One',
    roles: [] as string[],
    claims: [] as string[],
    ...overrides
  });

  beforeEach(() => {
    localStorage.clear();
    httpSpy = {
      post: jasmine.createSpy('post').and.returnValue(of({})),
      get: jasmine.createSpy('get').and.returnValue(of({}))
    };
    service = new AuthService(httpSpy as any);
  });

  // ── isAuthenticated ──────────────────────────────────────────────────────
  it('isAuthenticated returns false when no token', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('isAuthenticated returns true when token is in localStorage', () => {
    localStorage.setItem('lifehub_token', 'tok123');
    service = new AuthService(httpSpy as any);
    expect(service.isAuthenticated()).toBeTrue();
  });

  // ── login ────────────────────────────────────────────────────────────────
  it('login stores token on successful response', (done) => {
    const user = makeUser();
    httpSpy.post.and.returnValue(of({ success: true, token: 'my-token', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(localStorage.getItem('lifehub_token')).toBe('my-token');
      done();
    });
  });

  it('login stores user in localStorage on success', (done) => {
    const user = makeUser({ fullName: 'Test User' });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      const stored = JSON.parse(localStorage.getItem('lifehub_user')!);
      expect(stored.fullName).toBe('Test User');
      done();
    });
  });

  it('login does not store token when success is false', (done) => {
    httpSpy.post.and.returnValue(of({ success: false }));

    service.login('u1@test.com', 'wrong').subscribe(() => {
      expect(localStorage.getItem('lifehub_token')).toBeNull();
      done();
    });
  });

  // ── logout ───────────────────────────────────────────────────────────────
  it('logout clears token and user from localStorage', () => {
    localStorage.setItem('lifehub_token', 'tok');
    localStorage.setItem('lifehub_user', '{}');

    service.logout();

    expect(localStorage.getItem('lifehub_token')).toBeNull();
    expect(localStorage.getItem('lifehub_user')).toBeNull();
  });

  it('logout emits null from getCurrentUser', () => {
    const user = makeUser();
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      httpSpy.post.and.returnValue(of({}));
      service.logout();

      let current: any = 'not-null';
      service.getCurrentUser().subscribe(u => current = u);
      expect(current).toBeNull();
    });
  });

  // ── hasRole ──────────────────────────────────────────────────────────────
  it('hasRole returns false when not logged in', () => {
    expect(service.hasRole('Admin')).toBeFalse();
  });

  it('hasRole returns true for matching role (case-insensitive)', (done) => {
    const user = makeUser({ roles: ['Admin'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.hasRole('admin')).toBeTrue();
      expect(service.hasRole('ADMIN')).toBeTrue();
      done();
    });
  });

  it('hasRole returns false for non-matching role', (done) => {
    const user = makeUser({ roles: ['User'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.hasRole('Admin')).toBeFalse();
      done();
    });
  });

  // ── hasClaim ─────────────────────────────────────────────────────────────
  it('hasClaim returns false when not logged in', () => {
    expect(service.hasClaim('permission', 'admin.users.view')).toBeFalse();
  });

  it('hasClaim returns true for exact match', (done) => {
    const user = makeUser({ claims: ['permission:admin.users.view'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.hasClaim('permission', 'admin.users.view')).toBeTrue();
      done();
    });
  });

  it('hasClaim with type only matches any value of that type', (done) => {
    const user = makeUser({ claims: ['permission:some.value'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.hasClaim('permission')).toBeTrue();
      done();
    });
  });

  // ── canViewAdmin ─────────────────────────────────────────────────────────
  it('canViewAdmin returns true for Admin role', (done) => {
    const user = makeUser({ roles: ['Admin'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.canViewAdmin()).toBeTrue();
      done();
    });
  });

  it('canViewAdmin returns true for admin claim', (done) => {
    const user = makeUser({ claims: ['permission:admin.users.view'] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.canViewAdmin()).toBeTrue();
      done();
    });
  });

  it('canViewAdmin returns false for regular user', (done) => {
    const user = makeUser({ roles: ['User'], claims: [] });
    httpSpy.post.and.returnValue(of({ success: true, token: 'tok', user }));

    service.login('u1@test.com', 'pass').subscribe(() => {
      expect(service.canViewAdmin()).toBeFalse();
      done();
    });
  });

  // ── loadUser on construction ─────────────────────────────────────────────
  it('restores user from localStorage on construction', () => {
    const user = makeUser({ fullName: 'Stored User' });
    localStorage.setItem('lifehub_token', 'tok');
    localStorage.setItem('lifehub_user', JSON.stringify(user));

    service = new AuthService(httpSpy as any);

    let current: any = null;
    service.getCurrentUser().subscribe(u => (current = u));
    expect(current?.fullName).toBe('Stored User');
  });

  it('does not crash when stored user JSON is malformed', () => {
    localStorage.setItem('lifehub_token', 'tok');
    localStorage.setItem('lifehub_user', 'not-json');

    expect(() => new AuthService(httpSpy as any)).not.toThrow();
  });
});
