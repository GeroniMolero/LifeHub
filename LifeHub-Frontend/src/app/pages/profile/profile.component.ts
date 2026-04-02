import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;
  loading = false;
  success = '';
  error = '';

  constructor(
    private fb: FormBuilder,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadCurrentUser();
  }

  initForms(): void {
    this.profileForm = this.fb.group({
      fullName: ['', Validators.required],
      bio: [''],
      profilePictureUrl: ['']
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  onProfileSubmit(): void {
    if (this.profileForm.invalid) return;

    this.loading = true;
    this.success = '';
    this.error = '';

    this.userService.updateProfile(this.profileForm.value).subscribe({
      next: (user) => {
        this.profileForm.patchValue({
          fullName: user.fullName || '',
          bio: user.bio || '',
          profilePictureUrl: user.profilePictureUrl || ''
        });
        this.success = 'Perfil actualizado correctamente.';
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || 'No se pudo actualizar el perfil.';
        this.loading = false;
      }
    });
  }

  onPasswordSubmit(): void {
    if (this.passwordForm.invalid) return;

    const newPassword = this.passwordForm.get('newPassword')?.value;
    const confirmPassword = this.passwordForm.get('confirmPassword')?.value;
    if (newPassword !== confirmPassword) {
      this.error = 'Las contraseñas no coinciden.';
      this.success = '';
      return;
    }

    this.loading = true;
    this.success = '';
    this.error = '';

    this.userService.changePassword(
      this.passwordForm.get('currentPassword')?.value,
      newPassword
    ).subscribe({
      next: () => {
        this.success = 'Contraseña actualizada correctamente.';
        this.passwordForm.reset();
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || 'No se pudo actualizar la contraseña.';
        this.loading = false;
      }
    });
  }

  private loadCurrentUser(): void {
    this.loading = true;
    this.userService.getCurrentUser().subscribe({
      next: (user) => {
        this.profileForm.patchValue({
          fullName: user.fullName || '',
          bio: user.bio || '',
          profilePictureUrl: user.profilePictureUrl || ''
        });
        this.loading = false;
      },
      error: () => {
        this.error = 'No se pudo cargar el perfil.';
        this.loading = false;
      }
    });
  }
}
