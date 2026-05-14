import { AuthGuard } from './auth.guard';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authSpy: { isAuthenticated: jasmine.Spy };
  let routerSpy: { navigate: jasmine.Spy };
  const mockRoute: any = {};

  beforeEach(() => {
    authSpy = { isAuthenticated: jasmine.createSpy('isAuthenticated') };
    routerSpy = { navigate: jasmine.createSpy('navigate') };
    guard = new AuthGuard(authSpy as any, routerSpy as any);
  });

  // ── canActivate: usuario autenticado ─────────────────────────────────────
  it('permite el acceso cuando el usuario está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    expect(guard.canActivate(mockRoute, { url: '/home' } as any)).toBeTrue();
  });

  it('no redirige cuando el usuario está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    guard.canActivate(mockRoute, { url: '/home' } as any);
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  // ── canActivate: usuario no autenticado ──────────────────────────────────
  it('bloquea el acceso cuando el usuario no está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(false);
    expect(guard.canActivate(mockRoute, { url: '/home' } as any)).toBeFalse();
  });

  it('redirige a /login cuando el usuario no está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(false);
    guard.canActivate(mockRoute, { url: '/home' } as any);
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ['/login'],
      { queryParams: { returnUrl: '/home' } }
    );
  });

  it('incluye la URL original como returnUrl al redirigir', () => {
    authSpy.isAuthenticated.and.returnValue(false);
    guard.canActivate(mockRoute, { url: '/spaces/42' } as any);
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ['/login'],
      { queryParams: { returnUrl: '/spaces/42' } }
    );
  });
});
