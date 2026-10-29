"""Históricos -> inicialización local -> evaluación -> reajuste de operación."""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL', '2')
from pathlib import Path
import gc
import json
import time
import numpy as np
import pandas as pd
from datos_v12 import *

OUT = ROOT / 'resultados'


def fit_run(name, train, validation, scaler, n_features=4, pretrained=None,
            epochs=100, pretrain=False, fixed=False, full_transfer=False):
    from tensorflow import keras
    from modelo_v12 import build_model, transfer_encoder, transfer_patterns, physical_outputs
    folder = OUT / name
    folder.mkdir(parents=True, exist_ok=True)
    events = train['event']
    positives = float((events[..., 0] * events[..., 1]).sum())
    negatives = float(events[..., 1].sum() - positives)
    positive_weight = float(np.clip(negatives / max(positives, 1), 1, 8)) if not pretrain else 1.
    effective = int(train['y'][..., n_features:].sum())
    fingerprint = {'version': 3, 'objetivo':'THL', 'batch':64, 'name': name, 'n_features': n_features, 'n': len(train['x']),
                   'epochs': epochs, 'fixed': fixed, 'pretrain': pretrain,
                   'sha_local': digest(LOCAL), 'sha_hist': digest(HISTORICAL),
                   'encoder': digest(pretrained) if pretrained else None}
    if full_transfer:
        fingerprint['full_transfer'] = True
    if (folder / 'resumen.json').exists() and (folder / 'modelo.keras').exists():
        saved = json.loads((folder / 'resumen.json').read_text(encoding='utf-8'))
        if saved['fingerprint'] == fingerprint:
            print('Reutilizado:', name, flush=True)
            return saved
    model = build_model(n_features, effective, positive_weight=positive_weight, pretrain=pretrain)
    if pretrained:
        source = keras.models.load_model(pretrained, compile=False)
        (transfer_patterns if full_transfer else transfer_encoder)(source, model)
        del source
    def targets(block):
        result = {'variables': block['y']}
        if n_features == 4:
            result['lluvia_logit'] = block['event']
        return result

    class PhysicalScore(keras.callbacks.Callback):
        def on_epoch_end(self, epoch, logs=None):
            output = {k: np.asarray(v) for k, v in self.model(validation['x'], training=False).items()}
            point, _ = physical_outputs(output, scaler, positive_weight, conditional=not pretrain)
            error = np.abs(point - validation['raw'])
            scale = np.asarray(scaler['physical_std'])[:n_features]
            score = float(np.nanmean(np.nanmean(error, axis=(0, 1)) / scale))
            logs['val_score_fisico'] = score

    callbacks = []
    early = None
    if validation is not None and not fixed:
        early = keras.callbacks.EarlyStopping(monitor='val_score_fisico', mode='min',
                                              patience=12, min_delta=.0001,
                                              restore_best_weights=True)
        callbacks.extend([PhysicalScore(), early])
    callbacks.append(keras.callbacks.CSVLogger(str(folder / 'historial.csv')))
    print(f'ENTRENANDO {name}: {len(train["x"])} ventanas, {n_features} entradas, {model.count_params()} parámetros', flush=True)
    start = time.time()
    history = model.fit(train['x'], targets(train),
                        validation_data=(validation['x'], targets(validation)) if validation is not None else None,
                        epochs=epochs, batch_size=64, shuffle=True,
                        verbose=2, callbacks=callbacks)
    model.save(folder / 'modelo.keras')
    best = int(early.best_epoch + 1) if early is not None else epochs
    summary = {'fingerprint': fingerprint, 'datos': catalog(train),
               'validacion': catalog(validation) if validation is not None else None,
               'epocas': len(history.history['loss']), 'epoca_restaurada': best,
               'parametros': model.count_params(), 'positive_weight': positive_weight,
               'n_features': n_features, 'preentrenamiento': pretrain,
               'inicializacion_encoder': str(pretrained) if pretrained else None,
               'score_validacion': float(history.history['val_score_fisico'][best - 1]) if early is not None else None,
               'segundos': round(time.time() - start, 2), 'es_modelo_final_sin_prueba_independiente': fixed}
    dump(folder / 'resumen.json', summary)
    dump(folder / 'transformacion.json', scaler)
    train['meta'].to_csv(folder / 'muestras_train.csv', index=False, encoding='utf-8-sig')
    if validation is not None:
        validation['meta'].to_csv(folder / 'muestras_val.csv', index=False, encoding='utf-8-sig')
    del model
    keras.backend.clear_session()
    gc.collect()
    return summary


