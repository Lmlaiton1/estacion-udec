// Modelos del control de umbrales y alarmas de cultivo (espejo de los DTO del backend)

export interface EtapaCultivo {
  id: string;
  cultivoId: string;
  nombre: string;
  orden: number;
  duracionDias: number | null;
  fechaInicio: string | null;
  activa: boolean;
  descripcion: string | null;
}

export interface EtapaCultivoCreate {
  cultivoId: string;
  nombre: string;
  orden: number;
  duracionDias: number | null;
  fechaInicio: string | null;
  descripcion: string | null;
  motivo?: string | null;
}

export interface Cultivo {
  id: string;
  numero: number;
  nombre: string;
  especie: string;
  variedad: string | null;
  ubicacion: string | null;
  areaM2: number | null;
  fechaSiembra: string | null;
  estaciones: number[];
  descripcion: string | null;
  activo: boolean;
  fechaCreacion: string;
  etapaActual: string | null;
  etapas: EtapaCultivo[];
}

export interface CultivoCreate {
  numero: number;
  nombre: string;
  especie: string;
  variedad: string | null;
  ubicacion: string | null;
  areaM2: number | null;
  fechaSiembra: string | null;
  estaciones: number[];
  descripcion: string | null;
  motivo?: string | null;
}

export interface SensorCultivo {
  id: string;
  cultivoId: string;
  cultivoNombre: string;
  nombre: string;
  clave: string;
  tipo: string;
  variable: string;
  unidad: string;
  rangoMin: number | null;
  rangoMax: number | null;
  precision: number | null;
  marca: string | null;
  especificaciones: string | null;
  activo: boolean;
  fechaInstalacion: string;
  ultimoValor: number | null;
  fechaUltimoValor: string | null;
  // Sensor de Nueva Naturaleza (acuaponía) vinculado; null = ESP por MQTT
  deviceId: string | null;
}

export interface SensorCultivoCreate {
  cultivoId: string;
  nombre: string;
  clave: string;
  tipo: string;
  variable: string;
  unidad: string;
  rangoMin: number | null;
  rangoMax: number | null;
  precision: number | null;
  marca: string | null;
  especificaciones: string | null;
  fechaInstalacion: string | null;
  deviceId: string | null;
  motivo?: string | null;
}

export interface ActuadorCultivo {
  id: string;
  cultivoId: string;
  cultivoNombre: string;
  nombre: string;
  clave: string;
  tipo: string;
  marca: string | null;
  potencia: string | null;
  especificaciones: string | null;
  modo: 'Automatico' | 'Manual';
  encendido: boolean;
  activo: boolean;
  fechaUltimoCambio: string | null;
  // Actuador de Nueva Naturaleza (acuaponía) vinculado; null = ESP por MQTT
  deviceId: string | null;
}

export interface ActuadorCultivoCreate {
  cultivoId: string;
  nombre: string;
  clave: string;
  tipo: string;
  marca: string | null;
  potencia: string | null;
  especificaciones: string | null;
  deviceId: string | null;
  motivo?: string | null;
}

// Sensor o actuador del sistema de acuaponía (Nueva Naturaleza) que se puede vincular a un cultivo
export interface DispositivoNuevaNaturaleza {
  id: string;
  nombre: string;
  tipo: 'Sensor' | 'Actuador';
  sistema: string;
  estado: string;
  modoAutomaticoAcuaponia: boolean;
  ultimoValor: number | null;
  fechaUltimoValor: string | null;
}

export interface AccionarActuador {
  encender: boolean;
  motivo: string;
  alarmaCultivoId: string | null;
}

export interface UmbralCultivo {
  id: string;
  cultivoId: string;
  cultivoNombre: string;
  sensorCultivoId: string;
  sensorNombre: string;
  variable: string;
  unidad: string;
  etapaCultivoId: string | null;
  etapaNombre: string | null;
  valorMin: number | null;
  valorMax: number | null;
  nivelGravedad: string;
  actuadorCultivoId: string | null;
  actuadorNombre: string | null;
  encenderActuador: boolean | null;
  revertirAlNormalizar: boolean;
  minutosEscalamientoSms: number | null;
  activo: boolean;
  descripcion: string | null;
  fechaCreacion: string;
}

export interface UmbralCultivoCreate {
  cultivoId: string;
  sensorCultivoId: string;
  etapaCultivoId: string | null;
  valorMin: number | null;
  valorMax: number | null;
  nivelGravedad: string;
  actuadorCultivoId: string | null;
  encenderActuador: boolean | null;
  revertirAlNormalizar: boolean;
  minutosEscalamientoSms: number | null;
  descripcion: string | null;
  motivo?: string | null;
}

export interface BitacoraAlarmaCultivo {
  id: string;
  alarmaCultivoId: string;
  accion: string;
  comentario: string | null;
  usuarioNombre: string;
  fecha: string;
}

export interface AlarmaCultivo {
  id: string;
  cultivoId: string;
  cultivoNombre: string;
  umbralCultivoId: string;
  sensorCultivoId: string;
  etapaNombre: string | null;
  variable: string;
  valorRegistrado: number;
  ultimoValor: number;
  ocurrencias: number;
  mensaje: string;
  nivelGravedad: string;
  estado: 'Activa' | 'Vista' | 'EnAtencion' | 'Resuelta' | 'Anulada';
  fechaDisparo: string;
  fechaUltimaOcurrencia: string;
  vistaPorNombre: string | null;
  fechaVista: string | null;
  atendidaPorNombre: string | null;
  fechaAtencion: string | null;
  resueltaPorNombre: string | null;
  fechaResolucion: string | null;
  solucion: string | null;
  smsEnviado: boolean;
  fechaSms: string | null;
  bitacora: BitacoraAlarmaCultivo[];
}

export interface EventoDispositivoCultivo {
  id: string;
  cultivoId: string;
  tipoDispositivo: 'Sensor' | 'Actuador';
  dispositivoId: string;
  nombreDispositivo: string;
  accion: string;
  origen: 'Automatico' | 'Manual';
  motivo: string;
  usuarioNombre: string;
  alarmaCultivoId: string | null;
  umbralCultivoId: string | null;
  fecha: string;
}

// Auditoría de configuración: quién creó, modificó, eliminó, desactivó o reactivó y por qué
export interface AuditoriaCultivo {
  id: string;
  cultivoId: string | null;
  tipoEntidad: 'Cultivo' | 'Etapa' | 'Sensor' | 'Actuador' | 'Umbral' | 'Alarma';
  entidadId: string;
  nombreEntidad: string;
  accion: string;
  detalle: string | null;
  motivo: string | null;
  usuarioNombre: string;
  fecha: string;
}

// Mensajes en tiempo real que llegan por el CultivoHub
export interface AlarmaCultivoEnVivo {
  tipo: 'Nueva' | 'Actualizada';
  id: string;
  cultivoId: string;
  cultivoNombre?: string;
  variable?: string;
  valor?: number;
  nivelGravedad?: string;
  mensaje?: string;
  estado: string;
  fechaDisparo?: string;
}

export interface ActuadorCultivoEnVivo {
  actuadorId: string;
  cultivoId: string;
  encendido: boolean;
  modo: string;
  activo: boolean;
}
