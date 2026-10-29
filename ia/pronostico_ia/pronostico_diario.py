"""Pronóstico diario operativo (botón "Pronóstico del día" del panel IA).

1. Agrega por hora las lecturas crudas que exporta el backend (Lecturas_Meteo) y las une al
   histórico horario dataset_local_mejorado.csv, sin modificar el archivo original.
2. Reajusta con todos los datos válidos sólo lo que depende de ellos: ajustes entre estaciones,
   escalas, LSTM final de la política, ridge, medianas horarias y luz de referencia.
   La política de modelos, los rangos de error y la LSTM de lluvia son los validados y no cambian.
3. Emite el pronóstico del día siguiente con los umbrales de cielo calibrados por la retroalimentación.

Todo el estado operativo vive en operacion/ (fuera de git); modelos/ queda como la versión entregada.
El resultado se escribe como JSON en --salida para que lo lea el backend.
"""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL', '2')
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import time
import warnings
import numpy as np
import pandas as pd
import datos_v12 as d
import entrenar_v12 as trainer
import entrenar_pronostico as ep
from predecir import emitir

ROOT = Path(__file__).resolve().parent
OPERACION = ROOT / 'operacion'
MODELOS_OP = OPERACION / 'modelos'
DATASET_OP = OPERACION / 'dataset_operacion.csv'
COLUMNAS = ['estacion', 'fecha_hora', 'tem', 'hum', 'rad', 'lluv', 'temperatura_min_c', 'temperatura_max_c',
            'radiacion_min_lux', 'radiacion_max_lux', 'n_mediciones', 'primera_medicion', 'ultima_medicion',
            'n_tem_validas', 'n_hum_validas', 'n_rad_validas', 'minutos_con_pulsos_publicados',
            'soporte_temporal_minutos', 'hora_parcial', 'radiacion_constante', 'origenes']
VARIABLES = {'temperatura': 'tem', 'humedad': 'hum', 'radiacion': 'rad', 'lluvia': 'lluv'}
# LSTM de la política: entradas, transferencia completa y pesos históricos de inicialización.
ESPECIFICACION = {'lstm_local': (4, False, None),
                  'lstm_historicos': (4, True, ROOT / 'preentrenado/historicos_final.keras'),
                  'lstm_auxiliar': (3, True, ROOT / 'preentrenado/historicos_final.keras')}


def agregar_por_hora(lecturas, corte):
    """Mismo esquema horario de dataset_local_mejorado.csv a partir de lecturas crudas.

    Una medición es un mensaje MQTT: todas sus variables comparten estación y FechaHora.
    La lluvia publicada es mm/min; se toma un valor por minuto (las publicaciones repetidas
    dentro del mismo minuto repiten la tasa) y se suma en la hora."""
    r = pd.read_csv(lecturas, parse_dates=['fecha_hora'])
    r = r[r.variable.isin(VARIABLES)].copy()
    if r.empty:
        return pd.DataFrame(columns=COLUMNAS)
    r['valor'] = pd.to_numeric(r.valor, errors='coerce')
    m = r.pivot_table(index=['estacion', 'fecha_hora'], columns='variable', values='valor', aggfunc='mean')
    m = m.rename(columns=VARIABLES).reindex(columns=list(VARIABLES.values())).reset_index()
    m.loc[~m.tem.between(-10, 50), 'tem'] = np.nan
    m.loc[~m.hum.between(0, 100), 'hum'] = np.nan
    m.loc[m.rad.lt(0), 'rad'] = np.nan
    m['hora'] = m.fecha_hora.dt.floor('h')
    g = m.groupby(['estacion', 'hora'])
    h = pd.DataFrame({
        'tem': g.tem.mean(), 'hum': g.hum.mean(), 'rad': g.rad.mean(),
        'temperatura_min_c': g.tem.min(), 'temperatura_max_c': g.tem.max(),
        'radiacion_min_lux': g.rad.min(), 'radiacion_max_lux': g.rad.max(),
        'n_mediciones': g.size(), 'primera_medicion': g.fecha_hora.min(), 'ultima_medicion': g.fecha_hora.max(),
        'n_tem_validas': g.tem.count(), 'n_hum_validas': g.hum.count(), 'n_rad_validas': g.rad.count()})
    lluvia = m.dropna(subset=['lluv']).assign(minuto=lambda x: x.fecha_hora.dt.floor('min'))
    lluvia = lluvia.groupby(['estacion', 'minuto']).lluv.max().reset_index()
    lluvia['hora'] = lluvia.minuto.dt.floor('h')
    gl = lluvia.groupby(['estacion', 'hora']).lluv
    h['lluv'] = gl.sum()
    h['minutos_con_pulsos_publicados'] = lluvia.assign(p=lluvia.lluv.gt(0)).groupby(['estacion', 'hora']).p.sum()
    h['soporte_temporal_minutos'] = (h.ultima_medicion - h.primera_medicion).dt.total_seconds() / 60
    h = h.reset_index().rename(columns={'hora': 'fecha_hora'})
    # Una hora que no había terminado al exportar no se usa (load_local exige hora_parcial False).
    h['hora_parcial'] = (h.fecha_hora + pd.Timedelta(hours=1)) > corte
    # Sensor pegado: el mismo valor de luz distinto de cero en toda la hora.
    h['radiacion_constante'] = h.n_rad_validas.ge(2) & h.radiacion_min_lux.eq(h.radiacion_max_lux) & h.radiacion_max_lux.gt(0)
    # La primera hora exportada suele estar recortada; se descarta para no reemplazar una completa.
    primera = h.groupby('estacion').fecha_hora.transform('min')
    h = h[h.fecha_hora > primera]
    h['origenes'] = 'lecturas_meteo'
    return h.reindex(columns=COLUMNAS)


