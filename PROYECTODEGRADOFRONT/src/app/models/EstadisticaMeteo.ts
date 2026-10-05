export interface EstadisticaMeteo {
  estacionNumero: number;
  variable: string;
  unidad: string;
  cantidadSolicitada: number;
  cantidadUsada: number;
  fechaDesde: string | null;
  fechaHasta: string | null;
  media: number | null;
  mediana: number | null;
  modas: number[];
  frecuenciaModa: number;
  minimo: number | null;
  maximo: number | null;
  rango: number | null;
  desviacionEstandar: number | null;
  valores: number[];
}
