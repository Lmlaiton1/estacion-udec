import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MATERIAL_MODULES } from '../../shared/material';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Authservice } from '../../service/auth/authservice';
import { Login as LoginModel } from '../../models/Login';
import { IForgetPassword } from '../../models/IForgetPassword';

@Component({
  selector: 'app-forget-password',
  imports: [CommonModule, MATERIAL_MODULES, FormsModule],
  templateUrl: './forget-password.html',
  styleUrl: './forget-password.scss'
})
export class ForgetPassword {
  question_Type: number = 0;
  respuesta: string = '';
  id_Card: string = '';
  newPassword: string = '';
  //errorMessage: string = '';

  constructor(private authService: Authservice, private router: Router, private _snackBar: MatSnackBar) {

    if (this.authService.userData) {
      this.router.navigate(['/login'])
    }

  } // Inyecta el router

  volver() {
    this.router.navigate(['/login']);
  }

  recuperar() {

    const recuperar: IForgetPassword = {
      user: this.id_Card,
      question_Type: this.question_Type,
      answer: this.respuesta,
      newPassword: this.newPassword
    };
    this.authService.recuperar(recuperar).subscribe({
      next: (response) => {
        if (response.result == 0) {
          this._snackBar.open(response.informationMessage || 'Operación exitosa', 'Cerrar', {
            duration: 3000,
          });

          setTimeout(() => this.router.navigate(['/login']), 1200);
        }

        else if (response.result == 1) {
          // Error controlado por el backend
          const mensaje = response.errorMessage || 'Ocurrió un error inesperado';
          this._snackBar.open(mensaje, 'Cerrar', {
            duration: 3000,
          });
        }
      },
      error: (err) => {
        // Error inesperado: puede ser texto o un objeto
        const mensaje = typeof err.error === 'string' ? err.error
          : err.error?.errorMessage || 'Error de conexión o error inesperado';

        this._snackBar.open(mensaje, 'Cerrar', {
          duration: 3000,
        });
      }
    });
  }
}