def preparar_dataset(lecturas, corte):
    base = pd.read_csv(d.LOCAL, parse_dates=['fecha_hora'])
    base = base.reindex(columns=COLUMNAS)
    nuevas = agregar_por_hora(lecturas, corte) if lecturas else pd.DataFrame(columns=COLUMNAS)
    if len(nuevas):
        # Las horas de la base de datos reemplazan a las del histórico; el resto se conserva.
        claves = pd.MultiIndex.from_frame(nuevas[['estacion', 'fecha_hora']])
        base = base[~pd.MultiIndex.from_frame(base[['estacion', 'fecha_hora']]).isin(claves)]
    datos = (pd.concat([base, nuevas], ignore_index=True) if len(nuevas) else base).sort_values(['estacion', 'fecha_hora'])
    for col in ['hora_parcial', 'radiacion_constante']:
        datos[col] = datos[col].astype(str).eq('True')
    OPERACION.mkdir(exist_ok=True)
    datos.to_csv(DATASET_OP, index=False)
    return {'horas_historico': int(len(base)), 'horas_nuevas': int(len(nuevas)),
            'ultima_hora': str(datos.fecha_hora.max())}


def reentrenar():
    """Reajuste final de entrenar_pronostico.main() con los datos operativos."""
    if not MODELOS_OP.exists():
        shutil.copytree(ROOT / 'modelos', MODELOS_OP)
    trainer.OUT = MODELOS_OP
    trainer.LOCAL = DATASET_OP          # la huella de fit_run detecta si los datos cambiaron
    local, _ = d.load_local(path=DATASET_OP)
    latest = max(g.index.max() for g in local.values()) + pd.Timedelta(hours=1)
    full, adjfull = d.load_local(latest, path=DATASET_OP)
    finalscale = d.fit_scaler(full, latest, 'lux')
    d.dump(MODELOS_OP / 'ajustes_operacion.json', adjfull)
    d.dump(MODELOS_OP / 'escala_operacion.json', finalscale)
    policy = ep.read(MODELOS_OP / 'politica.json')
    corridas = ep.read(ROOT / 'resultados/resumen_entrenamiento.json')['corridas']
    required = sorted({v.replace('_mezcla50', '') for v in policy.values() if v.startswith('lstm_')})
    mapping, info = {}, {}
    for name in required:
        n, fulltransfer, pretrained = ESPECIFICACION[name]
        b = ep.targets_thl(d.make_windows(full, finalscale, end=latest, n_features=n))
        inicio = time.time()
        with warnings.catch_warnings():
            warnings.simplefilter('ignore', RuntimeWarning)
            resumen = trainer.fit_run('final_' + name, b, None, finalscale, n_features=n, pretrained=pretrained,
                                      full_transfer=fulltransfer, pretrain=True, fixed=True,
                                      epochs=corridas[name]['epoca_restaurada'])
        mapping[name] = {'carpeta': 'final_' + name, 'entradas': n, 'ventanas': d.catalog(b)}
        info[name] = {'ventanas': len(b['x']), 'segundos': round(time.time() - inicio, 2),
                      'epocas': resumen['epocas']}
    for alpha in [10, 100]:
        b = ep.targets_thl(d.make_windows(full, finalscale, end=latest, n_features=3))
        d.dump(MODELOS_OP / f'ridge_{alpha}_final.json', ep.ridge_fit(b, finalscale, float(alpha)))
    train = full['estacion_3'].loc[full['estacion_3'].index < latest, d.FEATURES[:3]]
    train.groupby(train.index.hour).median().to_csv(MODELOS_OP / 'medianas_operacion.csv')
    train.groupby(train.index.hour).quantile(.9).to_csv(MODELOS_OP / 'luz_referencia.csv')
    d.dump(MODELOS_OP / 'mapeo.json', mapping)
    return {'modelos': info, 'politica': policy, 'datos_hasta_exclusivo': str(latest)}