def predict_run(name, block, passes=50):
    from tensorflow import keras
    from modelo_v12 import mc_predict, physical_outputs
    folder = OUT / name
    summary = json.loads((folder / 'resumen.json').read_text(encoding='utf-8'))
    scaler = json.loads((folder / 'transformacion.json').read_text(encoding='utf-8'))
    model = keras.models.load_model(folder / 'modelo.keras', compile=False)
    outputs = mc_predict(model, block['x'], passes=passes)
    values, probability = physical_outputs(outputs, scaler, summary['positive_weight'], conditional=True)
    for i, row in enumerate(block['meta'].itertuples(index=False)):
        hours = pd.date_range(row.inicio_objetivo, periods=48, freq='h').hour
        night = (hours <= 4) | (hours >= 19)
        values[:, i, night, 2] = 0.
    del model
    keras.backend.clear_session()
    return values, probability


def align(block, origins):
    indices = block['meta'].set_index('origen').loc[origins].reset_index()
    positions = [int(block['meta'].index[block['meta'].origen.eq(x)][0]) for x in origins]
    return {key: block[key][positions] for key in ['x', 'y', 'event', 'raw']} | {'meta': indices}


def baselines(block, scaler, local):
    previous = inverse(block['x'], scaler)
    seasonal = np.tile(previous, (1, 2, 1))
    training = local['estacion_3'].loc[local['estacion_3'].index < TRAIN_END, FEATURES]
    table = training.groupby(training.index.hour).median()
    medians = []
    for row in block['meta'].itertuples(index=False):
        hours = pd.date_range(row.inicio_objetivo, periods=48, freq='h').hour
        medians.append(table.reindex(hours).to_numpy())
    median = np.asarray(medians)
    return {'persistencia_24h': seasonal, 'mediana_horaria': median}


def choose_policy(predictions, block, base):
    truth = block['raw']
    rows, policy = [], {}
    for j, feature in enumerate(FEATURES):
        candidates = {name: value[..., j] for name, value in base.items()}
        for name, values in predictions.items():
            if values.shape[-1] <= j:
                continue
            for alpha in [.5, 1.]:
                candidates[f'{name}@{alpha}'] = values[..., j] * alpha + base['persistencia_24h'][..., j] * (1 - alpha)
        if j == 3:
            candidates['cero_publicado'] = np.zeros_like(truth[..., j])
        valid = np.isfinite(truth[..., j])
        for name, values in candidates.items():
            error = values[valid] - truth[..., j][valid]
            rows.append({'variable': feature, 'candidato': name, 'mae': float(np.abs(error).mean()),
                         'rmse': float(np.sqrt(np.mean(error ** 2))),
                         'criterio': 'rmse' if j == 3 else 'mae', 'n': int(valid.sum())})
        options = [row for row in rows if row['variable'] == feature]
        best = min(options, key=lambda x: x[x['criterio']])
        policy[feature] = best['candidato']
    return policy, pd.DataFrame(rows)


def apply_policy(policy, predictions, base):
    selected = np.empty_like(base['persistencia_24h'])
    for j, feature in enumerate(FEATURES):
        name = policy[feature]
        if name == 'cero_publicado':
            selected[..., j] = 0.
        elif name in base:
            selected[..., j] = base[name][..., j]
        else:
            model, weight = name.split('@')
            weight = float(weight)
            selected[..., j] = predictions[model][..., j] * weight + base['persistencia_24h'][..., j] * (1 - weight)
    return selected


