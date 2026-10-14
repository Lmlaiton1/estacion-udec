import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { Observable, Subscription } from 'rxjs';
import { MATERIAL_MODULES } from '../../shared/material';
import { CultivoService } from '../../service/cultivo/cultivo-service';
import { AlarmaAviso } from '../../service/cultivo/alarma-aviso';
import { Authservice } from '../../service/auth/authservice';
import { Rolservice } from '../../service/rol/rolservice';
import { WeatherService } from '../../service/weather/weather-service';
import { EstacionMeteo } from '../../models/EstacionMeteo';
import { Responses } from '../../models/Response';
import {
  ActuadorCultivo, ActuadorCultivoCreate, AlarmaCultivo, AuditoriaCultivo, BitacoraAlarmaCultivo, Cultivo, CultivoCreate,
  EtapaCultivo, EtapaCultivoCreate, EventoDispositivoCultivo, SensorCultivo, SensorCultivoCreate, UmbralCultivo, UmbralCultivoCreate
} from '../../models/Cultivo';

// Ventana para pedir el "por qué" / "cómo" antes de ejecutar una acción auditada
interface AccionPendiente {
  titulo: string;
  descripcion: string;
  etiqueta: string;
  texto: string;
  ejecutar: (texto: string) => Observable<Responses>;
  exito: string;
  peligro?: boolean;
  // Solo al accionar un actuador: alarma que se está atendiendo
  alarmasCultivo?: AlarmaCultivo[];
  alarmaId?: string | null;
  // Solo al modificar una alarma
  editarAlarma?: boolean;
  gravedad?: string;
  solucion?: string | null;
  solucionEditable?: boolean;
}

@Component({
  selector: 'app-estacion-cultivo',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, MatTabsModule],
  templateUrl: './estacion-cultivo.html',
  styleUrls: ['./estacion-cultivo.scss']
})
export class EstacionCultivo implements OnInit, OnDestroy {
  readonly niveles = ['Baja', 'Media', 'Alta'];
  readonly tiposActuador = ['Ventilador', 'Riego', 'Nebulizador', 'Calefactor', 'Extractor', 'Iluminación', 'Otro'];
  readonly tiposEntidad = ['Cultivo', 'Etapa', 'Sensor', 'Actuador', 'Umbral', 'Alarma'];
  readonly nombresAccion: Partial<Record<string, string>> = {
    Disparada: 'Alarma disparada',
    Vista: 'Vio la alarma',
    TomoControl: 'Tomó el control',
    Comentario: 'Comentario',
    Resuelta: 'Resolvió la alarma',
    AccionManual: 'Acción manual',
    AccionAutomatica: 'Acción automática',
    SmsEnviado: 'SMS enviado',
    SmsFallido: 'SMS no enviado',
    Modificada: 'Modificó la alarma',
    Anulada: 'Anuló la alarma',
    Reactivada: 'Reactivó la alarma',
    Eliminada: 'Eliminó la alarma'
  };
  readonly nombresAuditoria: Partial<Record<string, string>> = {
    Creado: 'Creó',
    Modificado: 'Modificó',
    Eliminado: 'Eliminó',
    Activado: 'Activó',
    Desactivado: 'Desactivó',
    EtapaActual: 'Cambió etapa actual',
    QuitarEtapaActual: 'Quitó etapa actual',
    Anulada: 'Anuló',
    Reactivada: 'Reactivó'
  };

  rolUsuario: string | null = null;
  tabIndex = 0;

  cultivos: Cultivo[] = [];
  cultivoFiltro: string | null = null;
  // Estaciones del catálogo que se pueden asociar a un cultivo
  estaciones: EstacionMeteo[] = [];

  sensores: SensorCultivo[] = [];
  actuadores: ActuadorCultivo[] = [];
  umbrales: UmbralCultivo[] = [];
  alarmas: AlarmaCultivo[] = [];
  soloAbiertas = true;
  eventos: EventoDispositivoCultivo[] = [];
  bitacora: BitacoraAlarmaCultivo[] = [];
  auditoria: AuditoriaCultivo[] = [];
  filtroTipoDispositivo: string | null = null;
  filtroTipoEntidad: string | null = null;
  filtroDesde: string | null = null;

