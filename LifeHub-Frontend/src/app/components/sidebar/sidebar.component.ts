import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent implements OnInit {
  navItems: NavItem[] = [];
  isMobileOpen = false;

  ngOnInit(): void {
    this.initializeNavItems();
  }

  private initializeNavItems(): void {
    this.navItems = [
      { label: 'Inicio', path: '/home', icon: '🏠' },
      { label: 'Mi Perfil', path: '/profile', icon: '👤' },
      { label: 'Amigos', path: '/friends', icon: '👥' },
      { label: 'Chat', path: '/chat', icon: '💬' },
      { label: 'Recomendaciones', path: '/recommendations', icon: '⭐' },
      { label: 'Documentos', path: '/documents', icon: '📄' },
      { label: 'Música', path: '/music', icon: '🎵' }
    ];
  }

  toggleMobileSidebar(): void {
    this.isMobileOpen = !this.isMobileOpen;
  }

  closeMobileSidebar(): void {
    this.isMobileOpen = false;
  }
}
