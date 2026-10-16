import { Component, OnInit } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexFill, ApexStroke, ApexTooltip, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import { MATERIAL_MODULES } from '../../shared/material';
import { PronosticoService } from '../../service/pronostico/pronostico-service';
import { Authservice } from '../../service/auth/authservice';
import { Rolservice } from '../../service/rol/rolservice';
import { EstadisticaPronostico, PronosticoFranjaMeteo, PronosticoHora, PronosticoMeteo } from '../../models/PronosticoMeteo';

type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  stroke: ApexStroke;
  fill: ApexFill;
  dataLabels: ApexDataLabels;
  tooltip: ApexTooltip;
  colors: string[];
};

interface VariableGrafica {
  clave: 'temperatura' | 'humedad' | 'luz' | 'lluvia';
  etiqueta: string;
  unidad: string;
  decimales: number;
}

// Nombres de días y meses en español para el pipe date (solo este componente lo usa con 'es')
registerLocaleData(localeEs);

// Presentación de cada condición que emite el modelo (predecir.py)
const CONDICIONES: Record<string, { icono: string; clase: string; texto: string }> = {
  'Señal de sol': { icono: 'wb_sunny', clase: 'sol', texto: 'Soleado' },
  'Posible nubosidad': { icono: 'cloud', clase: 'nublado', texto: 'Nublado' },
  'Posible lluvia': { icono: 'water_drop', clase: 'lluvia', texto: 'Posible lluvia' },
  'Cielo no determinado': { icono: 'nights_stay', clase: 'noche', texto: 'Sin lluvia (cielo no determinado)' },
  'Condición variable': { icono: 'filter_drama', clase: 'variable', texto: 'Variable' }
};

@Component({
  selector: 'app-estacion-ia',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, MatProgressBarModule],
  templateUrl: './estacion-ia.html',
  styleUrls: ['./estacion-ia.scss']
})
export class EstacionIa implements OnInit {
  readonly variables: VariableGrafica[] = [
    { clave: 'temperatura', etiqueta: 'Temperatura', unidad: '°C', decimales: 1 },
    { clave: 'humedad', etiqueta: 'Humedad', unidad: '%', decimales: 0 },
    { clave: 'luz', etiqueta: 'Luz', unidad: 'lux', decimales: 0 },
    { clave: 'lluvia', etiqueta: 'Lluvia estimada', unidad: 'mm/h', decimales: 2 }
  ];
  readonly opcionesReales = [
    { valor: 'Soleado', icono: 'wb_sunny' },
    { valor: 'Nublado', icono: 'cloud' },
    { valor: 'Lluvioso', icono: 'water_drop' }
  ];

  rolUsuario: string | null = null;
  pronostico: PronosticoMeteo | null = null;
  franjasRecientes: PronosticoFranjaMeteo[] = [];
  estadisticas: EstadisticaPronostico | null = null;
  variableSeleccionada: VariableGrafica = this.variables[0];
  grafica: ChartOptions | null = null;

  cargando = true;
  generando = false;
  calificando: string | null = null;

  constructor(
    private _pronosticoService: PronosticoService,
    private _authService: Authservice,
    private _rolService: Rolservice,
    private _snackBar: MatSnackBar
  ) {
    const user = this._authService.getTokenUserInfo();
    if (user?.rolId) {
      this._rolService.getRol(user.rolId).subscribe(data => this.rolUsuario = data.name);
    }
  }

  ngOnInit(): void {
    this.cargarPronostico();
    this.cargarCalificaciones();
  }

  // Administrador y Operario pueden reentrenar; el resto solo consulta y califica
  get puedeGenerar(): boolean {
    return this.rolUsuario === 'Administrador' || this.rolUsuario === 'Operario';
  }

  get pendientes(): number {
    return this.franjasRecientes.filter(f => !f.condicionReal).length;
  }

  get faltanParaCalibrar(): number {
    return this.estadisticas ? Math.max(0, this.estadisticas.minimoParaCalibrar - this.estadisticas.calificadas) : 0;
  }

  generar(): void {
    this.generando = true;
    this._pronosticoService.generar().subscribe({
      next: resp => {
        this.generando = false;
        this.pronostico = resp.data as PronosticoMeteo;
        this.construirGrafica();
        this.cargarCalificaciones();
        this._snackBar.open(resp.informationMessage || 'Pronóstico generado', 'Cerrar', { duration: 6000 });
      },
      error: err => {
        this.generando = false;
        this.mostrarError(err?.error?.errorMessage || 'No se pudo generar el pronóstico');
      }
    });
  }

