import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { UmbralMeteo } from '../../models/UmbralMeteo';
import { UmbralMeteoCreate } from '../../models/UmbralMeteoCreate';
import { UmbralMeteoUpdate } from '../../models/UmbralMeteoUpdate';
import { AlertaMeteo } from '../../models/AlertaMeteo';

// Ventana que pide un texto obligatorio antes de ejecutar (motivo o qué se hizo); queda en la auditoría
interface AccionPendiente {
  titulo: string;
  descripcion: string;
  etiqueta: string;
  peligro: boolean;
  texto: string;
  ejecutar: (texto: string) => void;
}

@Component({
  selector: 'app-estacion-umbrales',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './estacion-umbrales.html',
  styleUrls: ['./estacion-umbrales.scss']
})
export class EstacionUmbrales implements OnInit {
  readonly variables = ['temperatura', 'humedad', 'radiacion', 'velocidadViento', 'direccion', 'lluvia'];
  readonly niveles = ['Baja', 'Media', 'Alta'];

  umbrales: UmbralMeteo[] = [];
  columnasUmbrales = ['estacion', 'variable', 'rango', 'nivel', 'activo', 'acciones'];

  alertas: AlertaMeteo[] = [];
  columnasAlertas = ['fechaHora', 'estacion', 'variable', 'mensaje', 'nivel', 'acciones'];
  soloActivas = true;

  formUmbralVisible = false;
  umbralEnEdicion: UmbralMeteo | null = null;
  formUmbral = this.umbralVacio();

  accion: AccionPendiente | null = null;
  enviando = false;

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  ngOnInit(): void {
    this.cargarUmbrales();
    this.cargarAlertas();
  }

  private cargarUmbrales(): void {
    this._weatherService.getUmbrales().subscribe({
      next: data => this.umbrales = data,
      error: () => this.mostrarError('No se pudieron cargar los umbrales')
    });
  }

  private cargarAlertas(): void {
    this._weatherService.getAlertas().subscribe({
      next: data => this.alertas = data.sort((a, b) => new Date(b.fechaHora).getTime() - new Date(a.fechaHora).getTime()),
      error: () => this.mostrarError('No se pudieron cargar las alertas')
    });
  }

  get alertasVisibles(): AlertaMeteo[] {
    return this.soloActivas ? this.alertas.filter(a => !a.visto) : this.alertas;
  }

  toggleSoloActivas(): void {
    this.soloActivas = !this.soloActivas;
  }

  private umbralVacio(): UmbralMeteoCreate {
    return {
      estacionNumero: null,
      variable: this.variables[0],
      valorMin: null,
      valorMax: null,
      nivelGravedad: this.niveles[0],
      activo: true,
      descripcion: '',
      motivo: ''
    };
  }

  nuevoUmbral(): void {
    this.umbralEnEdicion = null;
    this.formUmbral = this.umbralVacio();
    this.formUmbralVisible = true;
  }

  editarUmbral(umbral: UmbralMeteo): void {
    this.umbralEnEdicion = umbral;
    this.formUmbral = {
      estacionNumero: umbral.estacionNumero,
      variable: umbral.variable,
      valorMin: umbral.valorMin,
      valorMax: umbral.valorMax,
      nivelGravedad: umbral.nivelGravedad,
      activo: umbral.activo,
      descripcion: umbral.descripcion ?? '',
      motivo: ''
    };
    this.formUmbralVisible = true;
  }

  cancelarUmbral(): void {
    this.formUmbralVisible = false;
  }

  guardarUmbral(): void {
    if (!this.formUmbral.motivo.trim()) {
      this.mostrarError('Indique el motivo: queda registrado en la auditoría');
      return;
    }
    this.formUmbral.motivo = this.formUmbral.motivo.trim();

    if (this.umbralEnEdicion) {
      const payload: UmbralMeteoUpdate = { ...this.formUmbral };
      this._weatherService.actualizarUmbral(this.umbralEnEdicion.id, payload).subscribe({
        next: () => {
          this._snackBar.open('Umbral actualizado', 'Cerrar', { duration: 3000 });
          this.formUmbralVisible = false;
          this.cargarUmbrales();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo actualizar el umbral'))
      });
    } else {
      this._weatherService.crearUmbral(this.formUmbral).subscribe({
        next: () => {
          this._snackBar.open('Umbral creado', 'Cerrar', { duration: 3000 });
          this.formUmbralVisible = false;
          this.cargarUmbrales();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo crear el umbral'))
      });
    }
  }

  eliminarUmbral(umbral: UmbralMeteo): void {
    this.pedirTexto({
      titulo: `Eliminar el umbral de ${umbral.variable} (${this.nombreEstacion(umbral.estacionNumero)})`,
      descripcion: 'Si el umbral ya generó alertas no se puede eliminar; en ese caso desactívelo.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      peligro: true,
      ejecutar: motivo => this._weatherService.eliminarUmbral(umbral.id, motivo).subscribe({
        next: () => this.terminarAccion('Umbral eliminado', () => this.cargarUmbrales()),
        error: err => this.fallarAccion(this.extraerError(err, 'No se pudo eliminar el umbral'))
      })
    });
  }

  marcarVista(alerta: AlertaMeteo): void {
    this.pedirTexto({
      titulo: 'Marcar alerta como vista',
      descripcion: alerta.mensaje,
      etiqueta: '¿Qué se hizo? (ej.: se revisó la estación, se cubrió el cultivo)',
      peligro: false,
      ejecutar: texto => this._weatherService.marcarVistaAlerta(alerta.id, texto).subscribe({
        next: () => this.terminarAccion('Alerta marcada como vista', () => this.cargarAlertas()),
        error: err => this.fallarAccion(this.extraerError(err, 'No se pudo marcar la alerta como vista'))
      })
    });
  }

  private pedirTexto(accion: Omit<AccionPendiente, 'texto'>): void {
    this.accion = { ...accion, texto: '' };
  }

  confirmarAccion(): void {
    if (!this.accion || !this.accion.texto.trim() || this.enviando) return;
    this.enviando = true;
    this.accion.ejecutar(this.accion.texto.trim());
  }

  cancelarAccion(): void {
    if (!this.enviando) this.accion = null;
  }

  private terminarAccion(mensaje: string, recargar: () => void): void {
    this.enviando = false;
    this.accion = null;
    this._snackBar.open(mensaje, 'Cerrar', { duration: 3000 });
    recargar();
  }

  private fallarAccion(mensaje: string): void {
    this.enviando = false;
    this.mostrarError(mensaje);
  }

  nombreEstacion(numero: number | null): string {
    return numero === null ? 'Todas' : `Estación ${numero}`;
  }

  rango(umbral: UmbralMeteo): string {
    const min = umbral.valorMin ?? '—';
    const max = umbral.valorMax ?? '—';
    return `${min} a ${max}`;
  }

  private extraerError(err: any, mensajePorDefecto: string): string {
    return err?.error?.errorMessage || mensajePorDefecto;
  }

  private mostrarError(mensaje: string): void {
    this._snackBar.open(mensaje, 'Cerrar', { duration: 4000 });
  }
}
