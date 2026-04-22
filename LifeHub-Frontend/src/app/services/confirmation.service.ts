import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ConfirmationService {
  confirmAction(message: string): boolean {
    return window.confirm(message);
  }

  confirmDelete(targetDescription: string): boolean {
    return this.confirmAction(`¿Seguro que quieres eliminar ${targetDescription}?`);
  }
}
