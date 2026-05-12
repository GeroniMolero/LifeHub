import { MediaFileStorageService } from './media-file-storage.service';

function makeFile(name: string, type = 'audio/mpeg'): File {
  return new File(['audio-data'], name, { type });
}

describe('MediaFileStorageService', () => {
  let service: MediaFileStorageService;

  beforeEach(() => {
    service = new MediaFileStorageService();
  });

  it('saveFile + getFile returns a File with the same name and type', async () => {
    const file = makeFile('test-track.mp3');
    await service.saveFile('id-1', file);
    const result = await service.getFile('id-1');
    expect(result).not.toBeNull();
    expect(result!.name).toBe('test-track.mp3');
    expect(result!.type).toBe('audio/mpeg');
  });

  it('getFile for unknown id returns null', async () => {
    const result = await service.getFile('nonexistent-xyz');
    expect(result).toBeNull();
  });

  it('deleteFile after saveFile makes getFile return null', async () => {
    const file = makeFile('to-delete.mp3');
    await service.saveFile('id-del', file);
    await service.deleteFile('id-del');
    const result = await service.getFile('id-del');
    expect(result).toBeNull();
  });

  it('saveFile with the same id twice overwrites the previous file', async () => {
    await service.saveFile('id-overwrite', makeFile('first.mp3'));
    await service.saveFile('id-overwrite', makeFile('second.mp3'));
    const result = await service.getFile('id-overwrite');
    expect(result!.name).toBe('second.mp3');
  });

  it('a second service instance reuses the same IndexedDB (does not fail on reopen)', async () => {
    const file = makeFile('shared.mp3');
    await service.saveFile('id-shared', file);
    const service2 = new MediaFileStorageService();
    const result = await service2.getFile('id-shared');
    expect(result).not.toBeNull();
    expect(result!.name).toBe('shared.mp3');
  });

  it('deleteFile on a non-existent id does not throw', async () => {
    await expectAsync(service.deleteFile('id-not-there')).toBeResolved();
  });

  it('saves and retrieves an image file', async () => {
    const img = new File(['img-data'], 'photo.png', { type: 'image/png' });
    await service.saveFile('id-img', img);
    const result = await service.getFile('id-img');
    expect(result!.type).toBe('image/png');
    expect(result!.name).toBe('photo.png');
  });

  it('multiple files can coexist independently', async () => {
    await service.saveFile('id-a', makeFile('a.mp3'));
    await service.saveFile('id-b', makeFile('b.mp3'));
    const a = await service.getFile('id-a');
    const b = await service.getFile('id-b');
    expect(a!.name).toBe('a.mp3');
    expect(b!.name).toBe('b.mp3');
  });
});
