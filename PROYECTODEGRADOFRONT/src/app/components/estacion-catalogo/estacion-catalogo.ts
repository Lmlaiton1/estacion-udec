import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { EstacionMeteo } from '../../models/EstacionMeteo';
import { SensorMeteo } from '../../models/SensorMeteo';
import { EstacionMeteoCreate } from '../../models/EstacionMeteoCreate';
import { EstacionMeteoUpdate } from '../../models/EstacionMeteoUpdate';
import { SensorMeteoCreate } from '../../models/SensorMeteoCreate';
import { SensorMeteoUpdate } from '../../models/SensorMeteoUpdate';
import { AuditoriaMeteo } from '../../models/AuditoriaMeteo';

@Component({
  selector: 'app-estacion-catalogo',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, MatButtonToggleModule],
  templateUrl: './estacion-catalogo.html',
  styleUrls: ['./estacion-catalogo.scss']
})
export class EstacionCatalogo implements OnInit {
  vista: 'estaciones' | 'sensores' | 'auditoria' = 'estaciones';

  estaciones: EstacionMeteo[] = [];
  columnasEstaciones = ['numero', 'nombre', 'ubicacion', 'activa', 'acciones'];

  sensores: SensorMeteo[] = [];
  columnasSensores = ['nombre', 'tipo', 'variables', 'activo', 'acciones'];

  auditoria: AuditoriaMeteo[] = [];
  columnasAuditoria = ['fecha', 'tipoEntidad', 'nombreEntidad', 'accion', 'detalle', 'motivo', 'usuarioNombre'];

  // Formulario de estación (null = creando una nueva)
  formEstacionVisible = false;
  estacionEnEdicion: EstacionMeteo | null = null;
  formEstacion = this.estacionVacia();
  fechaInstalacionDate: Date = new Date();

  // Formulario de sensor (null = creando uno nuevo)
  formSensorVisible = false;
  sensorEnEdicion: SensorMeteo | null = null;
  formSensor = this.sensorVacio();

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  ngOnInit(): void {
    this.cargarEstaciones();
    this.cargarSensores();
    this.cargarAuditoria();
  }

  cambiarVista(vista: 'estaciones' | 'sensores' | 'auditoria'): void {
    this.vista = vista;
  }

  // Color de la acción en la auditoría: rojo/gris para lo que quita o apaga, verde para el resto
  claseAccion(accion: string): string {
    return ['Desactivada', 'Desactivado', 'Eliminado'].includes(accion) ? 'inactiva' : 'activa';
  }

  private cargarAuditoria(): void {
    this._weatherService.getAuditoriaMeteo().subscribe({
      next: data => this.auditoria = data,
      error: () => this.mostrarError('No se pudo cargar la auditoría')
    });
  }

  private cargarEstaciones(): void {
    this._weatherService.getEstaciones().subscribe({
      next: data => this.estaciones = data,
      error: () => this.mostrarError('No se pudieron cargar las estaciones')
    });
  }

  private cargarSensores(): void {
    this._weatherService.getSensores().subscribe({
      next: data => this.sensores = data,
      error: () => this.mostrarError('No se pudieron cargar los sensores')
    });
  }

  private estacionVacia(): EstacionMeteoCreate {
    return {
      numero: 1,
      nombre: '',
      ubicacion: '',
      descripcion: '',
      activa: true,
      fechaInstalacion: new Date().toISOString()
    };
  }

  private sensorVacio(): SensorMeteoCreate {
    return { id: '', nombre: '', tipo: '', variables: '', descripcion: '' };
  }

  nuevaEstacion(): void {
    this.estacionEnEdicion = null;
    this.formEstacion = this.estacionVacia();
    this.fechaInstalacionDate = new Date();
    this.formEstacionVisible = true;
  }

  editarEstacion(estacion: EstacionMeteo): void {
    this.estacionEnEdicion = estacion;
    this.formEstacion = {
      numero: estacion.numero,
      nombre: estacion.nombre,
      ubicacion: estacion.ubicacion,
      descripcion: estacion.descripcion ?? '',
      activa: estacion.activa,
      fechaInstalacion: estacion.fechaInstalacion
    };
    this.fechaInstalacionDate = new Date(estacion.fechaInstalacion);
    this.formEstacionVisible = true;
  }

  cancelarEstacion(): void {
    this.formEstacionVisible = false;
  }

