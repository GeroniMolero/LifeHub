import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/auth.model';
import { CreativeSpace } from '../../models/creative-space.model';
import { Document } from '../../models/document.model';
import { LayoutHeaderAction, LayoutHeaderState, LayoutHeaderStateService } from '../../services/layout-header-state.service';
import { LayoutHeaderComponent } from './components/layout-header/layout-header.component';
import { LayoutSidebarComponent } from './components/layout-sidebar/layout-sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, LayoutHeaderComponent, LayoutSidebarComponent],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent implements OnInit {
  currentUser: User | null = null;
  isSidebarOpen = false;
  headerTitle = 'LifeHub';
  headerDescription = 'Panel principal';
  headerMeta: string[] = [];
  headerActions: LayoutHeaderAction[] = [];
  private routeHeaderTitle = 'LifeHub';
  private routeHeaderDescription = 'Panel principal';
  private headerOverride: LayoutHeaderState | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private layoutHeaderStateService: LayoutHeaderStateService
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser().subscribe(user => {
      this.currentUser = user;
    });

    if (this.authService.isAuthenticated()) {
      this.authService.refreshCurrentUser().subscribe({
        error: () => {
          // Si falla el refresco, se conserva el usuario cargado desde storage.
        }
      });
    }

    this.layoutHeaderStateService.headerOverride$.subscribe(override => {
      this.headerOverride = override;
      this.applyHeaderState();
    });

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => this.updateHeaderFromRoute());

    this.updateHeaderFromRoute();
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }

  logout(): void {
    this.authService.logout();
    this.isSidebarOpen = false;
    this.router.navigate(['/login']);
  }

  private updateHeaderFromRoute(): void {
    let route = this.activatedRoute;

    while (route.firstChild) {
      route = route.firstChild;
    }

    const data = route.snapshot.data;
    const resolvedSpace = data['space'] as CreativeSpace | undefined;
    const resolvedDocument = data['document'] as Document | undefined;

    if (resolvedSpace) {
      this.routeHeaderTitle = `${resolvedSpace.name}`;
      this.routeHeaderDescription = resolvedSpace.description?.trim() || data['headerDescription'] || 'Panel principal';
      this.applyHeaderState();
      return;
    }

    if (resolvedDocument) {
      this.routeHeaderTitle = `${resolvedDocument.title}`;
      this.routeHeaderDescription = resolvedDocument.description?.trim() || data['headerDescription'] || 'Panel principal';
      this.applyHeaderState();
      return;
    }

    this.routeHeaderTitle = data['headerTitle'] || 'LifeHub';
    this.routeHeaderDescription = data['headerDescription'] || 'Panel principal';
    this.applyHeaderState();
  }

  private applyHeaderState(): void {
    this.headerTitle = this.headerOverride?.title || this.routeHeaderTitle;
    this.headerDescription = this.headerOverride?.description || this.routeHeaderDescription;
    this.headerMeta = this.headerOverride?.meta || [];
    this.headerActions = this.headerOverride?.actions || [];
  }
}