def evaluate(predictions, block, base, policy, probabilities, threshold):
    selected = apply_policy(policy, predictions, base)
    all_predictions = {**predictions, **base, 'sistema_seleccionado': selected,
                       'cero_publicado': np.zeros_like(selected)}
    details = []
    for i, meta in enumerate(block['meta'].itertuples(index=False)):
        for h, date in enumerate(pd.date_range(meta.inicio_objetivo, periods=48, freq='h')):
            for j, feature in enumerate(FEATURES):
                actual = block['raw'][i, h, j]
                if not np.isfinite(actual):
                    continue
                row = {'origen': meta.origen, 'fecha_objetivo': date, 'horizonte': h + 1,
                       'variable': feature, 'observado_e3': float(actual)}
                for name, values in all_predictions.items():
                    if values.shape[-1] > j and (name != 'cero_publicado' or j == 3):
                        row[name] = float(values[i, h, j])
                if j == 3:
                    row['prob_pulso_publicado_modelo'] = float(probabilities[i, h])
                    row['senal_pulso_publicado'] = bool(probabilities[i, h] >= threshold)
                details.append(row)
    detail = pd.DataFrame(details)
    detail.to_csv(OUT / 'evaluacion_detallada.csv', index=False, encoding='utf-8-sig')
    rows = []
    for feature in FEATURES:
        for label, lo, hi in [('1–48 h', 1, 48), ('1–24 h', 1, 24), ('25–48 h', 25, 48)]:
            data = detail[detail.variable.eq(feature) & detail.horizonte.between(lo, hi)]
            for name in all_predictions:
                if name not in data:
                    continue
                sub = data.dropna(subset=[name])
                if sub.empty:
                    continue
                actual = sub.observado_e3.to_numpy()
                err = sub[name].to_numpy() - actual
                denominator = ((actual - actual.mean()) ** 2).sum()
                rows.append({'variable': feature, 'modelo': name, 'horizonte': label,
                             'mae': float(np.abs(err).mean()), 'rmse': float(np.sqrt((err ** 2).mean())),
                             'r2': float(1 - (err ** 2).sum() / denominator) if denominator > 0 else None,
                             'n_pares': len(actual), 'horas_unicas': int(sub.fecha_objetivo.nunique()),
                             'origenes': int(sub.origen.nunique())})
    metrics = pd.DataFrame(rows)
    metrics.to_csv(OUT / 'metricas_variables.csv', index=False, encoding='utf-8-sig')
    rain = detail[detail.variable.eq('lluv')]
    y = rain.observado_e3.gt(0).to_numpy()
    p = rain.prob_pulso_publicado_modelo.to_numpy()
    decision = p >= threshold
    tp, fp, fn, tn = [int(mask.sum()) for mask in [decision & y, decision & ~y, ~decision & y, ~decision & ~y]]
    precision = tp / max(tp + fp, 1)
    recall = tp / max(tp + fn, 1)
    dump(OUT / 'metricas_evento_publicado.json', {
        'objetivo': 'presencia de pulsos PUBLICADOS; un cero no demuestra ausencia real de lluvia',
        'umbral_elegido_solo_en_validacion': threshold, 'tp': tp, 'fp': fp, 'fn': fn, 'tn': tn,
        'precision_publicaciones': precision, 'sensibilidad_publicaciones': recall,
        'f1_publicaciones': 2 * precision * recall / max(precision + recall, 1e-12),
        'brier_publicaciones': float(np.mean((p - y) ** 2)),
        'horas_positivas_unicas': int(rain.loc[y, 'fecha_objetivo'].nunique()),
        'clasificacion_meteorologica_tres_clases_validada': False, 'probabilidad_meteorologica_calibrada': False})
    return metrics