  guardarEstacion(): void {
    const fechaInstalacion = this.fechaInstalacionDate.toISOString();

    if (this.estacionEnEdicion) {
      const payload: EstacionMeteoUpdate = {
        nombre: this.formEstacion.nombre,
        ubicacion: this.formEstacion.ubicacion,
        descripcion: this.formEstacion.descripcion,
        activa: this.formEstacion.activa,
        fechaInstalacion
      };
      this._weatherService.actualizarEstacion(this.estacionEnEdicion.id, payload).subscribe({
        next: () => {
          this._snackBar.open('Estación actualizada', 'Cerrar', { duration: 3000 });
          this.formEstacionVisible = false;
          this.cargarEstaciones();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo actualizar la estación'))
      });
    } else {
      const payload: EstacionMeteoCreate = { ...this.formEstacion, fechaInstalacion };
      this._weatherService.crearEstacion(payload).subscribe({
        next: () => {
          this._snackBar.open('Estación creada', 'Cerrar', { duration: 3000 });
          this.formEstacionVisible = false;
          this.cargarEstaciones();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo crear la estación'))
      });
    }
  }

  alternarEstacion(estacion: EstacionMeteo): void {
    const nuevoEstado = !estacion.activa;
    const accion = nuevoEstado ? 'activar' : 'desactivar';
    const motivo = prompt(`¿Por qué querés ${accion} la estación "${estacion.nombre}"? (obligatorio)`);
    if (motivo === null) return;
    if (!motivo.trim()) {
      this.mostrarError('El motivo es obligatorio');
      return;
    }

    this._weatherService.cambiarEstadoEstacion(estacion.id, { activo: nuevoEstado, motivo: motivo.trim() }).subscribe({
      next: () => {
        estacion.activa = nuevoEstado;
        this._snackBar.open(nuevoEstado ? 'Estación activada' : 'Estación desactivada', 'Cerrar', { duration: 3000 });
        this.cargarAuditoria();
      },
      error: err => this.mostrarError(this.extraerError(err, 'No se pudo cambiar el estado de la estación'))
    });
  }

  eliminarEstacion(estacion: EstacionMeteo): void {
    if (!confirm(`¿Eliminar la estación "${estacion.nombre}"?`)) return;

    this._weatherService.eliminarEstacion(estacion.id).subscribe({
      next: () => {
        this._snackBar.open('Estación eliminada', 'Cerrar', { duration: 3000 });
        this.cargarEstaciones();
      },
      error: err => this.mostrarError(this.extraerError(err, 'No se pudo eliminar la estación'))
    });
  }

  nuevoSensor(): void {
    this.sensorEnEdicion = null;
    this.formSensor = this.sensorVacio();
    this.formSensorVisible = true;
  }

  editarSensor(sensor: SensorMeteo): void {
    this.sensorEnEdicion = sensor;
    this.formSensor = {
      id: sensor.id,
      nombre: sensor.nombre,
      tipo: sensor.tipo,
      variables: sensor.variables,
      descripcion: sensor.descripcion ?? ''
    };
    this.formSensorVisible = true;
  }

  cancelarSensor(): void {
    this.formSensorVisible = false;
  }

  guardarSensor(): void {
    if (this.sensorEnEdicion) {
      const payload: SensorMeteoUpdate = {
        nombre: this.formSensor.nombre,
        tipo: this.formSensor.tipo,
        variables: this.formSensor.variables,
        descripcion: this.formSensor.descripcion
      };
      this._weatherService.actualizarSensor(this.sensorEnEdicion.id, payload).subscribe({
        next: () => {
          this._snackBar.open('Sensor actualizado', 'Cerrar', { duration: 3000 });
          this.formSensorVisible = false;
          this.cargarSensores();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo actualizar el sensor'))
      });
    } else {
      this._weatherService.crearSensor(this.formSensor).subscribe({
        next: () => {
          this._snackBar.open('Sensor creado', 'Cerrar', { duration: 3000 });
          this.formSensorVisible = false;
          this.cargarSensores();
        },
        error: err => this.mostrarError(this.extraerError(err, 'No se pudo crear el sensor'))
      });
    }
  }

  alternarSensor(sensor: SensorMeteo): void {
    const nuevoEstado = !sensor.activo;
    const accion = nuevoEstado ? 'activar' : 'desactivar';
    const motivo = prompt(`¿Por qué querés ${accion} el sensor "${sensor.nombre}"? (obligatorio)`);
    if (motivo === null) return;
    if (!motivo.trim()) {
      this.mostrarError('El motivo es obligatorio');
      return;
    }

    this._weatherService.cambiarEstadoSensor(sensor.id, { activo: nuevoEstado, motivo: motivo.trim() }).subscribe({
      next: () => {
        sensor.activo = nuevoEstado;
        this._snackBar.open(nuevoEstado ? 'Sensor activado' : 'Sensor desactivado', 'Cerrar', { duration: 3000 });
        this.cargarAuditoria();
      },
      error: err => this.mostrarError(this.extraerError(err, 'No se pudo cambiar el estado del sensor'))
    });
  }

  eliminarSensor(sensor: SensorMeteo): void {
    if (!confirm(`¿Eliminar el sensor "${sensor.nombre}"?`)) return;

    this._weatherService.eliminarSensor(sensor.id).subscribe({
      next: () => {
        this._snackBar.open('Sensor eliminado', 'Cerrar', { duration: 3000 });
        this.cargarSensores();
      },
      error: err => this.mostrarError(this.extraerError(err, 'No se pudo eliminar el sensor'))
    });
  }

  private extraerError(err: any, mensajePorDefecto: string): string {
    return err?.error?.errorMessage || mensajePorDefecto;
  }

  private mostrarError(mensaje: string): void {
    this._snackBar.open(mensaje, 'Cerrar', { duration: 4000 });
  }
}
