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
  //errorMessage: string = '';

  constructor(private authService: Authservice, private router: Router, private _snackBar: MatSnackBar,
    private _scheduleService: ScheduleService
  ) {

    if (this.authService.userData) {
      this.router.navigate(['/login'])
    }

  }

  ngOnInit(): void {
    this.cargarConfigYPorts();
    this.refreshInterval = setInterval(() => {
      this.cargarConfigYPorts();
    }, 5000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  cargarConfigYPorts() {

    this._scheduleService.getSerialConfig().subscribe({
      next: (cfg) => {
        this.selectedPort = cfg?.portName || '';
        this.selectedBaudRate = cfg?.baudRate || 9600;

        this._scheduleService.getAvailablePorts().subscribe({
          next: (ports) => {
            this.ports = ports;
            if (!this.ports.includes(this.selectedPort)) {
              this.selectedPort = this.ports[0] || '';
            }
          },
          error: (err) => {
            console.error('Error cargando puertos:', err);
            this._snackBar.open('No se pudieron cargar los puertos disponibles.', 'Cerrar', { duration: 3000 });
          }
        });
      },
      error: (err) => {
        console.error('Error cargando configuración serial:', err);
        this._snackBar.open('No se pudo cargar la configuración serial.', 'Cerrar', { duration: 3000 });
      }
    });
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

          // Redirigir al componente actuator
          this.router.navigate(['/menu']);

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
