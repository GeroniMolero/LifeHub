import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';

import { ToastMessage, ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.scss']
})
export class ToastContainerComponent {
  readonly toasts$: Observable<ToastMessage[]>;
  readonly TRUNCATE_LIMIT = 120;
  private readonly expandedIds = new Set<number>();

  constructor(private toastService: ToastService) {
    this.toasts$ = this.toastService.toasts$;
  }

  trackById(_index: number, toast: ToastMessage): number {
    return toast.id;
  }

  isLong(message: string): boolean {
    return message.length > this.TRUNCATE_LIMIT;
  }

  isExpanded(id: number): boolean {
    return this.expandedIds.has(id);
  }

  toggleExpand(id: number): void {
    if (this.expandedIds.has(id)) {
      this.expandedIds.delete(id);
    } else {
      this.expandedIds.add(id);
    }
  }

  dismiss(id: number): void {
    this.expandedIds.delete(id);
    this.toastService.dismiss(id);
  }
}
