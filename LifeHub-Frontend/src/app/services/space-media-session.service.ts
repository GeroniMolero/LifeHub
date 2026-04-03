import { Injectable } from '@angular/core';
import { SpaceMediaReference } from '../models/space-media-reference.model';

@Injectable({
  providedIn: 'root'
})
export class SpaceMediaSessionService {
  private readonly keyPrefix = 'space-media-session:';

  getReferences(spaceId: number): SpaceMediaReference[] {
    const raw = sessionStorage.getItem(this.getKey(spaceId));
    if (!raw) return [];

    try {
      const parsed = JSON.parse(raw) as SpaceMediaReference[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  addReference(spaceId: number, reference: SpaceMediaReference): SpaceMediaReference[] {
    const current = this.getReferences(spaceId);
    const updated = [reference, ...current];
    this.saveReferences(spaceId, updated);
    return updated;
  }

  removeReference(spaceId: number, referenceId: string): SpaceMediaReference[] {
    const updated = this.getReferences(spaceId).filter(reference => reference.id !== referenceId);
    this.saveReferences(spaceId, updated);
    return updated;
  }

  private saveReferences(spaceId: number, references: SpaceMediaReference[]): void {
    sessionStorage.setItem(this.getKey(spaceId), JSON.stringify(references));
  }

  private getKey(spaceId: number): string {
    return `${this.keyPrefix}${spaceId}`;
  }
}
