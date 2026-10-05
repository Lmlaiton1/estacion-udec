import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MATERIAL_MODULES } from '../../shared/material';
import { Authservice } from '../../service/auth/authservice';

// Tarjeta de módulo que se muestra en la pantalla de inicio
interface ModuloInicio {
  id: 'naturaleza' | 'estacion';
  titulo: string;
  descripcion: string;
  icono: string;
  iconoSecundario: string;
  ruta: string;
  caracteristicas: { icono: string; texto: string }[];
}

@Component({
  selector: 'app-inicio',
  imports: [MATERIAL_MODULES],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss'
})
export class Inicio {

  nombreUsuario = '';

  modulos: ModuloInicio[] = [
    {
      id: 'naturaleza',
      titulo: 'Nueva Naturaleza',
      descripcion: 'Acuaponía: dispositivos, sensores, actuadores y control.',
      icono: 'eco',
      iconoSecundario: 'water_drop',
      ruta: '/menu',
      caracteristicas: [
        { icono: 'sensors', texto: 'Sensores' },
        { icono: 'touch_app', texto: 'Actuadores' },
        { icono: 'fact_check', texto: 'Checklist' }
      ]
    },
    {
      id: 'estacion',
      titulo: 'Estación Meteorológica',
      descripcion: 'Red de estaciones ESP32: monitoreo y predicción de microclima.',
      icono: 'cloud',
      iconoSecundario: 'wb_sunny',
      ruta: '/estacion',
      caracteristicas: [
        { icono: 'thermostat', texto: 'Clima' },
        { icono: 'air', texto: 'Viento' },
        { icono: 'insights', texto: 'Predicción IA' }
      ]
    }
  ];

  constructor(private router: Router, private _authService: Authservice) {
    this.nombreUsuario = (this._authService.getTokenUserInfo()?.name ?? '').toUpperCase();
  }

  entrar(modulo: ModuloInicio): void {
    this.router.navigate([modulo.ruta]);
  }

  cerrarSesion(): void {
    this._authService.logout();
  }
}
