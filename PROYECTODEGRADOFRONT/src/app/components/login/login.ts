import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MATERIAL_MODULES } from '../../shared/material';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Authservice } from '../../service/auth/authservice';
import { Login as LoginModel } from '../../models/Login';
import { RouterLink } from '@angular/router';
import { ScheduleService } from '../../service/schedule/schedule-service';
import { SerialPortConfig } from '../../models/SerialPortConfig';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, MATERIAL_MODULES, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login implements OnInit, OnDestroy {
  user: string = '';
  password: string = '';
  ports: string[] = [];
  selectedPort: string = '';
  baudRates: number[] = [9600, 19200, 38400, 57600, 115200];
  selectedBaudRate: number = 9600;
  serialPort!: SerialPortConfig;
  private refreshInterval: any;
  // Evita repetir el aviso de error cada 5 segundos: se muestra una vez hasta que vuelva a funcionar
  private avisoErrorMostrado = false;
  //errorMessage: string = '';

  constructor(private authService: Authservice, private router: Router, private _snackBar: MatSnackBar,
    private _scheduleService: ScheduleService
  ) {

    if (this.authService.userData) {
      this.router.navigate(['/login'])
    }

  }

  ngOnInit(): void {
    this.cargarConfig();
    // Solo se refresca la lista de puertos (ej. al conectar el USB); la selección del usuario se respeta
    this.refreshInterval = setInterval(() => {
      this.cargarPuertos();
    }, 5000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  // Configuración guardada: se carga una sola vez al abrir el login
  cargarConfig() {
    this._scheduleService.getSerialConfig().subscribe({
      next: (cfg) => {
        this.selectedPort = cfg?.portName || '';
        this.selectedBaudRate = cfg?.baudRate || 9600;
        this.cargarPuertos();
      },
      error: (err) => {
        console.error('Error cargando configuración serial:', err);
        this.avisarErrorUnaVez('No se pudo cargar la configuración serial.');
        this.cargarPuertos();
      }
    });
  }

  cargarPuertos() {
    this._scheduleService.getAvailablePorts().subscribe({
      next: (ports) => {
        this.ports = ports;
        this.avisoErrorMostrado = false;
        if (!this.ports.includes(this.selectedPort)) {
          this.selectedPort = this.ports[0] || '';
        }
      },
      error: (err) => {
        console.error('Error cargando puertos:', err);
        this.avisarErrorUnaVez('No se pudieron cargar los puertos disponibles.');
      }
    });
  }

  private avisarErrorUnaVez(mensaje: string) {
    if (this.avisoErrorMostrado) return;
    this.avisoErrorMostrado = true;
    this._snackBar.open(mensaje, 'Cerrar', { duration: 3000 });
  }

  guardarConfigSerial(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.serialPort = {
        portName: this.selectedPort,
        baudRate: this.selectedBaudRate
      }
      this._scheduleService.saveSerialConfig(this.serialPort).subscribe({
        next: () => {

          resolve();
          this._snackBar.open('✅ Configuración guardada exitosamente', 'Cerrar', {
            duration: 3000
          });

        },
        error: (err) => {
          console.error('Error guardando configuración serial:', err);
          this._snackBar.open('No se pudo guardar la configuración del puerto.', 'Cerrar', { duration: 3000 });
          reject(err);
        }
      });
    });
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
