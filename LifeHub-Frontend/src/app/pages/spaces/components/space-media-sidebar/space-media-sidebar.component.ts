import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SpaceMediaReference } from '../../../../models/space-media-reference.model';

interface PlayerTrack {
  id: string;
  title: string;
  src: string;
  image?: string;
  isStatic?: boolean;
}

const FALLBACK_IMAGE = 'assets/images/music-fallback.jpg';

const STATIC_TRACKS: PlayerTrack[] = [
  {
    id: 'static-1',
    title: 'Aventure – A Beautiful Garden',
    src: 'assets/music/aventure-garden.mp3',
    image: 'assets/images/aventure-garden.jpg',
    isStatic: true,
  },
  {
    id: 'static-2',
    title: 'Inspiring Cinematic Music – Tunetank',
    src: 'assets/music/inspiring-cinematic.mp3',
    image: 'assets/images/inspiring-cinematic.jpg',
    isStatic: true,
  },
];

@Component({
  selector: 'app-space-media-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './space-media-sidebar.component.html',
  styleUrls: ['./space-media-sidebar.component.scss']
})
export class SpaceMediaSidebarComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('audioPlayer') private readonly audioPlayer?: ElementRef<HTMLAudioElement>;
  @ViewChild('tracklist')  private readonly tracklistRef?: ElementRef<HTMLUListElement>;

  @Input({ required: true }) loadingMedia = false;
  @Input({ required: true }) mediaReferences: SpaceMediaReference[] = [];
  @Input({ required: true }) audioMediaReferences: SpaceMediaReference[] = [];
  @Input() activeVisualMediaIds: Set<string> = new Set();
  @Input() localFileBlobUrls: Map<string, string> = new Map();

  @Output() openCreateMedia = new EventEmitter<void>();
  @Output() onMediaReferenceClick = new EventEmitter<SpaceMediaReference>();
  @Output() removeMediaReference = new EventEmitter<string>();
  @Output() collapsedChange = new EventEmitter<boolean>();

  isCollapsed = true;
  activeTab: 'visual' | 'music' = 'music';
  private prevTotal = 0;
  private prevVisualCount = 0;

  currentIndex = 0;
  isPlaying = false;
  searchQuery = '';
  visualSearchQuery = '';
  isMuted = false;
  progressValue = 0;
  currentTimeLabel = '0:00';
  totalTimeLabel = '0:00';
  volumeValue = 0.8;

  private _playerTracks: PlayerTrack[] = [...STATIC_TRACKS];

  get playerTracks(): PlayerTrack[] {
    return this._playerTracks;
  }

  private rebuildPlayerTracks(): void {
    const uploaded = this.audioMediaReferences
      .map(ref => ({
        id: ref.id,
        title: ref.label,
        src: this.localFileBlobUrls.get(ref.id) ?? '',
        image: FALLBACK_IMAGE,
      }))
      .filter(t => t.src !== '');
    this._playerTracks = [...STATIC_TRACKS, ...uploaded];
  }

  trackById(_index: number, track: PlayerTrack): string {
    return track.id;
  }

  get currentTrack(): PlayerTrack | undefined {
    return this.playerTracks[this.currentIndex];
  }

  get filteredVisualMediaReferences(): SpaceMediaReference[] {
    const q = this.visualSearchQuery.trim().toLowerCase();
    if (!q) return this.visualMediaReferences;
    return this.visualMediaReferences.filter(item => item.label.toLowerCase().includes(q));
  }

  get filteredTracks(): PlayerTrack[] {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) return this.playerTracks;
    return this.playerTracks.filter(t => t.title.toLowerCase().includes(q));
  }

  trackIndexById(id: string): number {
    return this.playerTracks.findIndex(t => t.id === id);
  }

  toggleCollapsed(): void {
    this.isCollapsed = !this.isCollapsed;
    this.collapsedChange.emit(this.isCollapsed);
  }

  show(tab?: 'visual' | 'music'): void {
    this.isCollapsed = false;
    if (tab) this.activeTab = tab;
    this.collapsedChange.emit(false);
  }

  ngAfterViewInit(): void {
    this.applyVolume(this.volumeValue);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['mediaReferences'] || changes['audioMediaReferences']) {
      const total = this.mediaReferences.length + this.audioMediaReferences.length;
      const visualCount = this.visualMediaReferences.length;
      if (total > this.prevTotal && !this.isCollapsed) {
        if (visualCount > this.prevVisualCount) this.activeTab = 'visual';
        else this.activeTab = 'music';
      }
      this.prevTotal = total;
      this.prevVisualCount = visualCount;
    }

    if (changes['audioMediaReferences'] || changes['localFileBlobUrls']) {
      this.rebuildPlayerTracks();
      const tracks = this.playerTracks;
      if (tracks.length === 0) {
        const player = this.audioPlayer?.nativeElement;
        if (player) { player.pause(); player.removeAttribute('src'); player.load(); }
        this.currentIndex = 0;
        this.resetProgress();
      } else if (this.currentIndex >= tracks.length) {
        this.loadTrack(tracks.length - 1, this.isPlaying);
      }
    }
  }

  ngOnDestroy(): void {}

  togglePlay(): void {
    const player = this.audioPlayer?.nativeElement;
    if (!player || !this.currentTrack) return;
    if (!player.src || player.getAttribute('src') === '') {
      this.loadTrack(this.currentIndex, true);
      return;
    }
    player.paused ? player.play().catch(() => {}) : player.pause();
  }

  prevTrack(): void {
    if (this.playerTracks.length === 0) return;
    this.loadTrack(this.currentIndex - 1, true);
  }

  nextTrack(): void {
    if (this.playerTracks.length === 0) return;
    this.loadTrack(this.currentIndex + 1, true);
  }

  selectTrack(index: number): void {
    const player = this.audioPlayer?.nativeElement;
    if (index === this.currentIndex && player?.src) { this.togglePlay(); return; }
    this.loadTrack(index, true);
  }

  toggleMute(): void {
    const player = this.audioPlayer?.nativeElement;
    if (!player) return;
    player.muted = !player.muted;
    this.isMuted = player.muted;
  }

  onProgressInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (!target) return;
    const player = this.audioPlayer?.nativeElement;
    if (!player || !Number.isFinite(player.duration) || player.duration === 0) return;
    player.currentTime = (Number(target.value) / 1000) * player.duration;
  }

  onVolumeInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (!target) return;
    this.applyVolume(Number(target.value));
  }

  onLoadedMetadata(): void { this.updateProgress(); }
  onTimeUpdate(): void { this.updateProgress(); }
  onPlay(): void { this.isPlaying = true; }
  onPause(): void { this.isPlaying = false; }
  onEnded(): void { this.loadTrack(this.currentIndex + 1, true); }
  onVolumeUpdate(): void {
    const player = this.audioPlayer?.nativeElement;
    if (!player) return;
    this.isMuted = player.muted;
    this.volumeValue = player.volume;
  }

  isMediaActiveInMain(id: string): boolean { return this.activeVisualMediaIds.has(id); }
  getLocalMediaUrl(id: string): string | null { return this.localFileBlobUrls.get(id) ?? null; }

  isAudioItem(item: SpaceMediaReference): boolean {
    return this.audioMediaReferences.some(a => a.id === item.id);
  }

  get visualMediaReferences(): SpaceMediaReference[] {
    return this.mediaReferences.filter(item => !this.isAudioItem(item));
  }

  mediaTypeLabel(item: SpaceMediaReference): string {
    if (item.type === 'external-embed') return item.provider || 'Enlace';
    if (item.mimeType?.startsWith('video/')) return 'Vídeo';
    if (item.mimeType?.startsWith('image/')) return 'Imagen';
    return 'Local';
  }

  private loadTrack(index: number, autoplay: boolean): void {
    const tracks = this.playerTracks;
    if (tracks.length === 0) return;
    this.currentIndex = ((index % tracks.length) + tracks.length) % tracks.length;
    const player = this.audioPlayer?.nativeElement;
    if (!player) return;
    player.src = tracks[this.currentIndex].src;
    player.load();
    if (autoplay) player.play().catch(() => {});
    setTimeout(() => {
      const list = this.tracklistRef?.nativeElement;
      list?.querySelector<HTMLElement>('.track-item.is-active')
          ?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }, 0);
  }

  private updateProgress(): void {
    const player = this.audioPlayer?.nativeElement;
    if (!player || !Number.isFinite(player.duration) || player.duration === 0) {
      this.resetProgress(); return;
    }
    this.progressValue = Math.round((player.currentTime / player.duration) * 1000);
    this.currentTimeLabel = this.formatTime(player.currentTime);
    this.totalTimeLabel = this.formatTime(player.duration);
  }

  private resetProgress(): void {
    this.progressValue = 0;
    this.currentTimeLabel = '0:00';
    this.totalTimeLabel = '0:00';
    this.isPlaying = false;
  }

  private applyVolume(value: number): void {
    this.volumeValue = value;
    const player = this.audioPlayer?.nativeElement;
    if (!player) return;
    player.volume = value;
    if (value > 0 && player.muted) player.muted = false;
    this.isMuted = player.muted;
  }

  private formatTime(value: number): string {
    if (!Number.isFinite(value) || value < 0) return '0:00';
    const minutes = Math.floor(value / 60);
    const seconds = Math.floor(value % 60).toString().padStart(2, '0');
    return `${minutes}:${seconds}`;
  }
}