  columnasSensores = ['cultivo', 'nombre', 'tipo', 'variable', 'clave', 'rango', 'ultimo', 'estado', 'acciones'];
  columnasUmbrales = ['cultivo', 'sensor', 'etapa', 'rango', 'nivel', 'accion', 'sms', 'estado', 'acciones'];
  columnasAuditoria = ['fecha', 'entidad', 'accion', 'detalle', 'motivo', 'usuario'];
  columnasEventos = ['fecha', 'dispositivo', 'accion', 'origen', 'usuario', 'motivo'];
  columnasBitacora = ['fecha', 'accion', 'usuario', 'comentario'];

  // Formularios (null = cerrado; id = edición)
  formCultivo: CultivoCreate | null = null;
  cultivoEnEdicion: string | null = null;
  formEtapa: EtapaCultivoCreate | null = null;
  etapaEnEdicion: string | null = null;
  formSensor: SensorCultivoCreate | null = null;
  sensorEnEdicion: string | null = null;
  formActuador: ActuadorCultivoCreate | null = null;
  actuadorEnEdicion: string | null = null;
  formUmbral: UmbralCultivoCreate | null = null;
  umbralEnEdicion: string | null = null;

  accion: AccionPendiente | null = null;
  enviando = false;

  private subs: Subscription[] = [];
  private alTerminarAccion: () => void = () => { };

  constructor(
    private _cultivoService: CultivoService,
    public aviso: AlarmaAviso,
    private _authService: Authservice,
    private _rolService: Rolservice,
    private _weatherService: WeatherService,
    private _snackBar: MatSnackBar
  ) {
    const user = this._authService.getTokenUserInfo();
    if (user?.rolId) {
      this._rolService.getRol(user.rolId).subscribe(data => this.rolUsuario = data.name);
    }
  }

