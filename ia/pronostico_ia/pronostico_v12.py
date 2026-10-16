"""Carga los modelos REAJUSTADOS y emite un solo día civil desde estación 3."""
from pathlib import Path
import argparse
import json
import numpy as np
import pandas as pd
from datos_v12 import ROOT, LOCAL, FEATURES, L, H, load_local, transform, inverse, dump

OUT = ROOT / 'resultados'


def load_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))


def latest_input(frames, scaler, n_features, origin=None):
    frame = frames['estacion_3']
    cols = ['in_' + col for col in FEATURES[:n_features]]
    if origin is None:
        valid = frame[cols].notna().all(axis=1)
        origin = frame.index[valid].max()
    history = frame.reindex(pd.date_range(origin - pd.Timedelta(hours=23), origin, freq='h'))[cols]
    if len(history) != 24 or not history.notna().all().all():
        raise ValueError('La estación 3 no tiene 24 horas recientes completas; no se inventan entradas.')
    return transform(history.to_numpy()[None], scaler), origin


def emit(dataset=LOCAL):
    from tensorflow import keras
    from modelo_v12 import mc_predict, physical_outputs
    adjustments = load_json(OUT / 'ajustes_operacion.json')
    frames, _ = load_local(saved=adjustments, cutoff=pd.Timestamp(adjustments['ajuste_hasta_exclusivo']), path=dataset)
    mapping = load_json(OUT / 'operacion_mapeo.json')
    event_model = load_json(OUT / 'modelo_evento.json')['modelo']
    principal_scaler = load_json(OUT / mapping[event_model] / 'transformacion.json')
    x, origin = latest_input(frames, principal_scaler, 4)
    dates = pd.date_range(origin + pd.Timedelta(hours=1), periods=48, freq='h')
    predictions, probabilities, policies = {}, None, load_json(OUT / 'politica.json')
    diagnostics = {}
    for key, folder in mapping.items():
        specs = load_json(OUT / folder / 'resumen.json')
        n = specs['n_features']
        model = keras.models.load_model(OUT / folder / 'modelo.keras', compile=False)
        scaler = load_json(OUT / folder / 'transformacion.json')
        history, _ = latest_input(frames, scaler, n, origin)
        raw = mc_predict(model, history, passes=120, seed=1042)
        numeric, probability = physical_outputs(raw, scaler, specs['positive_weight'], conditional=True)
        numeric = numeric[:, 0]
        numeric[:, (dates.hour <= 4) | (dates.hour >= 19), 2] = 0.
        predictions[key] = numeric
        if probability is not None and key == event_model:
            probabilities = probability[:, 0]
        mean = numeric.mean(0)
        diagnostics[key] = {FEATURES[j]: float(np.max(np.abs(mean[:24, j] - mean[24:, j]))) for j in range(n)}
        del model
        keras.backend.clear_session()
    previous = np.tile(inverse(x[0], principal_scaler), (2, 1))
    median = pd.read_csv(OUT / 'medianas_operacion.csv', index_col=0)
    median.index = median.index.astype(int)
    base = {'persistencia_24h': previous, 'mediana_horaria': median.reindex(dates.hour)[FEATURES].to_numpy()}
    selected = np.empty_like(predictions[event_model])
    for j, feature in enumerate(FEATURES):
        candidate = policies[feature]
        if candidate == 'cero_publicado':
            selected[..., j] = 0.
        elif candidate in base:
            selected[..., j] = base[candidate][..., j]
        else:
            name, alpha = candidate.split('@')
            alpha = float(alpha)
            selected[..., j] = alpha * predictions[name][..., j] + (1 - alpha) * base['persistencia_24h'][..., j]
    point = selected.mean(axis=0)
    p = probabilities.mean(0)
    threshold = load_json(OUT / 'umbral_evento.json')['umbral']
    rain_guard = load_json(OUT / 'control_habilidad_lluvia.json')
    cutoff = pd.Timestamp(adjustments['ajuste_hasta_exclusivo'])
    prior_light = frames['estacion_3'].loc[frames['estacion_3'].index < cutoff, 'rad'].dropna()
    reference = prior_light.groupby(prior_light.index.hour).quantile(.9)
    state = []
    for h, date in enumerate(dates):
        if rain_guard['habilitar_senal_lluvia_en_condicion'] and p[h] >= threshold:
            state.append('SEÑAL_LLUVIOSA_EXPERIMENTAL')
        elif 6 <= date.hour < 18:
            state.append('SOLEADO_ORIENTATIVO' if point[h, 2] >= max(100, .65 * reference.get(date.hour, 0)) else 'NUBLADO_ORIENTATIVO')
        else:
            state.append('CIELO_NO_ESTIMABLE')
    hourly = pd.DataFrame(point, index=dates, columns=FEATURES)
    hourly.index.name = 'fecha_hora'
    for j, feature in enumerate(FEATURES):
        hourly[feature + '_p05_pesos'] = np.quantile(selected[..., j], .05, axis=0)
        hourly[feature + '_p95_pesos'] = np.quantile(selected[..., j], .95, axis=0)
    hourly['lluv_estimacion_modelo_hurdle'] = predictions[event_model][..., 3].mean(axis=0)
    hourly['indice_pulso_publicado_modelo'] = p
    hourly['condicion_orientativa'] = state
    hourly['confianza_meteorologica_calibrada'] = False
    hourly.to_csv(OUT / 'pronostico_horario_48h.csv', encoding='utf-8-sig')
    day = origin.normalize() + pd.Timedelta(days=1)
    chosen = np.flatnonzero(dates.normalize() == day)
    if len(chosen) != 24:
        raise ValueError('No se obtuvo el siguiente día civil completo')
    rows = []
    periods = ['Madrugada', 'Mañana', 'Tarde', 'Noche']
    for b, period in enumerate(periods):
        indices = chosen[b * 6:(b + 1) * 6]
        values = point[indices]
        states = np.asarray(state)[indices]
        if np.count_nonzero(states == 'SEÑAL_LLUVIOSA_EXPERIMENTAL') >= 3:
            condition = 'Posible lluvia*'
        elif np.count_nonzero(states == 'SOLEADO_ORIENTATIVO') >= 3:
            condition = 'Soleado*'
        elif np.count_nonzero(states == 'NUBLADO_ORIENTATIVO') >= 3:
            condition = 'Nublado*'
        else:
            condition = 'Cielo no estimable'
        rows.append({'fecha': str(day.date()), 'franja': period, 'condicion_orientativa': condition,
                     'tem_media_c': float(values[:, 0].mean()), 'tem_min_curva_c': float(values[:, 0].min()),
                     'tem_max_curva_c': float(values[:, 0].max()), 'hum_media_pct': float(values[:, 1].mean()),
                     'rad_media_lux': float(values[:, 2].mean()), 'lluv_publicada_parcial_mm': float(values[:, 3].sum()),
                     'lluv_modelo_hurdle_parcial_mm': float(hourly.iloc[indices].lluv_estimacion_modelo_hurdle.sum()),
                     'indice_max_pulso_publicado': float(p[indices].max()),
                     'confianza_meteorologica_calibrada': False})
    daily = pd.DataFrame(rows)
    daily.to_csv(OUT / 'pronostico_diario.csv', index=False, encoding='utf-8-sig')
    dump(OUT / 'diagnostico_horizonte.json', {'max_diferencia_entre_h_y_h_mas_24': diagnostics,
                                             'salida_forzada_a_repetir_dias': False,
                                             'nota': 'Una variable seleccionada como referencia simple sí puede conservar periodicidad.'})
    dump(OUT / 'pronostico_metadatos.json', {
        'ubicacion': 'Universidad de Cundinamarca, extensión Facatativá', 'estacion_referencia': 3,
        'ultimo_intervalo_entrada_inicio': str(origin), 'fin_exclusivo_entrada': str(origin + pd.Timedelta(hours=1)),
        'dia_pronosticado': str(day.date()), 'horizonte_horas': 48,
        'fuentes': ['nasa_power', 'openmeteo', 'estacion_1', 'estacion_2', 'estacion_3'],
        'sensores_utilizados': FEATURES, 'luz_salida_unidad': 'lux',
        'lluvia': 'mm publicados parciales, no total meteorológico real',
        'estados_clima': 'Reglas orientativas sobre luz prevista y señal de publicaciones; no clasificador validado de tres clases.',
        'indice_pulso': 'Score del modelo para registro de pulsos; no confianza meteorológica calibrada ni probabilidad de la franja.',
        'rangos_p05_p95': 'Sólo variación de pesos variacionales; no intervalos predictivos totales.',
        'metricas_corresponden_a_modelos_de_evaluacion': True,
        'control_habilidad_lluvia': rain_guard,
        'modelo_operativo_reajustado_con_todos_los_datos_validos': True,
        'politica': policies})
    text = [f'# Pronóstico experimental · {day:%d/%m/%Y}', '',
            'Universidad de Cundinamarca, extensión Facatativá. Estación 3 de referencia.',
            f'Última hora de entrada: {origin} (inicio del intervalo).', '',
            '| Franja | Condición orientativa | Temperatura media | Humedad | Luz | Lluvia publicada parcial |',
            '|---|---|---:|---:|---:|---:|']
    for row in daily.itertuples():
        text.append(f'| {row.franja} | {row.condicion_orientativa} | {row.tem_media_c:.1f} °C | {row.hum_media_pct:.0f} % | {row.rad_media_lux:.0f} lux | {row.lluv_publicada_parcial_mm:.2f} mm |')
    text += ['', '*Condición experimental: no existe clasificación meteorológica validada ni confianza calibrada.',
             'Las cantidades de lluvia describen registros parciales del firmware. Cero no significa ausencia real de lluvia.',
             'Los modelos se reajustaron con el historial válido completo; las métricas guardadas pertenecen a modelos anteriores separados para evaluación.']
    (OUT / 'PRONOSTICO_DIARIO.md').write_text('\n'.join(text), encoding='utf-8')
    print(daily.to_string(index=False))
    from graficas_v12 import generate
    generate()
    return daily


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--dataset', type=Path, default=LOCAL)
    emit(parser.parse_args().dataset)
