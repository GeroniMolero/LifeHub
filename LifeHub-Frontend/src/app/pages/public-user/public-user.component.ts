import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { User } from '../../models/auth.model';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-public-user',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-user.component.html',
  styleUrls: ['./public-user.component.scss']
})
export class PublicUserComponent implements OnInit {
  user: User | null = null;
  loading = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (!userId) {
      this.error = 'Usuario no válido.';
      return;
    }

    this.loadUser(userId);
  }

  private loadUser(userId: string): void {
    this.loading = true;
    this.error = '';

    this.userService.getUser(userId).subscribe({
      next: user => {
        this.user = user;
        this.loading = false;
      },
      error: err => {
        this.error = err?.error?.message || 'No se pudo cargar el perfil del usuario.';
        this.loading = false;
      }
    });
  }
}
