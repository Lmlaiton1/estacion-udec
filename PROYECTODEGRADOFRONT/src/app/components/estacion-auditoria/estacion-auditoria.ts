import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { AuditoriaMeteo } from '../../models/AuditoriaMeteo';

// Consulta de la auditoría del dominio meteo: quién hizo qué, cuándo y por qué
// (umbrales, alertas marcadas como vistas, estaciones y sensores)
@Component({
  selector: 'app-estacion-auditoria',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './estacion-auditoria.html',
  styleUrls: ['./estacion-auditoria.scss']
})
export class EstacionAuditoria implements OnInit {
  readonly tipos = [
    { valor: '', texto: 'Todos' },
    { valor: 'Alerta', texto: 'Alertas vistas' },
    { valor: 'Umbral', texto: 'Umbrales' },
    { valor: 'Estacion', texto: 'Estaciones' },
    { valor: 'Sensor', texto: 'Sensores' }
  ];

  auditoria: AuditoriaMeteo[] = [];
  columnas = ['fecha', 'usuarioNombre', 'tipoEntidad', 'nombreEntidad', 'accion', 'detalle', 'motivo'];
  cargando = false;

  // Filtros
  tipo = '';
  usuario = '';
  texto = '';
  desde = '';
  hasta = '';

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this._weatherService.getAuditoriaMeteo().subscribe({
      next: data => {
        this.auditoria = data;
        this.cargando = false;
      },
      error: () => {
        this.cargando = false;
        this._snackBar.open('No se pudo cargar la auditoría', 'Cerrar', { duration: 4000 });
      }
    });
  }

  get usuarios(): string[] {
    return [...new Set(this.auditoria.map(a => a.usuarioNombre).filter(n => !!n))].sort();
  }

  get filtrada(): AuditoriaMeteo[] {
    const texto = this.texto.trim().toLowerCase();
    const desde = this.desde ? new Date(`${this.desde}T00:00:00`).getTime() : null;
    const hasta = this.hasta ? new Date(`${this.hasta}T23:59:59`).getTime() : null;

    return this.auditoria.filter(a => {
      const fecha = new Date(a.fecha).getTime();
      if (this.tipo && a.tipoEntidad !== this.tipo) return false;
      if (this.usuario && a.usuarioNombre !== this.usuario) return false;
      if (desde !== null && fecha < desde) return false;
      if (hasta !== null && fecha > hasta) return false;
      if (texto && ![a.nombreEntidad, a.detalle, a.motivo, a.accion]
        .some(campo => (campo ?? '').toLowerCase().includes(texto))) return false;
      return true;
    });
  }

  limpiarFiltros(): void {
    this.tipo = '';
    this.usuario = '';
    this.texto = '';
    this.desde = '';
    this.hasta = '';
  }

  nombreTipo(tipo: string): string {
    const nombres: Record<string, string> = { Alerta: 'Alerta', Umbral: 'Umbral', Estacion: 'Estación', Sensor: 'Sensor' };
    return nombres[tipo] ?? tipo;
  }

  // Rojo/gris para lo que quita o apaga, verde para el resto
  claseAccion(accion: string): string {
    return ['Desactivada', 'Desactivado', 'Eliminado'].includes(accion) ? 'inactiva' : 'activa';
  }
}