  ngOnInit(): void {
    this.cargarCultivos();
    this.cargarEstaciones();
    this.cargarTodo();

    // Tiempo real: refresca alarmas y actuadores cuando cambian en otra pantalla o por el sistema
    this._cultivoService.iniciarConexion();
    this.subs.push(
      this._cultivoService.alarma$.subscribe(() => this.cargarAlarmas()),
      this._cultivoService.actuador$.subscribe(a => {
        const actuador = this.actuadores.find(x => x.id === a.actuadorId);
        if (actuador) {
          actuador.encendido = a.encendido;
          actuador.modo = a.modo as any;
          actuador.activo = a.activo;
        }
        if (this.tabIndex === 5) this.cargarAuditoria();
      }),
      this._cultivoService.lectura$.subscribe(() => this.cargarSensores())
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  get esAdmin(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  get cultivosVisibles(): Cultivo[] {
    return this.cultivoFiltro ? this.cultivos.filter(c => c.id === this.cultivoFiltro) : this.cultivos;
  }

  get alarmasAbiertas(): number {
    return this.alarmas.filter(a => a.estado !== 'Resuelta' && a.estado !== 'Anulada').length;
  }

  // ---------- Carga ----------
  cambiarFiltroCultivo(): void {
    this.cargarTodo();
  }

  cambiarTab(index: number): void {
    this.tabIndex = index;
    if (index === 5) this.cargarAuditoria();
  }

  private cargarTodo(): void {
    this.cargarSensores();
    this.cargarActuadores();
    this.cargarUmbrales();
    this.cargarAlarmas();
    if (this.tabIndex === 5) this.cargarAuditoria();
  }

  private cargarCultivos(): void {
    this._cultivoService.getCultivos().subscribe({
      next: data => this.cultivos = data,
      error: () => this.mostrarError('No se pudieron cargar los cultivos')
    });
  }

  private cargarEstaciones(): void {
    this._weatherService.getEstaciones().subscribe({
      next: data => this.estaciones = data.sort((a, b) => a.numero - b.numero),
      error: () => this.mostrarError('No se pudieron cargar las estaciones')
    });
  }

  private cargarSensores(): void {
    this._cultivoService.getSensores(this.cultivoFiltro).subscribe({
      next: data => this.sensores = data,
      error: () => this.mostrarError('No se pudieron cargar los sensores')
    });
  }

  private cargarActuadores(): void {
    this._cultivoService.getActuadores(this.cultivoFiltro).subscribe({
      next: data => this.actuadores = data,
      error: () => this.mostrarError('No se pudieron cargar los actuadores')
    });
  }

  private cargarUmbrales(): void {
    this._cultivoService.getUmbrales(this.cultivoFiltro).subscribe({
      next: data => this.umbrales = data,
      error: () => this.mostrarError('No se pudieron cargar los umbrales')
    });
  }

  cargarAlarmas(): void {
    this._cultivoService.getAlarmas(this.cultivoFiltro, this.soloAbiertas).subscribe({
      next: data => this.alarmas = data,
      error: () => this.mostrarError('No se pudieron cargar las alarmas')
    });
  }

  cargarAuditoria(): void {
    const desde = this.filtroDesde || null;
    this._cultivoService.getAuditoriaConfiguracion(this.cultivoFiltro, this.filtroTipoEntidad, desde).subscribe({
      next: data => this.auditoria = data,
      error: () => this.mostrarError('No se pudo cargar la auditoría de configuración')
    });
    this._cultivoService.getEventos(this.cultivoFiltro, this.filtroTipoDispositivo, desde).subscribe({
      next: data => this.eventos = data,
      error: () => this.mostrarError('No se pudo cargar la auditoría de dispositivos')
    });
    this._cultivoService.getBitacoraAlarmas(this.cultivoFiltro, desde).subscribe({
      next: data => this.bitacora = data,
      error: () => this.mostrarError('No se pudo cargar la bitácora de alarmas')
    });
  }

  // ---------- Cultivos ----------
  nuevoCultivo(): void {
    const siguiente = this.cultivos.length ? Math.max(...this.cultivos.map(c => c.numero)) + 1 : 1;
    this.cultivoEnEdicion = null;
    this.formCultivo = {
      numero: siguiente, nombre: '', especie: '', variedad: null, ubicacion: null, areaM2: null,
      fechaSiembra: null, estaciones: [], descripcion: null, motivo: null
    };
  }

  editarCultivo(c: Cultivo): void {
    this.cultivoEnEdicion = c.id;
    this.formCultivo = {
      numero: c.numero, nombre: c.nombre, especie: c.especie, variedad: c.variedad, ubicacion: c.ubicacion,
      areaM2: c.areaM2, fechaSiembra: c.fechaSiembra?.substring(0, 10) ?? null, estaciones: [...c.estaciones],
      descripcion: c.descripcion, motivo: null
    };
  }

  // ---------- Estaciones del cultivo ----------
  get todasLasEstaciones(): boolean {
    return !!this.formCultivo && this.estaciones.length > 0 &&
      this.estaciones.every(e => this.formCultivo!.estaciones.includes(e.numero));
  }

  alternarTodasLasEstaciones(marcar: boolean): void {
    if (!this.formCultivo) return;
    this.formCultivo.estaciones = marcar ? this.estaciones.map(e => e.numero) : [];
  }

  textoEstaciones(numeros: number[]): string {
    if (!numeros.length) return 'Sin estaciones';
    const todas = this.estaciones.length > 0 && this.estaciones.every(e => numeros.includes(e.numero));
    const lista = [...numeros].sort((a, b) => a - b).join(', ');
    return todas ? `Todas las estaciones (${lista})` : (numeros.length === 1 ? `Estación ${lista}` : `Estaciones ${lista}`);
  }

  guardarCultivo(): void {
    if (!this.formCultivo) return;
    const peticion = this.cultivoEnEdicion
      ? this._cultivoService.actualizarCultivo(this.cultivoEnEdicion, this.formCultivo)
      : this._cultivoService.crearCultivo(this.formCultivo);

    this.ejecutar(peticion, this.cultivoEnEdicion ? 'Cultivo actualizado' : 'Cultivo creado', () => {
      this.formCultivo = null;
      this.cargarCultivos();
    });
  }

  cambiarEstadoCultivo(c: Cultivo): void {
    const activar = !c.activo;
    this.pedirTexto({
      titulo: `${activar ? 'Reactivar' : 'Desactivar'} el cultivo ${c.nombre}`,
      descripcion: activar ? 'El cultivo vuelve a registrar lecturas y evaluar umbrales.'
        : 'Mientras esté desactivado no se guardan lecturas ni se disparan alarmas de este cultivo.',
      etiqueta: '¿Por qué? (queda en la auditoría)',
      exito: activar ? 'Cultivo reactivado' : 'Cultivo desactivado',
      peligro: !activar,
      ejecutar: motivo => this._cultivoService.cambiarEstadoCultivo(c.id, { activo: activar, motivo })
    }, () => this.cargarCultivos());
  }

  eliminarCultivo(c: Cultivo): void {
    this.pedirTexto({
      titulo: `Eliminar el cultivo ${c.nombre}`,
      descripcion: 'También se eliminan sus etapas, sensores, actuadores y umbrales; las alarmas abiertas se anulan ' +
        'y los actuadores encendidos se apagan. Las lecturas y la auditoría se conservan.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Cultivo eliminado',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarCultivo(c.id, motivo)
    }, () => {
      if (this.cultivoFiltro === c.id) this.cultivoFiltro = null;
      this.cargarCultivos();
      this.cargarTodo();
    });
  }

  // ---------- Etapas ----------
  nuevaEtapa(c: Cultivo): void {
    this.etapaEnEdicion = null;
    this.formEtapa = {
      cultivoId: c.id, nombre: '', orden: (c.etapas.length ? Math.max(...c.etapas.map(e => e.orden)) : 0) + 1,
      duracionDias: null, fechaInicio: null, descripcion: null, motivo: null
    };
  }

  editarEtapa(e: EtapaCultivo): void {
    this.etapaEnEdicion = e.id;
    this.formEtapa = {
      cultivoId: e.cultivoId, nombre: e.nombre, orden: e.orden, duracionDias: e.duracionDias,
      fechaInicio: e.fechaInicio?.substring(0, 10) ?? null, descripcion: e.descripcion, motivo: null
    };
  }

  guardarEtapa(): void {
    if (!this.formEtapa) return;
    const peticion = this.etapaEnEdicion
      ? this._cultivoService.actualizarEtapa(this.etapaEnEdicion, this.formEtapa)
      : this._cultivoService.crearEtapa(this.formEtapa);

    this.ejecutar(peticion, 'Etapa guardada', () => {
      this.formEtapa = null;
      this.cargarCultivos();
    });
  }

  activarEtapa(e: EtapaCultivo): void {
    this.pedirTexto({
      titulo: `Pasar a la etapa "${e.nombre}"`,
      descripcion: 'Desde ahora se evaluarán los umbrales de esta etapa (y los de "todas las etapas").',
      etiqueta: '¿Por qué cambia de etapa? (queda en la auditoría)',
      exito: `Etapa "${e.nombre}" activa`,
      ejecutar: motivo => this._cultivoService.activarEtapa(e.id, motivo)
    }, () => this.cargarCultivos());
  }

  quitarEtapaActual(e: EtapaCultivo): void {
    this.pedirTexto({
      titulo: `Desactivar la etapa "${e.nombre}"`,
      descripcion: 'El cultivo queda sin etapa actual: solo se evaluarán los umbrales de "todas las etapas".',
      etiqueta: '¿Por qué? (queda en la auditoría)',
      exito: 'Etapa desactivada',
      peligro: true,
      ejecutar: motivo => this._cultivoService.quitarEtapaActual(e.id, motivo)
    }, () => this.cargarCultivos());
  }

  eliminarEtapa(e: EtapaCultivo): void {
    this.pedirTexto({
      titulo: `Eliminar la etapa "${e.nombre}"`,
      descripcion: 'Si la etapa tiene umbrales, primero elimínelos o cámbielos de etapa.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Etapa eliminada',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarEtapa(e.id, motivo)
    }, () => this.cargarCultivos());
  }

  etapasDe(cultivoId: string | null | undefined): EtapaCultivo[] {
    return this.cultivos.find(c => c.id === cultivoId)?.etapas ?? [];
  }

  // ---------- Sensores ----------
  nuevoSensor(): void {
    this.sensorEnEdicion = null;
    this.formSensor = {
      cultivoId: this.cultivoFiltro ?? this.cultivos[0]?.id ?? '', nombre: '', clave: '', tipo: 'DHT22',
      variable: 'temperatura', unidad: '°C', rangoMin: -40, rangoMax: 80, precision: 0.5,
      marca: null, especificaciones: null, fechaInstalacion: null, motivo: null
    };
  }

  // Plantilla rápida: el DHT22 mide dos variables, se registran como dos sensores
  plantillaDht22(variable: 'temperatura' | 'humedad'): void {
    if (!this.formSensor) return;
    const esTemp = variable === 'temperatura';
    Object.assign(this.formSensor, {
      nombre: esTemp ? 'DHT22 temperatura' : 'DHT22 humedad',
      clave: variable,
      tipo: 'DHT22',
      variable,
      unidad: esTemp ? '°C' : '%',
      rangoMin: esTemp ? -40 : 0,
      rangoMax: esTemp ? 80 : 100,
      precision: esTemp ? 0.5 : 2,
      especificaciones: esTemp ? 'Resolución 0.1 °C, alimentación 3.3–5 V' : 'Resolución 0.1 %HR, alimentación 3.3–5 V'
    });
  }

  editarSensor(s: SensorCultivo): void {
    this.sensorEnEdicion = s.id;
    this.formSensor = {
      cultivoId: s.cultivoId, nombre: s.nombre, clave: s.clave, tipo: s.tipo, variable: s.variable, unidad: s.unidad,
      rangoMin: s.rangoMin, rangoMax: s.rangoMax, precision: s.precision, marca: s.marca,
      especificaciones: s.especificaciones, fechaInstalacion: s.fechaInstalacion?.substring(0, 10) ?? null, motivo: null
    };
  }

  guardarSensor(): void {
    if (!this.formSensor) return;
    const peticion = this.sensorEnEdicion
      ? this._cultivoService.actualizarSensor(this.sensorEnEdicion, this.formSensor)
      : this._cultivoService.crearSensor(this.formSensor);

    this.ejecutar(peticion, 'Sensor guardado', () => {
      this.formSensor = null;
      this.cargarSensores();
    });
  }

  cambiarEstadoSensor(s: SensorCultivo): void {
    const activar = !s.activo;
    this.pedirTexto({
      titulo: `${activar ? 'Reactivar' : 'Desactivar'} ${s.nombre}`,
      descripcion: activar ? 'El sensor volverá a registrar lecturas y a evaluar umbrales.'
        : 'Mientras esté desactivado no se guardan sus lecturas ni se evalúan sus umbrales.',
      etiqueta: '¿Por qué? (queda en la auditoría)',
      exito: activar ? 'Sensor reactivado' : 'Sensor desactivado',
      peligro: !activar,
      ejecutar: motivo => this._cultivoService.cambiarEstadoSensor(s.id, { activo: activar, motivo })
    }, () => this.cargarSensores());
  }

  eliminarSensor(s: SensorCultivo): void {
    this.pedirTexto({
      titulo: `Eliminar ${s.nombre}`,
      descripcion: 'Las lecturas históricas se conservan. Si el sensor tiene umbrales, primero elimínelos.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Sensor eliminado',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarSensor(s.id, motivo)
    }, () => this.cargarSensores());
  }

  sensoresDe(cultivoId: string | null | undefined): SensorCultivo[] {
    return this.sensores.filter(s => s.cultivoId === cultivoId);
  }

  // ---------- Actuadores ----------
  nuevoActuador(): void {
    this.actuadorEnEdicion = null;
    this.formActuador = {
      cultivoId: this.cultivoFiltro ?? this.cultivos[0]?.id ?? '', nombre: '', clave: '', tipo: 'Ventilador',
      marca: null, potencia: null, especificaciones: null, motivo: null
    };
  }

  editarActuador(a: ActuadorCultivo): void {
    this.actuadorEnEdicion = a.id;
    this.formActuador = {
      cultivoId: a.cultivoId, nombre: a.nombre, clave: a.clave, tipo: a.tipo, marca: a.marca,
      potencia: a.potencia, especificaciones: a.especificaciones, motivo: null
    };
  }

  guardarActuador(): void {
    if (!this.formActuador) return;
    const peticion = this.actuadorEnEdicion
      ? this._cultivoService.actualizarActuador(this.actuadorEnEdicion, this.formActuador)
      : this._cultivoService.crearActuador(this.formActuador);

    this.ejecutar(peticion, 'Actuador guardado', () => {
      this.formActuador = null;
      this.cargarActuadores();
    });
  }

  // Encendido/apagado manual: pide el "para qué" y permite asociarlo a una alarma abierta
  accionarActuador(a: ActuadorCultivo, alarma?: AlarmaCultivo): void {
    const encender = !a.encendido;
    const abiertas = this.alarmas.filter(x => x.cultivoId === a.cultivoId && x.estado !== 'Resuelta' && x.estado !== 'Anulada');

    this.pedirTexto({
      titulo: `${encender ? 'Encender' : 'Apagar'} ${a.nombre}`,
      descripcion: a.modo === 'Automatico'
        ? 'El actuador pasará a modo MANUAL para que el sistema no revierta tu acción. Devuélvelo a automático cuando termines.'
        : 'Acción manual sobre el actuador.',
      etiqueta: `¿Para qué lo ${encender ? 'enciendes' : 'apagas'}? (queda en la auditoría)`,
      exito: `${a.nombre} ${encender ? 'encendido' : 'apagado'}`,
      alarmasCultivo: abiertas,
      alarmaId: alarma?.id ?? null,
      ejecutar: motivo => this._cultivoService.accionarActuador(a.id, {
        encender, motivo, alarmaCultivoId: this.accion?.alarmaId ?? null
      })
    }, () => {
      this.cargarActuadores();
      this.cargarAlarmas();
    });
  }

  cambiarModo(a: ActuadorCultivo): void {
    const modo = a.modo === 'Automatico' ? 'Manual' : 'Automatico';
    this.pedirTexto({
      titulo: `Pasar ${a.nombre} a modo ${modo === 'Automatico' ? 'automático' : 'manual'}`,
      descripcion: modo === 'Automatico'
        ? 'Los umbrales volverán a encender y apagar este actuador.'
        : 'Los umbrales dejarán de controlar este actuador hasta que vuelva a automático.',
      etiqueta: 'Motivo (queda en la auditoría)',
      exito: `Modo ${modo === 'Automatico' ? 'automático' : 'manual'}`,
      ejecutar: motivo => this._cultivoService.cambiarModoActuador(a.id, modo, motivo)
    }, () => this.cargarActuadores());
  }

  cambiarEstadoActuador(a: ActuadorCultivo): void {
    const activar = !a.activo;
    this.pedirTexto({
      titulo: `${activar ? 'Reactivar' : 'Desactivar'} ${a.nombre}`,
      descripcion: activar ? 'El actuador vuelve a estar disponible.' : 'Si está encendido se enviará la orden de apagado.',
      etiqueta: '¿Por qué? (queda en la auditoría)',
      exito: activar ? 'Actuador reactivado' : 'Actuador desactivado',
      peligro: !activar,
      ejecutar: motivo => this._cultivoService.cambiarEstadoActuador(a.id, { activo: activar, motivo })
    }, () => this.cargarActuadores());
  }

  eliminarActuador(a: ActuadorCultivo): void {
    this.pedirTexto({
      titulo: `Eliminar ${a.nombre}`,
      descripcion: 'Si está encendido se manda a apagar. Si está asociado a umbrales, primero quítelo de ellos.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Actuador eliminado',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarActuador(a.id, motivo)
    }, () => this.cargarActuadores());
  }

  actuadoresDe(cultivoId: string | null | undefined): ActuadorCultivo[] {
    return this.actuadores.filter(a => a.cultivoId === cultivoId);
  }

  // ---------- Umbrales ----------
  nuevoUmbral(): void {
    const cultivoId = this.cultivoFiltro ?? this.cultivos[0]?.id ?? '';
    this.umbralEnEdicion = null;
    this.formUmbral = {
      cultivoId, sensorCultivoId: this.sensoresDe(cultivoId)[0]?.id ?? '', etapaCultivoId: null,
      valorMin: null, valorMax: null, nivelGravedad: 'Media', actuadorCultivoId: null, encenderActuador: true,
      revertirAlNormalizar: true, minutosEscalamientoSms: null, descripcion: null, motivo: null
    };
  }

  editarUmbral(u: UmbralCultivo): void {
    this.umbralEnEdicion = u.id;
    this.formUmbral = {
      cultivoId: u.cultivoId, sensorCultivoId: u.sensorCultivoId, etapaCultivoId: u.etapaCultivoId,
      valorMin: u.valorMin, valorMax: u.valorMax, nivelGravedad: u.nivelGravedad, actuadorCultivoId: u.actuadorCultivoId,
      encenderActuador: u.encenderActuador ?? true, revertirAlNormalizar: u.revertirAlNormalizar,
      minutosEscalamientoSms: u.minutosEscalamientoSms, descripcion: u.descripcion, motivo: null
    };
  }

  cambioCultivoUmbral(): void {
    if (!this.formUmbral) return;
    this.formUmbral.sensorCultivoId = this.sensoresDe(this.formUmbral.cultivoId)[0]?.id ?? '';
    this.formUmbral.etapaCultivoId = null;
    this.formUmbral.actuadorCultivoId = null;
  }

  guardarUmbral(): void {
    if (!this.formUmbral) return;
    const payload = { ...this.formUmbral };
    if (!payload.actuadorCultivoId) payload.encenderActuador = null;

    const peticion = this.umbralEnEdicion
      ? this._cultivoService.actualizarUmbral(this.umbralEnEdicion, payload)
      : this._cultivoService.crearUmbral(payload);

    this.ejecutar(peticion, 'Umbral guardado', () => {
      this.formUmbral = null;
      this.cargarUmbrales();
    });
  }

  cambiarEstadoUmbral(u: UmbralCultivo): void {
    const activar = !u.activo;
    this.pedirTexto({
      titulo: `${activar ? 'Reactivar' : 'Desactivar'} el umbral de ${u.sensorNombre}`,
      descripcion: activar ? 'El umbral vuelve a evaluarse con cada lectura.'
        : 'Mientras esté desactivado no dispara alarmas ni acciona actuadores.',
      etiqueta: '¿Por qué? (queda en la auditoría)',
      exito: activar ? 'Umbral reactivado' : 'Umbral desactivado',
      peligro: !activar,
      ejecutar: motivo => this._cultivoService.cambiarEstadoUmbral(u.id, { activo: activar, motivo })
    }, () => this.cargarUmbrales());
  }

  eliminarUmbral(u: UmbralCultivo): void {
    this.pedirTexto({
      titulo: `Eliminar el umbral de ${u.sensorNombre}`,
      descripcion: 'Las alarmas que ya generó se conservan.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Umbral eliminado',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarUmbral(u.id, motivo)
    }, () => this.cargarUmbrales());
  }

  rango(min: number | null, max: number | null, unidad = ''): string {
    if (min !== null && max !== null) return `${min} a ${max} ${unidad}`;
    if (min !== null) return `≥ ${min} ${unidad}`;
    if (max !== null) return `≤ ${max} ${unidad}`;
    return '—';
  }

  // ---------- Alarmas: atención ----------
  toggleSoloAbiertas(): void {
    this.soloAbiertas = !this.soloAbiertas;
    this.cargarAlarmas();
  }

  marcarVista(a: AlarmaCultivo): void {
    this.ejecutar(this._cultivoService.marcarVista(a.id), 'Alarma marcada como vista', () => this.cargarAlarmas());
  }

  tomarControl(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Tomar el control de la alarma',
      descripcion: a.mensaje,
      etiqueta: '¿Cómo vas a atenderla? (ej.: voy al invernadero, enciendo ventilador manual)',
      exito: 'Tienes el control de la alarma',
      ejecutar: texto => this._cultivoService.tomarControl(a.id, texto)
    }, () => this.cargarAlarmas());
  }

  comentarAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Agregar comentario',
      descripcion: a.mensaje,
      etiqueta: 'Comentario',
      exito: 'Comentario agregado',
      ejecutar: texto => this._cultivoService.comentar(a.id, texto)
    }, () => this.cargarAlarmas());
  }

  resolverAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Resolver alarma',
      descripcion: a.mensaje,
      etiqueta: '¿Cómo se solucionó?',
      exito: 'Alarma resuelta',
      ejecutar: texto => this._cultivoService.resolver(a.id, texto)
    }, () => this.cargarAlarmas());
  }

  // ---------- Alarmas: administración (solo Administrador) ----------
  modificarAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Modificar alarma',
      descripcion: a.mensaje,
      etiqueta: '¿Por qué se modifica? (queda en la auditoría)',
      exito: 'Alarma modificada',
      editarAlarma: true,
      gravedad: a.nivelGravedad,
      solucion: a.solucion,
      solucionEditable: a.estado === 'Resuelta',
      ejecutar: motivo => this._cultivoService.modificarAlarma(a.id, this.accion?.gravedad ?? a.nivelGravedad,
        this.accion?.solucionEditable ? (this.accion?.solucion ?? null) : null, motivo)
    }, () => this.cargarAlarmas());
  }

  anularAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Desactivar (anular) alarma',
      descripcion: 'Úselo para falsas alarmas (ej. sensor desconectado). Si el valor sigue fuera de rango, se disparará una alarma nueva.',
      etiqueta: '¿Por qué se anula? (queda en la auditoría)',
      exito: 'Alarma anulada',
      peligro: true,
      ejecutar: motivo => this._cultivoService.anularAlarma(a.id, motivo)
    }, () => this.cargarAlarmas());
  }

  reactivarAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Reactivar alarma',
      descripcion: a.estado === 'Resuelta'
        ? 'La alarma vuelve a quedar abierta y se borra la resolución actual (queda registrada en su historial).'
        : 'La alarma vuelve a quedar abierta para atenderla.',
      etiqueta: '¿Por qué se reactiva? (queda en la auditoría)',
      exito: 'Alarma reactivada',
      ejecutar: motivo => this._cultivoService.reactivarAlarma(a.id, motivo)
    }, () => this.cargarAlarmas());
  }

  eliminarAlarma(a: AlarmaCultivo): void {
    this.pedirTexto({
      titulo: 'Eliminar alarma',
      descripcion: 'Deja de verse en la lista. Su historial queda en la auditoría.',
      etiqueta: '¿Por qué se elimina? (queda en la auditoría)',
      exito: 'Alarma eliminada',
      peligro: true,
      ejecutar: motivo => this._cultivoService.eliminarAlarma(a.id, motivo)
    }, () => this.cargarAlarmas());
  }

  nombreEstado(estado: string): string {
    return estado === 'EnAtencion' ? 'En atención' : estado;
  }

  esAbierta(a: AlarmaCultivo): boolean {
    return a.estado !== 'Resuelta' && a.estado !== 'Anulada';
  }

  async activarNotificaciones(): Promise<void> {
    const permiso = await this.aviso.solicitarPermiso();
    if (permiso === 'granted') this._snackBar.open('Notificaciones de escritorio activadas', 'Cerrar', { duration: 3000 });
    else if (permiso === 'denied') this.mostrarError('El navegador bloqueó las notificaciones. Habilítalas en el candado de la barra de direcciones');
  }

  // ---------- Utilidades ----------
  private pedirTexto(accion: Omit<AccionPendiente, 'texto'>, alTerminar: () => void): void {
    this.accion = { ...accion, texto: '' };
    this.alTerminarAccion = alTerminar;
  }

  confirmarAccion(): void {
    if (!this.accion || !this.accion.texto.trim()) return;
    const accion = this.accion;
    this.ejecutar(accion.ejecutar(accion.texto.trim()), accion.exito, () => {
      this.accion = null;
      this.alTerminarAccion();
    });
  }

  cancelarAccion(): void {
    this.accion = null;
  }

  private ejecutar(peticion: Observable<Responses>, exito: string, alTerminar: () => void): void {
    this.enviando = true;
    peticion.subscribe({
      next: () => {
        this.enviando = false;
        this._snackBar.open(exito, 'Cerrar', { duration: 3000 });
        alTerminar();
      },
      error: err => {
        this.enviando = false;
        this.mostrarError(err?.error?.errorMessage || 'No se pudo completar la acción');
      }
    });
  }

  private mostrarError(mensaje: string): void {
    this._snackBar.open(mensaje, 'Cerrar', { duration: 5000 });
  }
}
