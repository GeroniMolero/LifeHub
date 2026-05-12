import { AdminGuard } from './admin.guard';

describe('AdminGuard', () => {
  let guard: AdminGuard;
  let authSpy: { canViewAdmin: jasmine.Spy };
  let routerSpy: { navigate: jasmine.Spy };

  beforeEach(() => {
    authSpy = { canViewAdmin: jasmine.createSpy('canViewAdmin') };
    routerSpy = { navigate: jasmine.createSpy('navigate') };
    guard = new AdminGuard(authSpy as any, routerSpy as any);
  });

  // ── canActivate: usuario con acceso admin ────────────────────────────────
  it('permite el acceso cuando el usuario tiene permisos de admin', () => {
    authSpy.canViewAdmin.and.returnValue(true);
    expect(guard.canActivate()).toBeTrue();
  });

  it('no redirige cuando el usuario tiene permisos de admin', () => {
    authSpy.canViewAdmin.and.returnValue(true);
    guard.canActivate();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  // ── canActivate: usuario sin acceso admin ────────────────────────────────
  it('bloquea el acceso cuando el usuario no tiene permisos de admin', () => {
    authSpy.canViewAdmin.and.returnValue(false);
    expect(guard.canActivate()).toBeFalse();
  });

  it('redirige a /home cuando el usuario no tiene permisos de admin', () => {
    authSpy.canViewAdmin.and.returnValue(false);
    guard.canActivate();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/home']);
  });
});
