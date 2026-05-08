import { of, throwError } from 'rxjs';

import { ConfigService } from './config.service';

const createService = (httpGetResult: any) =>
  new ConfigService({ get: () => httpGetResult } as any);

describe('ConfigService', () => {
  it('returns default limits before loadLimits is called', () => {
    const service = createService(of({}));

    expect(service.maxDocuments).toBe(20);
    expect(service.maxSpaces).toBe(10);
    expect(service.maxPublishedDocs).toBe(10);
    expect(service.maxProfileDocs).toBe(3);
    expect(service.maxProfileSpaces).toBe(3);
  });

  it('updates limits after a successful loadLimits call', (done) => {
    const serverLimits = {
      maxDocumentsPerUser: 50,
      maxSpacesPerUser: 15,
      maxPublishedDocumentsPerUser: 20,
      maxProfileVisibleDocumentsPerUser: 5,
      maxProfileVisibleSpacesPerUser: 4
    };
    const service = createService(of(serverLimits));

    service.loadLimits().subscribe(() => {
      expect(service.maxDocuments).toBe(50);
      expect(service.maxSpaces).toBe(15);
      expect(service.maxPublishedDocs).toBe(20);
      expect(service.maxProfileDocs).toBe(5);
      expect(service.maxProfileSpaces).toBe(4);
      done();
    });
  });

  it('keeps default limits when the HTTP call fails', (done) => {
    const service = createService(throwError(() => new Error('Network error')));

    service.loadLimits().subscribe(() => {
      expect(service.maxDocuments).toBe(20);
      expect(service.maxSpaces).toBe(10);
      done();
    });
  });
});
