import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { AdminGuard } from './guards/admin.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { SpaceWorkspaceComponent } from './pages/spaces/space-workspace/space-workspace.component';
import { creativeSpaceResolver } from './resolvers/creative-space.resolver';
import { documentResolver } from './resolvers/document.resolver';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'public/documents/:documentId',
    loadComponent: () => import('./pages/public-document/public-document.component').then(m => m.PublicDocumentComponent)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        redirectTo: 'spaces',
        pathMatch: 'full'
      },
      {
        path: 'home',
        data: {
          headerTitle: 'Inicio',
          headerDescription: 'Resumen general de tu actividad en LifeHub'
        },
        loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent)
      },
      {
        path: 'spaces',
        data: {
          headerTitle: 'Espacios',
          headerDescription: 'Espacios personales para crear contenido'
        },
        loadComponent: () => import('./pages/spaces/spaces-list/spaces-list.component').then(m => m.SpacesListComponent)
      },
      {
        path: 'spaces/:id',
        resolve: {
          space: creativeSpaceResolver
        },
        data: {
          headerTitle: 'Espacio creativo',
          headerDescription: 'Editor y multimedia del espacio seleccionado'
        },
        component: SpaceWorkspaceComponent
      },
      {
        path: 'profile',
        data: {
          headerTitle: 'Perfil',
          headerDescription: 'Gestiona tu información personal y preferencias'
        },
        loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent)
      },
      {
        path: 'documents',
        data: {
          headerTitle: 'Documentos',
          headerDescription: 'Crea, edita, versiona y publica tus documentos'
        },
        loadComponent: () => import('./pages/documents/documents-manager/documents-manager.component').then(m => m.DocumentsComponent)
      },
      {
        path: 'documents/:id',
        resolve: {
          document: documentResolver
        },
        data: {
          headerTitle: 'Documento',
          headerDescription: 'Edición, versiones y publicación del documento'
        },
        loadComponent: () => import('./pages/documents/document-detail/document-detail.component').then(m => m.DocumentDetailComponent)
      },
      {
        path: 'admin',
        canActivate: [AdminGuard],
        data: {
          headerTitle: 'Administración',
          headerDescription: 'Panel de gestión y configuración de la plataforma'
        },
        loadComponent: () => import('./pages/admin/admin.component').then(m => m.AdminComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/home'
  }
];
