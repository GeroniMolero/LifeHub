import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { User } from '../../../../models/auth.model';


@Component({
  selector: 'app-layout-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './layout-sidebar.component.html',
  styleUrls: ['./layout-sidebar.component.scss']
})
export class LayoutSidebarComponent {
  @Input() isOpen = false;
  @Input() currentUser: User | null = null;
  @Input() chatUnreadCount = 0;
  @Output() closeSidebar = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  readonly navigationItems = [
    {
      label: 'Inicio',
      route: '/home',
      iconPath: 'M3 10.5 12 3l9 7.5M6.75 8.25V21h10.5V8.25'
    },
    {
      label: 'Espacios',
      route: '/spaces',
      iconPath: 'M4.5 7.5h15v10.5h-15zM8.25 4.5h7.5v3h-7.5zM8.25 18h7.5v1.5h-7.5z'
    },
    {
      label: 'Documentos',
      route: '/documents',
      iconPath: 'M7.5 3.75h6l4.5 4.5V20.25H7.5zM13.5 3.75v4.5H18M9.75 12h6.75M9.75 15h6.75'
    },
    {
      label: 'Social',
      route: '/social',
      iconPath: 'M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 01-.825-.242m9.345-8.334a2.126 2.126 0 00-.476-.095 48.64 48.64 0 00-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0011.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155'
    },
    {
      label: 'Admin',
      route: '/admin',
      iconPath: 'M4.5 6.75h15v10.5h-15zM9 10.5h6M9 13.5h3',
      requiredPermission: 'admin.users.view'
    }
  ];

  get visibleNavigationItems() {
    return this.navigationItems.filter(item => {
      if (!item.requiredPermission) {
        return true;
      }

      const roles = this.currentUser?.roles ?? [];
      if (roles.some(r => r.toLowerCase() === 'admin')) {
        return true;
      }

      const claims = this.currentUser?.claims ?? [];
      return claims.includes(`permission:${item.requiredPermission}`);
    });
  }

  close(): void {
    this.closeSidebar.emit();
  }

  onLogout(): void {
    this.logout.emit();
    this.close();
  }
}