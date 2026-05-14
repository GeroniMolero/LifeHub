import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { JwtInterceptor } from './jwt.interceptor';

describe('JwtInterceptor', () => {
  let interceptor: JwtInterceptor;
  let authSpy: { getToken: jasmine.Spy; logout: jasmine.Spy };
  let routerSpy: { navigate: jasmine.Spy };

  const makeRequest = (url = '/api/test') => ({
    url,
    clone: jasmine.createSpy('clone').and.callFake((opts: any) => ({ url, ...opts }))
  });

  const makeHandler = (response: any) => ({
    handle: jasmine.createSpy('handle').and.returnValue(response)
  });

  beforeEach(() => {
    authSpy = {
      getToken: jasmine.createSpy('getToken').and.returnValue(null),
      logout: jasmine.createSpy('logout')
    };
    routerSpy = {
      navigate: jasmine.createSpy('navigate').and.returnValue(Promise.resolve(true))
    };
    interceptor = new JwtInterceptor(authSpy as any, routerSpy as any);
  });

  // ── Cabecera Authorization ────────────────────────────────────────────────
  it('añade la cabecera Authorization cuando hay token', (done) => {
    authSpy.getToken.and.returnValue('mi-token');
    const request = makeRequest();
    const handler = makeHandler(of({}));

    interceptor.intercept(request as any, handler as any).subscribe(() => {
      expect(request.clone).toHaveBeenCalledWith({
        setHeaders: { Authorization: 'Bearer mi-token' }
      });
      done();
    });
  });

  it('no modifica la petición cuando no hay token', (done) => {
    authSpy.getToken.and.returnValue(null);
    const request = makeRequest();
    const handler = makeHandler(of({}));

    interceptor.intercept(request as any, handler as any).subscribe(() => {
      expect(request.clone).not.toHaveBeenCalled();
      done();
    });
  });

  it('pasa la petición original al handler cuando no hay token', (done) => {
    authSpy.getToken.and.returnValue(null);
    const request = makeRequest();
    const handler = makeHandler(of({}));

    interceptor.intercept(request as any, handler as any).subscribe(() => {
      expect(handler.handle).toHaveBeenCalledWith(request as any);
      done();
    });
  });

  // ── Error 401 ────────────────────────────────────────────────────────────
  it('llama a logout cuando la respuesta es 401', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 401 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: () => {
        expect(authSpy.logout).toHaveBeenCalled();
        done();
      }
    });
  });

  it('redirige a /login cuando la respuesta es 401', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 401 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: () => {
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
        done();
      }
    });
  });

  it('propaga el error 401 al suscriptor', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 401 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
        done();
      }
    });
  });

  // ── Otros errores ────────────────────────────────────────────────────────
  it('no llama a logout en error 403', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 403 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: () => {
        expect(authSpy.logout).not.toHaveBeenCalled();
        done();
      }
    });
  });

  it('no llama a logout en error 500', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 500 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: () => {
        expect(authSpy.logout).not.toHaveBeenCalled();
        done();
      }
    });
  });

  it('propaga errores que no son 401', (done) => {
    const request = makeRequest();
    const error = new HttpErrorResponse({ status: 500 });
    const handler = makeHandler(throwError(() => error));

    interceptor.intercept(request as any, handler as any).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(500);
        done();
      }
    });
  });
});
