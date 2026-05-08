import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { filter, take } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [],
  template: ''
})
export class ProfileComponent implements OnInit {
  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.authService.getCurrentUser().pipe(
      filter(u => !!u?.id),
      take(1)
    ).subscribe(user => {
      this.router.navigate(['/profile', user!.id], { replaceUrl: true });
    });
  }
}
