import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApexAnnotations, ApexAxisChartSeries, ApexChart, ApexDataLabels, ApexStroke, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { EstadisticaMeteo } from '../../models/EstadisticaMeteo';

const NUMERO_CLASES = 10;

@Component({
  selector: 'app-estacion-estadistica',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './estacion-estadistica.html',
  styleUrls: ['./estacion-estadistica.scss']
})
export class EstacionEstadistica {
  // La dirección del viento queda por fuera: es una variable angular (requiere sin/cos)
  readonly variables = [
    { key: 'temperatura', label: 'Temperatura' },
    { key: 'humedad', label: 'Humedad' },
    { key: 'radiacion', label: 'Radiación' },
    { key: 'velocidadViento', label: 'Velocidad del viento' },
    { key: 'lluvia', label: 'Lluvia' },
  ];

  readonly cantidadesRapidas = [50, 100, 500, 1000];

  estacionNumero = 1;
  variable = 'temperatura';
  cantidad = 100;

  cargando = false;
  resultado: EstadisticaMeteo | null = null;

  // Histograma (distribución de frecuencias)
  histogramaSeries: ApexAxisChartSeries = [];
  histogramaChart: ApexChart = { type: 'bar', height: 300, toolbar: { show: false } };
  histogramaXAxis: ApexXAxis = { categories: [], title: { text: '' } };
  histogramaYAxis: ApexYAxis = { title: { text: 'Frecuencia' } };
  histogramaDataLabels: ApexDataLabels = { enabled: false };

  // Serie de valores con líneas de media y mediana
  serieSeries: ApexAxisChartSeries = [];
  serieChart: ApexChart = { type: 'line', height: 300, toolbar: { show: false }, zoom: { enabled: false } };
  serieXAxis: ApexXAxis = { title: { text: 'N.º de lectura' }, tickAmount: 10 };
  serieYAxis: ApexYAxis = { title: { text: '' } };
  serieStroke: ApexStroke = { width: 2, curve: 'straight' };
  serieAnnotations: ApexAnnotations = {};

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  get variableLabel(): string {
    return this.variables.find(v => v.key === this.variable)?.label ?? this.variable;
  }

  elegirCantidad(c: number): void {
    this.cantidad = c;
  }

  calcular(): void {
    if (!this.cantidad || this.cantidad < 1 || this.cantidad > 10000) {
      this._snackBar.open('La cantidad de datos debe estar entre 1 y 10000', 'Cerrar', { duration: 3000 });
      return;
    }

    this.cargando = true;
    this._weatherService.getEstadisticas(this.estacionNumero, this.variable, this.cantidad).subscribe({
      next: (data) => {
        this.cargando = false;
        this.resultado = data;
        if (data.cantidadUsada === 0) {
          this._snackBar.open('No hay lecturas para esa estación y variable', 'Cerrar', { duration: 3000 });
          return;
        }
        this.construirGraficas(data);
      },
      error: () => {
        this.cargando = false;
        this.resultado = null;
        this._snackBar.open('No se pudieron calcular las estadísticas', 'Cerrar', { duration: 4000 });
      }
    });
  }

  textoModa(r: EstadisticaMeteo): string {
    if (r.modas.length === 0) return 'Sin moda';
    return r.modas.map(m => m.toFixed(1)).join(' · ');
  }

  private construirGraficas(r: EstadisticaMeteo): void {
    const unidad = r.unidad ? ` (${r.unidad})` : '';

    // Histograma: se divide el rango en clases de igual ancho
    const min = r.minimo ?? 0;
    const max = r.maximo ?? 0;
    const clases = max === min ? 1 : NUMERO_CLASES;
    const ancho = max === min ? 1 : (max - min) / clases;
    const frecuencias = new Array(clases).fill(0);
    for (const v of r.valores) {
      // El valor máximo cae en la última clase
      const i = Math.min(Math.floor((v - min) / ancho), clases - 1);
      frecuencias[i]++;
    }
    const etiquetas = frecuencias.map((_, i) => {
      if (max === min) return min.toFixed(1);
      const desde = min + i * ancho;
      return `${desde.toFixed(1)} – ${(desde + ancho).toFixed(1)}`;
    });

    this.histogramaSeries = [{ name: 'Frecuencia', data: frecuencias }];
    this.histogramaXAxis = { categories: etiquetas, title: { text: `${this.variableLabel}${unidad}` } };

    // Serie cronológica con media y mediana como líneas horizontales
    this.serieSeries = [{ name: this.variableLabel, data: r.valores.map((v, i) => [i + 1, v]) }];
    this.serieYAxis = { title: { text: `${this.variableLabel}${unidad}` } };
    this.serieAnnotations = {
      yaxis: [
        {
          y: r.media ?? 0,
          borderColor: '#1d4d35',
          strokeDashArray: 0,
          label: { text: `Media ${r.media}`, style: { background: '#1d4d35', color: '#fff' } }
        },
        {
          y: r.mediana ?? 0,
          borderColor: '#e67e22',
          strokeDashArray: 6,
          label: { text: `Mediana ${r.mediana}`, position: 'left', textAnchor: 'start', style: { background: '#e67e22', color: '#fff' } }
        }
      ]
    };
  }
}
