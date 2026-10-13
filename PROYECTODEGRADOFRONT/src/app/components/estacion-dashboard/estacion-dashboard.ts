import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApexAxisChartSeries, ApexChart, ApexTitleSubtitle, ApexXAxis, ApexYAxis } from 'ng-apexcharts';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { LecturaMeteo } from '../../models/LecturaMeteo';
import { EstacionMeteo } from '../../models/EstacionMeteo';

type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  title: ApexTitleSubtitle;
};

interface DefinicionVariable {
  key: string;
  label: string;
}

interface TarjetaVariable {
  key: string;
  label: string;
  valor: number | null;
  valorCardinal: string | null;
  unidad: string;
  fechaHora: string | null;
}

interface GraficaVariable {
  key: string;
  label: string;
  chartOptions: ChartOptions;
}

interface EstadoEstacion {
  numero: number;
  nombre: string;
  activa: boolean;
  ultimaFecha: Date | null;
}

const MINUTOS_ACTIVA = 5;
const HORAS_HISTORICO_DEFECTO = 24;

@Component({
  selector: 'app-estacion-dashboard',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, MatButtonToggleModule],
  templateUrl: './estacion-dashboard.html',
  styleUrls: ['./estacion-dashboard.scss']
})
export class EstacionDashboard implements OnInit, OnDestroy {
  readonly variables: DefinicionVariable[] = [
    { key: 'temperatura', label: 'Temperatura' },
    { key: 'humedad', label: 'Humedad' },
    { key: 'radiacion', label: 'Radiación' },
    { key: 'velocidadViento', label: 'Velocidad del viento' },
    { key: 'direccion', label: 'Dirección del viento' },
    { key: 'lluvia', label: 'Lluvia' },
  ];

  vista: 'estacion' | 'comparativa' = 'estacion';

  estaciones: EstacionMeteo[] = [];
  estadoEstaciones: EstadoEstacion[] = [];

  // Vista 1 - por estación
  estacionSeleccionada = 1;
  private lecturasEstacionRaw: LecturaMeteo[] = [];
  private ultimasLecturas: LecturaMeteo[] = [];
  tarjetas: TarjetaVariable[] = [];
  graficasEstacion: GraficaVariable[] = [];

  // Vista 2 - comparativa
  variableSeleccionada = this.variables[0].key;
  private comparativaRaw: { numero: number; nombre: string; lecturas: LecturaMeteo[] }[] = [];
  graficaComparativa: ChartOptions | null = null;

  // Filtro de fechas (aplicado en el cliente)
  fechaInicio: Date | null = null;
  fechaFin: Date | null = null;

  cargando = false;

  private refreshInterval: any;

  constructor(private _weatherService: WeatherService) { }

  ngOnInit(): void {
    this._weatherService.getEstaciones().pipe(
      catchError(() => of([] as EstacionMeteo[]))
    ).subscribe(estaciones => {
      this.estaciones = estaciones;
      this.actualizar();
    });

    this.refreshInterval = setInterval(() => this.actualizar(), 30000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  cambiarVista(vista: 'estacion' | 'comparativa'): void {
    this.vista = vista;
    this.actualizar();
  }

  cambiarEstacionSeleccionada(numero: number): void {
    this.estacionSeleccionada = numero;
    this.procesarTarjetas();
    this.cargarLecturasEstacion();
  }

  cambiarVariableSeleccionada(variable: string): void {
    this.variableSeleccionada = variable;
    this.cargarComparativa();
  }

  aplicarFiltroFechas(): void {
    // El rango se manda al backend: hay que volver a pedir los datos, no solo reprocesar en el cliente.
    if (this.vista === 'estacion') {
      this.cargarLecturasEstacion();
    } else {
      this.cargarComparativa();
    }
  }

  limpiarFiltroFechas(): void {
    this.fechaInicio = null;
    this.fechaFin = null;
    this.aplicarFiltroFechas();
  }

  actualizar(): void {
    this.cargando = true;
    this.cargarEstadoEstaciones();

    if (this.vista === 'estacion') {
      this.cargarLecturasEstacion();
    } else {
      this.cargarComparativa();
    }
  }

  nombreEstacion(numero: number): string {
    return this.estaciones.find(e => e.numero === numero)?.nombre ?? `Estación ${numero}`;
  }

  private numerosEstaciones(): number[] {
    return this.estaciones.length ? this.estaciones.map(e => e.numero) : [1, 2, 3];
  }

  private cargarEstadoEstaciones(): void {
    this._weatherService.getUltimasLecturas().pipe(
      catchError(() => of([] as LecturaMeteo[]))
    ).subscribe(lecturas => {
      this.ultimasLecturas = lecturas;

      const numeros = this.numerosEstaciones();
      this.estadoEstaciones = numeros.map(numero => {
        const deLaEstacion = lecturas.filter(l => l.estacionNumero === numero);
        const ultimaFecha = this.ultimaFechaDe(deLaEstacion);
        const activa = ultimaFecha !== null && (Date.now() - ultimaFecha.getTime()) < MINUTOS_ACTIVA * 60 * 1000;

        return { numero, nombre: this.nombreEstacion(numero), activa, ultimaFecha };
      });

      this.procesarTarjetas();
    });
  }

  private ultimaFechaDe(lecturas: LecturaMeteo[]): Date | null {
    if (!lecturas.length) return null;

    const maxMs = lecturas.reduce((max, l) => {
      const t = new Date(l.fechaHora).getTime();
      return t > max ? t : max;
    }, 0);

    return maxMs > 0 ? new Date(maxMs) : null;
  }

  private cargarLecturasEstacion(): void {
    const { desde, hasta } = this.rangoEfectivo();

    this._weatherService.getByEstacion(this.estacionSeleccionada, new Date(desde), new Date(hasta)).pipe(
      catchError(() => of([] as LecturaMeteo[]))
    ).subscribe(lecturas => {
      this.lecturasEstacionRaw = lecturas;
      this.procesarGraficas();
      this.cargando = false;
    });
  }

  // Las tarjetas muestran el último valor real (viene de /UltimasLecturas), sin importar el filtro de fechas.
  private procesarTarjetas(): void {
    this.tarjetas = this.variables.map(v => {
      const ultima = this.ultimasLecturas.find(l => l.estacionNumero === this.estacionSeleccionada && l.variable === v.key) ?? null;

      return {
        key: v.key,
        label: v.label,
        valor: ultima ? ultima.valor : null,
        valorCardinal: ultima ? ultima.valorCardinal : null,
        unidad: ultima ? ultima.unidad : '',
        fechaHora: ultima ? ultima.fechaHora : null
      };
    });
  }

  // lecturasEstacionRaw ya viene filtrada por fecha desde el backend, acá solo se separa por variable.
  private procesarGraficas(): void {
    this.graficasEstacion = this.variables.map(v => {
      const datos = this.lecturasEstacionRaw
        .filter(l => l.variable === v.key)
        .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime());

      const unidad = datos.length ? datos[0].unidad : '';

      return {
        key: v.key,
        label: v.label,
        chartOptions: this.construirChart(v.label, unidad, datos)
      };
    });
  }

  private cargarComparativa(): void {
    const numeros = this.numerosEstaciones();
    const { desde, hasta } = this.rangoEfectivo();

    forkJoin(
      numeros.map(numero =>
        this._weatherService.getByEstacionAndVariable(numero, this.variableSeleccionada, new Date(desde), new Date(hasta))
          .pipe(catchError(() => of([] as LecturaMeteo[])))
      )
    ).subscribe(resultados => {
      this.comparativaRaw = numeros.map((numero, i) => ({
        numero,
        nombre: this.nombreEstacion(numero),
        lecturas: resultados[i]
      }));
      this.procesarComparativa();
      this.cargando = false;
    });
  }

  // comparativaRaw ya viene filtrada por fecha desde el backend.
  private procesarComparativa(): void {
    const label = this.variables.find(v => v.key === this.variableSeleccionada)?.label ?? this.variableSeleccionada;

    let unidad = '';
    const series: ApexAxisChartSeries = this.comparativaRaw.map(estacion => {
      const datos = [...estacion.lecturas].sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime());

      if (!unidad && datos.length) unidad = datos[0].unidad;

      return {
        name: estacion.nombre,
        data: datos.map(l => ({ x: new Date(l.fechaHora).getTime(), y: l.valor }))
      } as any;
    });

    this.graficaComparativa = {
      series,
      chart: { type: 'line', height: 400, toolbar: { show: true }, animations: { enabled: false } },
      xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
      yaxis: { title: { text: unidad } },
      title: { text: `Comparativa — ${label}`, align: 'center' }
    };
  }