def origen_reciente():
    """Última hora de la estación 3 con 24 horas previas completas en las 4 entradas.

    predecir.py sólo mira la última hora válida; un hueco de una hora dejaría sin pronóstico,
    así que se busca hacia atrás la ventana completa más reciente. No se rellenan huecos."""
    ra = ep.read(ROOT / 'lluvia/ajustes_estaciones_final.json')
    frames, _ = d.load_local(saved=ra, cutoff=pd.Timestamp(ra['ajuste_hasta_exclusivo']), path=DATASET_OP)
    completo = frames['estacion_3'][['in_' + c for c in d.FEATURES]].notna().all(axis=1)
    ventanas = completo.astype(int).rolling(24).sum().eq(24)
    if not ventanas.any():
        raise ValueError('La estación 3 no tiene ninguna ventana de 24 horas completas; no se inventan entradas.')
    return ventanas[ventanas].index.max()


def version_modelo():
    h = hashlib.sha256()
    for name in sorted(ep.read(MODELOS_OP / 'mapeo.json')):
        h.update((MODELOS_OP / f'final_{name}' / 'modelo.keras').read_bytes())
    h.update((ROOT / 'lluvia/modelo.keras').read_bytes())
    return h.hexdigest()[:12]


def limpio(valor):
    if isinstance(valor, (float, np.floating)):
        return None if not np.isfinite(valor) else float(valor)
    if isinstance(valor, (np.integer,)):
        return int(valor)
    return valor


def main():
    parser = argparse.ArgumentParser(description='Reentrena con las lecturas recientes y pronostica el día siguiente.')
    parser.add_argument('--lecturas', type=Path, help='CSV estacion,variable,fecha_hora,valor exportado de Lecturas_Meteo')
    parser.add_argument('--corte', help='Fecha y hora de la exportación (horas posteriores se consideran parciales)')
    parser.add_argument('--umbral-lluvia', type=float, default=.1)
    parser.add_argument('--factor-sol', type=float, default=.65)
    parser.add_argument('--salida', type=Path, required=True)
    args = parser.parse_args()
    corte = pd.Timestamp(args.corte) if args.corte else pd.Timestamp.now()
    datos = preparar_dataset(args.lecturas, corte)
    entrenamiento = reentrenar()
    destino = OPERACION / 'resultados'
    origen = origen_reciente()
    hourly, daily = emitir(DATASET_OP, destino, models=MODELOS_OP, umbral_lluvia=args.umbral_lluvia,
                           factor_sol=args.factor_sol, origen=origen)
    meta = ep.read(destino / 'pronostico_metadatos.json')
    referencia = pd.read_csv(MODELOS_OP / 'luz_referencia.csv', index_col=0).rad
    horario = []
    for t, v in hourly.iterrows():
        fila = {'fecha_hora': t.strftime('%Y-%m-%dT%H:%M:%S'), 'luz_referencia': referencia.get(t.hour, np.nan)}
        fila.update({k: v[k] for k in hourly.columns if k != 'confianza_clase_calibrada'})
        horario.append({k: limpio(x) for k, x in fila.items()})
    franjas = [{k: limpio(x) for k, x in r.items() if k != 'confianza_clase_calibrada'}
               for r in daily.to_dict('records')]
    advertencia = None
    if pd.Timestamp(meta['dia_pronosticado']) <= corte.normalize():
        advertencia = (f"La última ventana completa de la estación 3 termina el {origen:%d/%m %H:%M}; "
                       f"el pronóstico corresponde al {meta['dia_pronosticado']}, que ya empezó.")
    salida = {'origen': meta['origen_intervalo_entrada'], 'dia_pronosticado': meta['dia_pronosticado'],
              'advertencia': advertencia,
              'umbral_lluvia': args.umbral_lluvia, 'factor_sol': args.factor_sol,
              'version_modelo': version_modelo(), 'datos': datos, 'entrenamiento': entrenamiento,
              'horario': horario, 'franjas': franjas}
    args.salida.parent.mkdir(parents=True, exist_ok=True)
    args.salida.write_text(json.dumps(salida, ensure_ascii=False, indent=1), encoding='utf-8')
    print('OK', meta['dia_pronosticado'], flush=True)


if __name__ == '__main__':
    try:
        main()
    except ValueError as e:
        # Mensaje legible para el panel (el backend muestra la última línea de stderr).
        print('ERROR:', e, file=__import__('sys').stderr, flush=True)
        raise SystemExit(2)
