import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-layout-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './layout-sidebar.component.html',
  styleUrls: ['./layout-sidebar.component.scss']
})
export class LayoutSidebarComponent {
  @Input() isOpen = false;
  @Output() closeSidebar = new EventEmitter<void>();

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
      label: 'Perfil',
      route: '/profile',
      iconPath: 'M12 12a3.75 3.75 0 1 0 0-7.5 3.75 3.75 0 0 0 0 7.5ZM5.25 19.5a6.75 6.75 0 0 1 13.5 0'
    }
  ];

  close(): void {
    this.closeSidebar.emit();
  }
}