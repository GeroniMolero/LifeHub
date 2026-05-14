import { SpaceMediaSessionService } from './space-media-session.service';
import { SpaceMediaReference } from '../models/space-media-reference.model';

function makeRef(id: string, label = 'Track'): SpaceMediaReference {
  return { id, type: 'local-session-file', label, source: `${id}.mp3`, createdAt: new Date().toISOString() };
}

describe('SpaceMediaSessionService', () => {
  let service: SpaceMediaSessionService;

  beforeEach(() => {
    localStorage.clear();
    service = new SpaceMediaSessionService();
  });

  it('getReferences returns [] when storage is empty', () => {
    expect(service.getReferences(1)).toEqual([]);
  });

  it('getReferences returns parsed array when valid JSON is stored', () => {
    const refs = [makeRef('a'), makeRef('b')];
    localStorage.setItem('space-media-session:1', JSON.stringify(refs));
    expect(service.getReferences(1)).toEqual(refs);
  });

  it('getReferences returns [] for malformed JSON without throwing', () => {
    localStorage.setItem('space-media-session:1', 'not json!!');
    expect(() => service.getReferences(1)).not.toThrow();
    expect(service.getReferences(1)).toEqual([]);
  });

  it('getReferences returns [] when stored value is not an array', () => {
    localStorage.setItem('space-media-session:1', JSON.stringify({ foo: 'bar' }));
    expect(service.getReferences(1)).toEqual([]);
  });

  it('addReference in empty storage creates [ref] and persists it', () => {
    const ref = makeRef('x');
    const result = service.addReference(1, ref);
    expect(result).toEqual([ref]);
    expect(service.getReferences(1)).toEqual([ref]);
  });

  it('addReference prepends: new ref is first', () => {
    const existing = makeRef('old');
    service.addReference(1, existing);
    const newer = makeRef('new');
    const result = service.addReference(1, newer);
    expect(result[0].id).toBe('new');
    expect(result[1].id).toBe('old');
  });

  it('addReference with same id again duplicates it (no dedup by default)', () => {
    const ref = makeRef('dup');
    service.addReference(1, ref);
    const result = service.addReference(1, ref);
    expect(result.length).toBe(2);
  });

  it('removeReference by existing id removes it from storage', () => {
    service.addReference(1, makeRef('a'));
    service.addReference(1, makeRef('b'));
    const result = service.removeReference(1, 'a');
    expect(result.map(r => r.id)).not.toContain('a');
    expect(service.getReferences(1).map(r => r.id)).not.toContain('a');
  });

  it('removeReference by non-existent id leaves array unchanged', () => {
    service.addReference(1, makeRef('a'));
    const before = service.getReferences(1).length;
    service.removeReference(1, 'does-not-exist');
    expect(service.getReferences(1).length).toBe(before);
  });

  it('keys are isolated per spaceId', () => {
    service.addReference(1, makeRef('space1-ref'));
    service.addReference(2, makeRef('space2-ref'));
    expect(service.getReferences(1).map(r => r.id)).toEqual(['space1-ref']);
    expect(service.getReferences(2).map(r => r.id)).toEqual(['space2-ref']);
  });

  it('removeReference returns the updated array', () => {
    service.addReference(1, makeRef('a'));
    service.addReference(1, makeRef('b'));
    const result = service.removeReference(1, 'b');
    expect(result).toEqual(service.getReferences(1));
  });

  it('addReference returns the full updated array', () => {
    const ref = makeRef('z');
    const returned = service.addReference(1, ref);
    expect(returned).toEqual(service.getReferences(1));
  });
});
