import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface LayoutHeaderState {
  title?: string;
  description?: string;
  meta?: string[];
  actions?: LayoutHeaderAction[];
}

export interface LayoutHeaderAction {
  label: string;
  variant?: 'primary' | 'secondary' | 'ghost';
  route?: string | string[];
  action?: () => void;
}

@Injectable({
  providedIn: 'root'
})
export class LayoutHeaderStateService {
  private readonly headerOverrideSubject = new BehaviorSubject<LayoutHeaderState | null>(null);
  readonly headerOverride$ = this.headerOverrideSubject.asObservable();

  setOverride(state: LayoutHeaderState): void {
    this.headerOverrideSubject.next(state);
  }

  clearOverride(): void {
    this.headerOverrideSubject.next(null);
  }
}