  seleccionarVariable(v: VariableGrafica): void {
    this.variableSeleccionada = v;
    this.construirGrafica();
  }

  calificar(franja: PronosticoFranjaMeteo, condicionReal: string): void {
    this.calificando = franja.id;
    this._pronosticoService.calificar(franja.id, condicionReal).subscribe({
      next: resp => {
        this.calificando = null;
        Object.assign(franja, resp.data as PronosticoFranjaMeteo);
        this.cargarEstadisticas();
        this._snackBar.open(resp.informationMessage, 'Cerrar', { duration: 3000 });
      },
      error: err => {
        this.calificando = null;
        this.mostrarError(err?.error?.errorMessage || 'No se pudo guardar la calificación');
      }
    });
  }

  condicion(texto: string) {
    return CONDICIONES[texto] ?? { icono: 'help_outline', clase: 'variable', texto };
  }

  // Madrugada 0-5, Mañana 6-11, Tarde 12-17, Noche 18-23
  horario(orden: number): string {
    const inicio = orden * 6;
    return `${inicio.toString().padStart(2, '0')}:00 – ${(inicio + 5).toString().padStart(2, '0')}:59`;
  }

  private cargarPronostico(): void {
    this.cargando = true;
    this._pronosticoService.getUltimo().subscribe({
      next: data => {
        this.cargando = false;
        this.pronostico = data;
        this.construirGrafica();
      },
      error: () => {
        this.cargando = false;
        this.mostrarError('No se pudo cargar el último pronóstico');
      }
    });
  }

  private cargarCalificaciones(): void {
    this._pronosticoService.getFranjas(7).subscribe({
      next: data => this.franjasRecientes = data,
      error: () => this.mostrarError('No se pudieron cargar los pronósticos anteriores')
    });
    this.cargarEstadisticas();
  }

  private cargarEstadisticas(): void {
    this._pronosticoService.getEstadisticas().subscribe({
      next: data => this.estadisticas = data,
      error: () => this.estadisticas = null
    });
  }

  // Línea del pronóstico y, cuando existe, la banda de error (percentil 90 de validación)
  private construirGrafica(): void {
    if (!this.pronostico?.horario?.length) {
      this.grafica = null;
      return;
    }
    const v = this.variableSeleccionada;
    const valor = (h: PronosticoHora) => h[v.clave];
    const inferior = (h: PronosticoHora) => v.clave === 'lluvia' ? null : h[(v.clave + 'Inferior') as keyof PronosticoHora] as number | null;
    const superior = (h: PronosticoHora) => v.clave === 'lluvia' ? null : h[(v.clave + 'Superior') as keyof PronosticoHora] as number | null;
    const x = (h: PronosticoHora) => new Date(h.fechaHora).getTime();

    const series: any[] = [];
    if (v.clave !== 'lluvia') {
      series.push({
        type: 'rangeArea',
        name: 'Rango de error',
        data: this.pronostico.horario
          .filter(h => inferior(h) != null && superior(h) != null)
          .map(h => ({ x: x(h), y: [inferior(h), superior(h)] }))
      });
    }
    series.push({
      type: v.clave === 'lluvia' ? 'column' : 'line',
      name: v.etiqueta,
      data: this.pronostico.horario.filter(h => valor(h) != null).map(h => ({ x: x(h), y: valor(h) }))
    });

    this.grafica = {
      series,
      chart: { type: v.clave === 'lluvia' ? 'bar' : 'rangeArea', height: 320, toolbar: { show: true }, animations: { enabled: false } },
      colors: v.clave === 'lluvia' ? ['#1e6fb8'] : ['#9cc5ad', '#1d4d35'],
      fill: { opacity: v.clave === 'lluvia' ? [1] : [0.35, 1] },
      stroke: { curve: 'smooth', width: v.clave === 'lluvia' ? [0] : [0, 2] },
      dataLabels: { enabled: false },
      xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
      yaxis: {
        title: { text: v.unidad },
        min: v.clave === 'humedad' ? 0 : undefined,
        max: v.clave === 'humedad' ? 100 : undefined,
        labels: { formatter: (n: number) => n == null ? '' : n.toFixed(v.decimales) }
      },
      tooltip: { shared: true, x: { format: 'dd/MM HH:mm' } }
    };
  }

  private mostrarError(mensaje: string): void {
    this._snackBar.open(mensaje, 'Cerrar', { duration: 6000 });
  }
}
