import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { EstacionMeteo } from '../../models/EstacionMeteo';
import { EstadoSd, FinEnvioSd, MensajeSd } from '../../models/SdMeteo';

interface TarjetaSd {
  estacion: EstacionMeteo;
  estado: EstadoSd | null;
  esperando: boolean;          // se pidió el estado y aún no responde
  sinRespuesta: boolean;       // pasó el tiempo de espera sin respuesta
  recuperando: string | null;  // día que la placa está enviando
  resultado: FinEnvioSd | null;
  fechaManual: string;
}

// Lo que cada estación guardó en su tarjeta SD: estado de la SD y el RTC, días guardados
// y botón para cargar un día completo a la base de datos (sin duplicar lo que ya está).
@Component({
  selector: 'app-estacion-historico-sd',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './estacion-historico-sd.html',
  styleUrls: ['./estacion-historico-sd.scss']
})
export class EstacionHistoricoSd implements OnInit, OnDestroy {
  private readonly ESPERA_ESTADO_MS = 10000;

  tarjetas: TarjetaSd[] = [];
  cargando = false;
  private suscripcion?: Subscription;
  private temporizador?: ReturnType<typeof setTimeout>;

  private readonly errores: Record<string, string> = {
    no_existe: 'ese día no está en la SD',
    sin_sd: 'la estación no tiene SD disponible',
    ocupado: 'la estación ya está enviando otro día',
    fecha_invalida: 'fecha inválida',
    error_lectura: 'no se pudo leer el archivo de la SD'
  };

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  ngOnInit(): void {
    this.suscripcion = this._weatherService.sd$.subscribe(m => this.recibir(m));

    this.cargando = true;
    this._weatherService.getEstaciones().subscribe({
      next: estaciones => {
        this.tarjetas = estaciones
          .filter(e => e.activa)
          .sort((a, b) => a.numero - b.numero)
          .map(estacion => ({
            estacion, estado: null, esperando: false, sinRespuesta: false,
            recuperando: null, resultado: null, fechaManual: ''
          }));
        this.cargando = false;
        this._weatherService.iniciarConexionWeather().then(() => this.actualizar());
      },
      error: () => {
        this.cargando = false;
        this._snackBar.open('No se pudieron cargar las estaciones', 'Cerrar', { duration: 4000 });
      }
    });
  }

  ngOnDestroy(): void {
    this.suscripcion?.unsubscribe();
    if (this.temporizador) clearTimeout(this.temporizador);
  }

  actualizar(): void {
    this.tarjetas.forEach(t => { t.esperando = true; t.sinRespuesta = false; });
    this._weatherService.solicitarEstadoSd().subscribe({
      error: () => {
        this.tarjetas.forEach(t => t.esperando = false);
        this._snackBar.open('No se pudo pedir el estado de las SD', 'Cerrar', { duration: 4000 });
      }
    });

    // Las placas apagadas o sin conexión al broker no responden
    if (this.temporizador) clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => {
      this.tarjetas.filter(t => t.esperando).forEach(t => { t.esperando = false; t.sinRespuesta = true; });
    }, this.ESPERA_ESTADO_MS);
  }

  recuperar(t: TarjetaSd, dia: string): void {
    if (!dia) return;
    t.recuperando = dia;
    t.resultado = null;
    this._weatherService.recuperarDiaSd(t.estacion.id, dia).subscribe({
      error: err => {
        t.recuperando = null;
        this._snackBar.open(err?.error?.errorMessage ?? 'No se pudo pedir el día a la estación', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private recibir(m: MensajeSd): void {
    const t = this.tarjetas.find(x => x.estacion.numero === m.e);
    if (!t) return;

    if (m.tipo === 'estado') {
      t.estado = m;
      t.esperando = false;
      t.sinRespuesta = false;
      if (m.enviandoDia) t.recuperando = m.enviandoDia;
      return;
    }

    // fin_envio
    t.recuperando = null;
    t.resultado = m;
    const texto = m.error
      ? `${t.estacion.nombre}: ${this.errores[m.error] ?? m.error}`
      : `${t.estacion.nombre}, ${m.dia}: ${m.nuevas ?? 0} lecturas nuevas en la BD (${m.duplicadas ?? 0} ya estaban)`;
    this._snackBar.open(texto, 'Cerrar', { duration: 6000 });
  }

  textoError(codigo?: string): string {
    return codigo ? (this.errores[codigo] ?? codigo) : '';
  }

  tamano(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  porcentajeUso(e: EstadoSd): number {
    if (!e.totalMB) return 0;
    return Math.min(100, Math.round(((e.usadoMB ?? 0) / e.totalMB) * 100));
  }
}