def execute():
    OUT.mkdir(exist_ok=True)
    historical = load_historical()
    hist_val_start = TRAIN_END - pd.DateOffset(months=6)
    hscaler = fit_scaler(historical, hist_val_start, 'W/m²')
    hist_train = make_windows(historical, hscaler, end=hist_val_start, stride=24, conditional_rain=False)
    hist_val = make_windows(historical, hscaler, start=hist_val_start, end=TRAIN_END, stride=24, conditional_rain=False)
    print('HISTÓRICOS:', catalog(hist_train), 'VALIDACIÓN:', catalog(hist_val), flush=True)
    hist_summary = fit_run('preentrenamiento_evaluacion', hist_train, hist_val, hscaler,
                           epochs=12, pretrain=True)
    del hist_train, hist_val
    gc.collect()
    pretrained = OUT / 'preentrenamiento_evaluacion/modelo.keras'
    local, adjustments = load_local()
    export_frames(local, OUT / 'series_locales_evaluacion.csv')
    scaler = fit_scaler(local, TRAIN_END, 'lux')
    dump(OUT / 'ajustes_evaluacion.json', adjustments)
    blocks = {}
    for n in [3, 4]:
        blocks[n] = {name: make_windows(local, scaler, start, end, n_features=n,
                                       stride=1 if name == 'train' else 6,
                                       only_source=None if name == 'train' else 'estacion_3')
                     for name, start, end in [('train', None, TRAIN_END),
                                               ('val', TRAIN_END, VAL_END), ('test', VAL_END, TEST_END)]}
    runs = {}
    runs['local_sin_historicos'] = fit_run('local_sin_historicos', blocks[4]['train'], blocks[4]['val'], scaler)
    runs['principal_historicos'] = fit_run('principal_historicos', blocks[4]['train'], blocks[4]['val'], scaler, pretrained=pretrained)
    runs['auxiliar_historicos'] = fit_run('auxiliar_historicos', blocks[3]['train'], blocks[3]['val'], scaler,
                                       n_features=3, pretrained=pretrained)
    runs['principal_hist_completo'] = fit_run('principal_hist_completo', blocks[4]['train'], blocks[4]['val'], scaler,
                                            pretrained=pretrained, full_transfer=True)
    runs['auxiliar_hist_completo'] = fit_run('auxiliar_hist_completo', blocks[3]['train'], blocks[3]['val'], scaler,
                                           n_features=3, pretrained=pretrained, full_transfer=True)
    common_val = blocks[4]['val']['meta'].origen.tolist()
    common_test = blocks[4]['test']['meta'].origen.tolist()
    val_predictions, test_predictions, val_probabilities, test_probabilities = {}, {}, {}, {}
    for name, n in [('local_sin_historicos', 4), ('principal_historicos', 4), ('auxiliar_historicos', 3),
                    ('principal_hist_completo', 4), ('auxiliar_hist_completo', 3)]:
        vb, tb = align(blocks[n]['val'], common_val), align(blocks[n]['test'], common_test)
        vd, vp = predict_run(name, vb)
        td, tp = predict_run(name, tb)
        val_predictions[name] = vd.mean(axis=0)
        test_predictions[name] = td.mean(axis=0)
        if vp is not None:
            val_probabilities[name] = vp.mean(axis=0)
            test_probabilities[name] = tp.mean(axis=0)
    val_base = baselines(blocks[4]['val'], scaler, local)
    test_base = baselines(blocks[4]['test'], scaler, local)
    # La selección incluye el control: no se fuerza una transferencia adversa.
    operational_val = val_predictions
    policy, selection = choose_policy(operational_val, blocks[4]['val'], val_base)
    selection.to_csv(OUT / 'seleccion_validacion.csv', index=False, encoding='utf-8-sig')
    dump(OUT / 'politica.json', policy)
    y = blocks[4]['val']['raw'][..., 3]
    valid_rain = np.isfinite(y)
    event_candidates = [k for k in val_probabilities if k != 'local_sin_historicos']
    event_model = min(event_candidates, key=lambda k: float(np.mean((val_probabilities[k][valid_rain] - (y[valid_rain] > 0)) ** 2)))
    dump(OUT / 'modelo_evento.json', {'modelo': event_model, 'criterio': 'Brier de publicaciones, sólo validación'})
    p = val_probabilities[event_model]
    valid = np.isfinite(y)
    threshold_scores = []
    for threshold in [.03, .05, .08, .1, .15, .2, .3, .4, .5, .65, .8]:
        truth, pred = y[valid] > 0, p[valid] >= threshold
        tp = int((truth & pred).sum())
        fp = int((~truth & pred).sum())
        fn = int((truth & ~pred).sum())
        score = 2 * tp / max(2 * tp + fp + fn, 1)
        threshold_scores.append({'umbral': threshold, 'f1_publicaciones': score})
    threshold = max(threshold_scores, key=lambda r: (r['f1_publicaciones'], r['umbral']))['umbral']
    dump(OUT / 'umbral_evento.json', {'umbral': threshold, 'seleccion_solo_validacion': threshold_scores})
    from revisar_lluvia import run as check_rain_skill
    check_rain_skill()
    metrics = evaluate(test_predictions, blocks[4]['test'], test_base, policy,
                       test_probabilities[event_model], threshold)
    blocks[4]['test']['meta'].to_csv(OUT / 'muestras_prueba.csv', index=False, encoding='utf-8-sig')
    summary = {'fuentes_historicas': {k: len(v) for k, v in historical.items()},
               'sha_local': digest(LOCAL), 'sha_historicos': digest(HISTORICAL),
               'local_evaluacion': {str(n): {k: catalog(v) for k, v in part.items()} for n, part in blocks.items()},
               'politica': policy, 'umbral_evento_publicado': threshold,
               'evaluacion_modelos_finales': False,
               'nota': 'Los modelos siguientes se reajustan con todo el historial válido; las métricas anteriores pertenecen sólo a modelos de evaluación.'}
    dump(OUT / 'resumen_evaluacion.json', summary)
    print('POLÍTICA VALIDACIÓN:', policy, flush=True)
    print(metrics[(metrics.horizonte == '1–48 h') & metrics.modelo.isin(['principal_historicos', 'sistema_seleccionado', 'persistencia_24h', 'mediana_horaria'])].to_string(index=False), flush=True)
    # Reajuste final: no se evalúa retrospectivamente con datos ya vistos.
    latest = max(frame.index.max() for frame in local.values()) + pd.Timedelta(hours=1)
    local_full, final_adjustments = load_local(latest)
    export_frames(local_full, OUT / 'series_locales_operacion.csv')
    local_scaler = fit_scaler(local_full, latest, 'lux')
    hist_latest = max(frame.index.max() for frame in historical.values()) + pd.Timedelta(hours=1)
    final_hist_scaler = fit_scaler(historical, hist_latest, 'W/m²')
    final_hist = make_windows(historical, final_hist_scaler, end=hist_latest, stride=24, conditional_rain=False)
    hist_final_summary = fit_run('preentrenamiento_final', final_hist, None, final_hist_scaler,
                                 epochs=hist_summary['epoca_restaurada'], pretrain=True, fixed=True)
    del final_hist, historical
    gc.collect()
    final_encoder = OUT / 'preentrenamiento_final/modelo.keras'
    final_summaries = {}
    selected_models = {candidate.split('@')[0] for candidate in policy.values() if '@' in candidate}
    selected_models.add(event_model)
    selected_models.add(min(['auxiliar_historicos', 'auxiliar_hist_completo'], key=lambda k: runs[k]['score_validacion']))
    mapping = {}
    for evaluated in sorted(selected_models):
        n = runs[evaluated]['n_features']
        name = 'operacion_' + evaluated
        mapping[evaluated] = name
        final_data = make_windows(local_full, local_scaler, end=latest, n_features=n)
        final_summaries[name] = fit_run(name, final_data, None, local_scaler, n_features=n,
                                        pretrained=final_encoder if evaluated != 'local_sin_historicos' else None,
                                        epochs=runs[evaluated]['epoca_restaurada'], fixed=True,
                                        full_transfer='completo' in evaluated)
        del final_data
    dump(OUT / 'ajustes_operacion.json', final_adjustments)
    dump(OUT / 'operacion_mapeo.json', mapping)
    median_table = local_full['estacion_3'].loc[local_full['estacion_3'].index < latest, FEATURES]
    median_table.groupby(median_table.index.hour).median().to_csv(OUT / 'medianas_operacion.csv', encoding='utf-8-sig')
    dump(OUT / 'resumen_operacion.json', {'historicos': hist_final_summary['datos'],
                                        'locales': {k: v['datos'] for k, v in final_summaries.items()},
                                        'datos_locales_hasta_exclusivo': str(latest),
                                        'rad_local_unidad': 'lux', 'rad_historica_unidad': 'W/m²',
                                        'conversion_constante_lux_Wm2_aplicada': False,
                                        'modelo_final_evaluado_independientemente': False,
                                        'probabilidad_meteorologica_calibrada': False})
    print('ENTRENAMIENTO Y REAJUSTE FINAL COMPLETADOS.', flush=True)


if __name__ == '__main__':
    execute()
