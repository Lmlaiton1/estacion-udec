import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MATERIAL_MODULES } from '../../shared/material';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Authservice } from '../../service/auth/authservice';
import { Login as LoginModel } from '../../models/Login';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, MATERIAL_MODULES, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  user: string = '';
  password: string = '';
  //errorMessage: string = '';

  constructor(private authService: Authservice, private router: Router, private _snackBar: MatSnackBar) {

    if (this.authService.userData) {
      this.router.navigate(['/login'])
    }

  }

  login() {

    const loginData: LoginModel = {
      user: this.user,
      password: this.password
    };
    //console.log(loginData); // Imprimir la contraseña
    // lógica de autenticación futura
    // Llamamos al servicio y le pasamos la contraseña
    this.authService.login(loginData).subscribe({
      next: (response) => {
        if (response.result == 0) {

          // Redirigir a la pantalla de selección de dominio
          this.router.navigate(['/inicio']);

        }
        if (response.result == 1) {
          this._snackBar.open(response.errorMessage, 'Cerrar', {
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
