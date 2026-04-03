import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { LayoutHeaderAction } from '../../../../services/layout-header-state.service';

@Component({
  selector: 'app-layout-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './layout-header.component.html',
  styleUrls: ['./layout-header.component.scss']
})
export class LayoutHeaderComponent {
  @Input() headerTitle = 'LifeHub';
  @Input() headerDescription = 'Panel principal';
  @Input() headerMeta: string[] = [];
  @Input() headerActions: LayoutHeaderAction[] = [];
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  constructor(private router: Router) {}

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  onLogout(): void {
    this.logout.emit();
  }

  onHeaderAction(action: LayoutHeaderAction): void {
    action.action?.();

    if (action.route) {
      const target = Array.isArray(action.route) ? action.route : [action.route];
      this.router.navigate(target);
    }
  }
}