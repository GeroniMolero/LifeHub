import { SpaceMediaSidebarComponent } from './space-media-sidebar.component';
import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

function makeAudioRef(id: string, label = `Track ${id}`): SpaceMediaReference {
  return { id, type: 'local-session-file', label, source: `${id}.mp3`, mimeType: 'audio/mpeg', createdAt: new Date().toISOString() };
}

function makeVisualRef(id: string): SpaceMediaReference {
  return { id, type: 'local-session-file', label: `Image ${id}`, source: `${id}.png`, mimeType: 'image/png', createdAt: new Date().toISOString() };
}

function makeComponent(): SpaceMediaSidebarComponent {
  const c = new SpaceMediaSidebarComponent();
  c.loadingMedia = false;
  c.mediaReferences = [];
  c.audioMediaReferences = [];
  return c;
}

describe('SpaceMediaSidebarComponent', () => {

  // ── playerTracks ──────────────────────────────────────────────────────────

  describe('playerTracks', () => {
    it('returns only the 2 static tracks when no audioMediaReferences', () => {
      const c = makeComponent();
      expect(c.playerTracks.length).toBe(2);
      expect(c.playerTracks.every(t => t.isStatic)).toBeTrue();
    });

    it('includes uploaded tracks appended after static ones', () => {
      const c = makeComponent();
      const ref = makeAudioRef('upload-1');
      c.audioMediaReferences = [ref];
      c.localFileBlobUrls = new Map([['upload-1', 'blob:http://localhost/1']]);
      (c as any).rebuildPlayerTracks();
      const tracks = c.playerTracks;
      expect(tracks.length).toBe(3);
      expect(tracks[2].id).toBe('upload-1');
      expect(tracks[2].src).toBe('blob:http://localhost/1');
    });

    it('filters out audio refs that have no blob URL in the map', () => {
      const c = makeComponent();
      c.audioMediaReferences = [makeAudioRef('no-blob')];
      c.localFileBlobUrls = new Map();
      (c as any).rebuildPlayerTracks();
      expect(c.playerTracks.length).toBe(2);
    });

    it('filters out audio refs whose blob URL is empty string', () => {
      const c = makeComponent();
      c.audioMediaReferences = [makeAudioRef('empty-blob')];
      c.localFileBlobUrls = new Map([['empty-blob', '']]);
      (c as any).rebuildPlayerTracks();
      expect(c.playerTracks.length).toBe(2);
    });
  });

  // ── filteredTracks ────────────────────────────────────────────────────────

  describe('filteredTracks', () => {
    it('returns all playerTracks when searchQuery is empty', () => {
      const c = makeComponent();
      c.searchQuery = '';
      expect(c.filteredTracks.length).toBe(c.playerTracks.length);
    });

    it('returns matching track when query matches part of title', () => {
      const c = makeComponent();
      c.searchQuery = 'aventure';
      const result = c.filteredTracks;
      expect(result.length).toBe(1);
      expect(result[0].title.toLowerCase()).toContain('aventure');
    });

    it('returns empty array when no track matches the query', () => {
      const c = makeComponent();
      c.searchQuery = 'zzzzz-no-match';
      expect(c.filteredTracks.length).toBe(0);
    });

    it('search is case-insensitive', () => {
      const c = makeComponent();
      c.searchQuery = 'AVENTURE';
      expect(c.filteredTracks.length).toBe(1);
    });

    it('trims whitespace from search query', () => {
      const c = makeComponent();
      c.searchQuery = '  cinematic  ';
      expect(c.filteredTracks.length).toBe(1);
    });
  });

  // ── trackIndexById ────────────────────────────────────────────────────────

  describe('trackIndexById', () => {
    it('returns 0 for the first static track id', () => {
      const c = makeComponent();
      expect(c.trackIndexById('static-1')).toBe(0);
    });

    it('returns 1 for the second static track id', () => {
      const c = makeComponent();
      expect(c.trackIndexById('static-2')).toBe(1);
    });

    it('returns 2 for an uploaded track that is the third entry', () => {
      const c = makeComponent();
      c.audioMediaReferences = [makeAudioRef('upload-x')];
      c.localFileBlobUrls = new Map([['upload-x', 'blob:http://localhost/x']]);
      (c as any).rebuildPlayerTracks();
      expect(c.trackIndexById('upload-x')).toBe(2);
    });

    it('returns -1 for an unknown id', () => {
      const c = makeComponent();
      expect(c.trackIndexById('does-not-exist')).toBe(-1);
    });
  });

  // ── selectTrack ───────────────────────────────────────────────────────────

  describe('selectTrack', () => {
    it('changes currentIndex when selecting a different track', () => {
      const c = makeComponent();
      c.currentIndex = 0;
      c.selectTrack(1);
      expect(c.currentIndex).toBe(1);
    });

    it('same index with no player src calls loadTrack (index stays same)', () => {
      const c = makeComponent();
      c.currentIndex = 0;
      c.selectTrack(0);
      expect(c.currentIndex).toBe(0);
    });
  });

  // ── prevTrack / nextTrack wrap-around ─────────────────────────────────────

  describe('prevTrack / nextTrack', () => {
    it('nextTrack from the last track wraps around to index 0', () => {
      const c = makeComponent();
      const last = c.playerTracks.length - 1;
      c.currentIndex = last;
      c.nextTrack();
      expect(c.currentIndex).toBe(0);
    });

    it('prevTrack from index 0 wraps to the last track', () => {
      const c = makeComponent();
      c.currentIndex = 0;
      c.prevTrack();
      expect(c.currentIndex).toBe(c.playerTracks.length - 1);
    });
  });

  // ── visualMediaReferences ─────────────────────────────────────────────────

  describe('visualMediaReferences', () => {
    it('excludes audio references from the visual list', () => {
      const c = makeComponent();
      const audio = makeAudioRef('aud-1');
      const visual = makeVisualRef('vis-1');
      c.mediaReferences = [audio, visual];
      c.audioMediaReferences = [audio];
      const result = c.visualMediaReferences;
      expect(result.map(r => r.id)).not.toContain('aud-1');
      expect(result.map(r => r.id)).toContain('vis-1');
    });

    it('returns empty array when all refs are audio', () => {
      const c = makeComponent();
      const audio = makeAudioRef('aud-2');
      c.mediaReferences = [audio];
      c.audioMediaReferences = [audio];
      expect(c.visualMediaReferences.length).toBe(0);
    });
  });

  // ── formatTime (private, tested via cast) ─────────────────────────────────

  describe('formatTime (private)', () => {
    it('formats 0 as "0:00"', () => {
      const c = makeComponent() as any;
      expect(c.formatTime(0)).toBe('0:00');
    });

    it('formats 65 seconds as "1:05"', () => {
      const c = makeComponent() as any;
      expect(c.formatTime(65)).toBe('1:05');
    });

    it('formats NaN as "0:00"', () => {
      const c = makeComponent() as any;
      expect(c.formatTime(NaN)).toBe('0:00');
    });

    it('formats negative value as "0:00"', () => {
      const c = makeComponent() as any;
      expect(c.formatTime(-5)).toBe('0:00');
    });

    it('formats 3600 seconds as "60:00"', () => {
      const c = makeComponent() as any;
      expect(c.formatTime(3600)).toBe('60:00');
    });
  });

  // ── currentTrack ──────────────────────────────────────────────────────────

  describe('currentTrack', () => {
    it('returns the track at currentIndex', () => {
      const c = makeComponent();
      c.currentIndex = 1;
      expect(c.currentTrack?.id).toBe('static-2');
    });

    it('returns undefined when currentIndex is out of bounds', () => {
      const c = makeComponent();
      c.currentIndex = 999;
      expect(c.currentTrack).toBeUndefined();
    });
  });
});
