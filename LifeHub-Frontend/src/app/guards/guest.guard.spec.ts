import { GuestGuard } from './guest.guard';

describe('GuestGuard', () => {
  let guard: GuestGuard;
  let authSpy: { isAuthenticated: jasmine.Spy };
  let routerSpy: { navigate: jasmine.Spy };

  beforeEach(() => {
    authSpy = { isAuthenticated: jasmine.createSpy('isAuthenticated') };
    routerSpy = { navigate: jasmine.createSpy('navigate') };
    guard = new GuestGuard(authSpy as any, routerSpy as any);
  });

  // ── canActivate: usuario no autenticado (invitado) ───────────────────────
  it('permite el acceso cuando el usuario no está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(false);
    expect(guard.canActivate()).toBeTrue();
  });

  it('no redirige cuando el usuario no está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(false);
    guard.canActivate();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  // ── canActivate: usuario ya autenticado ──────────────────────────────────
  it('bloquea el acceso cuando el usuario ya está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    expect(guard.canActivate()).toBeFalse();
  });

  it('redirige a /home cuando el usuario ya está autenticado', () => {
    authSpy.isAuthenticated.and.returnValue(true);
    guard.canActivate();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/home']);
  });
});
