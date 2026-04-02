import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { User } from '../../../../models/auth.model';

@Component({
  selector: 'app-layout-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './layout-header.component.html',
  styleUrls: ['./layout-header.component.scss']
})
export class LayoutHeaderComponent {
  @Input() currentUser: User | null = null;
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  isProfileMenuOpen = false;

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  onToggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  onNavigateProfile(): void {
    this.isProfileMenuOpen = false;
  }

  onLogout(): void {
    this.isProfileMenuOpen = false;
    this.logout.emit();
  }
}