  private construirChart(label: string, unidad: string, lecturas: LecturaMeteo[]): ChartOptions {
    const datos = lecturas.map(l => ({ x: new Date(l.fechaHora).getTime(), y: l.valor }));

    return {
      series: [{ name: label, data: datos } as any],
      chart: { type: 'line', height: 250, toolbar: { show: true }, animations: { enabled: false } },
      xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
      yaxis: { title: { text: unidad } },
      title: { text: label, align: 'left' }
    };
  }

  // Rango efectivo de las gráficas: el filtro elegido por el usuario, o las últimas 24h por defecto.
  private rangoEfectivo(): { desde: number; hasta: number } {
    if (this.fechaInicio && this.fechaFin) {
      const desde = new Date(this.fechaInicio);
      desde.setHours(0, 0, 0, 0);
      const hasta = new Date(this.fechaFin);
      hasta.setHours(23, 59, 59, 999);
      return { desde: desde.getTime(), hasta: hasta.getTime() };
    }

    const hasta = Date.now();
    const desde = hasta - HORAS_HISTORICO_DEFECTO * 60 * 60 * 1000;
    return { desde, hasta };
  }

  private dentroDelRango(fechaHora: string, desde: number, hasta: number): boolean {
    const t = new Date(fechaHora).getTime();
    return t >= desde && t <= hasta;
  }

  usaFiltroPersonalizado(): boolean {
    return !!(this.fechaInicio && this.fechaFin);
  }

  exportarCsv(): void {
    const estacion = this.nombreEstacion(this.estacionSeleccionada);
    const fechaExportacion = new Date().toLocaleString('es-CO');
    const rango = this.usaFiltroPersonalizado()
      ? `${this.fechaInicio!.toLocaleDateString('es-CO')} a ${this.fechaFin!.toLocaleDateString('es-CO')}`
      : `Últimas ${HORAS_HISTORICO_DEFECTO} horas`;

    const encabezado = [
      `Estación: ${estacion} (N°${this.estacionSeleccionada})`,
      `Fecha de exportación: ${fechaExportacion}`,
      `Rango: ${rango}`,
      '',
      'FechaHora,Variable,Valor,ValorCardinal,Unidad'
    ];

    const { desde, hasta } = this.rangoEfectivo();
    const filas = this.lecturasEstacionRaw
      .filter(l => this.dentroDelRango(l.fechaHora, desde, hasta))
      .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime())
      .map(l => `${l.fechaHora},${l.variable},${l.valor},${l.valorCardinal ?? ''},${l.unidad}`);

    const contenido = [...encabezado, ...filas].join('\n');
    const blob = new Blob([contenido], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);

    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = `estacion-${this.estacionSeleccionada}-lecturas.csv`;
    enlace.click();

    URL.revokeObjectURL(url);
  }
}
