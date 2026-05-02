import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: number;
  message: string;
  type: ToastType;
  durationMs: number;
  leaving: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly MAX_VISIBLE = 5;
  private readonly LEAVE_DURATION_MS = 300;
  private readonly toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  private nextId = 1;

  get toasts$(): Observable<ToastMessage[]> {
    return this.toastsSubject.asObservable();
  }

  success(message: string, durationMs = 3500): number {
    return this.show(message, 'success', durationMs);
  }

  error(message: string, durationMs = 4500): number {
    return this.show(message, 'error', durationMs);
  }

  info(message: string, durationMs = 3500): number {
    return this.show(message, 'info', durationMs);
  }

  dismiss(id: number): void {
    const current = this.toastsSubject.value;
    const toast = current.find(t => t.id === id);
    if (!toast || toast.leaving) return;

    this.toastsSubject.next(
      current.map(t => t.id === id ? { ...t, leaving: true } : t)
    );

    setTimeout(() => {
      this.toastsSubject.next(
        this.toastsSubject.value.filter(t => t.id !== id)
      );
    }, this.LEAVE_DURATION_MS);
  }

  private show(message: string, type: ToastType, durationMs: number): number {
    const id = this.nextId++;
    const toast: ToastMessage = { id, message, type, durationMs, leaving: false };

    let current = [...this.toastsSubject.value];

    // If at max visible, evict the oldest non-leaving toast with animation
    const visibleCount = current.filter(t => !t.leaving).length;
    if (visibleCount >= this.MAX_VISIBLE) {
      const oldest = current.find(t => !t.leaving);
      if (oldest) {
        const evictId = oldest.id;
        current = current.map(t => t.id === evictId ? { ...t, leaving: true } : t);
        setTimeout(() => {
          this.toastsSubject.next(
            this.toastsSubject.value.filter(t => t.id !== evictId)
          );
        }, this.LEAVE_DURATION_MS);
      }
    }

    this.toastsSubject.next([...current, toast]);

    if (durationMs > 0) {
      setTimeout(() => this.dismiss(id), durationMs);
    }

    return id;
  }
}
