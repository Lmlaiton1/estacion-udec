"""Fuentes separadas, ventanas por estación y máscaras sólo de objetivos."""
from pathlib import Path
import json
import hashlib
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent
LOCAL = ROOT / 'datos/dataset_local_mejorado.csv'
HISTORICAL = ROOT / 'datos/historicos_externos_Wm2.csv'
FEATURES = ['tem', 'hum', 'rad', 'lluv']
TRAIN_END = pd.Timestamp('2026-09-30')
VAL_END = pd.Timestamp('2026-10-05')
TEST_END = pd.Timestamp('2026-10-10')
L, H = 24, 48


def dump(path, obj):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(obj, ensure_ascii=False, indent=2, allow_nan=False), encoding='utf-8')


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load_historical():
    d = pd.read_csv(HISTORICAL, parse_dates=['fecha_hora']).rename(columns={
        'temperatura_c': 'tem', 'humedad_pct': 'hum', 'radiacion_wm2': 'rad', 'precipitacion_mm': 'lluv'})
    for col in FEATURES:
        d[col] = pd.to_numeric(d[col], errors='coerce')
    d.loc[~d.tem.between(-10, 50), 'tem'] = np.nan
    d.loc[~d.hum.between(0, 100), 'hum'] = np.nan
    for col in ['rad', 'lluv']:
        d.loc[d[col].lt(0), col] = np.nan
    frames = {}
    for source, g in d.groupby('fuente'):
        g = g.set_index('fecha_hora').sort_index()[FEATURES]
        if g.index.duplicated().any():
            raise ValueError('Fecha histórica duplicada dentro de una fuente')
        g = g.reindex(pd.date_range(g.index.min(), g.index.max(), freq='h'))
        g.index.name = 'fecha_hora'
        for col in FEATURES:
            g['in_' + col] = g[col]
        frames[source] = g
    return frames


def load_local(cutoff=TRAIN_END, saved=None, path=LOCAL):
    d = pd.read_csv(path, parse_dates=['fecha_hora'])
    frames = {}
    for station in [1, 2, 3]:
        part = d[d.estacion.eq(station)].set_index('fecha_hora').sort_index()
        if part.empty:
            continue
        index = pd.date_range(part.index.min(), part.index.max(), freq='h')
        part = part.reindex(index)
        g = part[FEATURES].copy()
        coverage = part.soporte_temporal_minutos.ge(45) & part.hora_parcial.eq(False)
        for col in ['tem', 'hum', 'rad']:
            valid = coverage & part['n_' + col + '_validas'].ge(10)
            if col == 'rad':
                valid &= part.radiacion_constante.eq(False)
                valid &= ~(pd.Series(index.hour, index=index).between(8, 16) & part.rad.le(10))
            g.loc[~valid, col] = np.nan
        # La precipitación conservada es parcial: se exige cobertura de la hora,
        # pero nunca se deduce ausencia de lluvia de un cero publicado.
        g.loc[~coverage, 'lluv'] = np.nan
        frames[f'estacion_{station}'] = g
    reference = frames['estacion_3']
    adjustments = {}
    for source, g in frames.items():
        pars = {}
        for col in FEATURES:
            if source == 'estacion_3' or col == 'lluv':
                pars[col] = {'tipo': 'identidad', 'valor': 0., 'n_pares': 0}
                continue
            if saved is not None:
                p = saved['estaciones'][source][col]
            else:
                pair = pd.concat([reference[col].rename('r'), g[col].rename('o')], axis=1).dropna()
                pair = pair.loc[pair.index < cutoff]
                if col == 'rad':
                    pair = pair[(pair.r > 1000) & (pair.o > 1000)]
                    value = float(np.clip((pair.r / pair.o).median(), .25, 4)) if len(pair) else 1.
                    kind = 'razon_lux'
                else:
                    value = float((pair.r - pair.o).median()) if len(pair) else 0.
                    kind = 'desplazamiento'
                p = {'tipo': kind, 'valor': value, 'n_pares': len(pair)}
            pars[col] = dict(p)
            g[col] = g[col] * p['valor'] if p['tipo'] == 'razon_lux' else g[col] + p['valor']
            if col == 'hum':
                g[col] = g[col].clip(0, 100)
        adjustments[source] = pars
    # Se preservan las tres trayectorias. Sólo una luz ausente puede tomar un
    # respaldo MEDIDO contemporáneo; las otras variables no se fabrican.
    for source, g in frames.items():
        for col in FEATURES:
            g['in_' + col] = g[col]
        g['origen_luz_entrada'] = np.where(g.rad.notna(), source, '')
        for backup in ['estacion_3', 'estacion_1', 'estacion_2']:
            if backup == source or backup not in frames:
                continue
            candidate = frames[backup]['rad'].reindex(g.index)
            use = g.in_rad.isna() & candidate.notna() & g.tem.notna() & g.hum.notna()
            g.loc[use, 'in_rad'] = candidate.loc[use]
            g.loc[use, 'origen_luz_entrada'] = backup
        g.index.name = 'fecha_hora'
    return frames, {'ajuste_hasta_exclusivo': str(cutoff), 'estaciones': adjustments,
                    'rad_local': 'lux', 'lluv_local': 'mm publicados parciales; no lluvia total real'}


def nonlinear(values):
    y = np.asarray(values, dtype=float).copy()
    y[..., 2:] = np.sqrt(np.maximum(y[..., 2:], 0))
    return y


def fit_scaler(frames, cutoff, unit):
    pieces = [g.loc[g.index < cutoff, FEATURES].to_numpy(float) for g in frames.values()]
    raw = np.concatenate(pieces)
    z = nonlinear(raw)
    return {'features': FEATURES, 'mean': np.nanmean(z, axis=0).tolist(),
            'std': np.maximum(np.nanstd(z, axis=0), [1., 1., 1., .1]).tolist(),
            'physical_std': np.maximum(np.nanstd(raw, axis=0), [1., 1., 1., .1]).tolist(),
            'rad_unidad': unit, 'ajuste_hasta_exclusivo': str(cutoff),
            'transformacion': ['lineal', 'lineal', 'raiz', 'raiz']}


def transform(values, scaler):
    k = np.shape(values)[-1]
    return ((nonlinear(values) - np.asarray(scaler['mean'])[:k]) / np.asarray(scaler['std'])[:k]).astype('float32')


def inverse(values, scaler):
    k = np.shape(values)[-1]
    y = np.asarray(values, dtype=float) * np.asarray(scaler['std'])[:k] + np.asarray(scaler['mean'])[:k]
    y[..., 2:] = np.maximum(y[..., 2:], 0) ** 2
    y[..., 2:] = np.where(y[..., 2:] < 1e-12, 0., y[..., 2:])
    y[..., 0] = np.clip(y[..., 0], -10, 50)
    y[..., 1] = np.clip(y[..., 1], 0, 100)
    return y


def make_windows(frames, scaler, start=None, end=None, n_features=4, stride=1,
                 conditional_rain=True, only_source=None, require=12):
    result = {'x': [], 'y': [], 'event': [], 'raw': [], 'meta': []}
    for source, frame in frames.items():
        if only_source and source != only_source:
            continue
        values = frame[FEATURES[:n_features]].to_numpy(float)
        inputs = frame[['in_' + col for col in FEATURES[:n_features]]].to_numpy(float)
        z = transform(inputs, scaler)
        times = frame.index
        for last in range(L - 1, len(frame) - require):
            origin = times[last]
            first = origin + pd.Timedelta(hours=1)
            if stride > 1 and origin.hour % stride != stride - 1:
                continue
            if start is not None and first < start:
                continue
            if end is not None and first + pd.Timedelta(hours=require - 1) >= end:
                continue
            history = z[last - L + 1:last + 1]
            if not np.isfinite(history).all():
                continue
            target = np.full((H, n_features), np.nan)
            available = min(H, len(frame) - last - 1)
            target[:available] = values[last + 1:last + 1 + available]
            dates = pd.date_range(first, periods=H, freq='h')
            if end is not None:
                target[dates >= end] = np.nan
            mask = np.isfinite(target)
            if mask[:, :2].all(axis=1).sum() < require:
                continue
            event = np.zeros((H, 2), dtype='float32')
            if n_features == 4:
                event[:, 0] = np.nan_to_num(target[:, 3] > 0).astype(float)
                event[:, 1] = np.isfinite(target[:, 3]).astype(float)
                if conditional_rain:
                    mask[:, 3] &= target[:, 3] > 0
            y = np.concatenate([np.nan_to_num(transform(target, scaler)), mask.astype(float)], axis=-1)
            result['x'].append(history)
            result['y'].append(y)
            result['event'].append(event)
            result['raw'].append(target)
            result['meta'].append({'fuente': source, 'origen': origin,
                                   'inicio_objetivo': first, 'fin_teorico_48h': dates[-1],
                                   'ultimo_objetivo_real': dates[np.isfinite(target).any(axis=1)][-1],
                                   'horas_objetivo': int(mask[:, :2].all(axis=1).sum())})
    if not result['x']:
        raise ValueError('Sin ventanas válidas para esta configuración')
    for key in ['x', 'y', 'event', 'raw']:
        result[key] = np.asarray(result[key], dtype='float32')
    result['meta'] = pd.DataFrame(result['meta'])
    return result


def catalog(block):
    input_hours, target_hours = set(), set()
    for i, row in enumerate(block['meta'].itertuples(index=False)):
        input_hours.update(pd.date_range(row.origen - pd.Timedelta(hours=23), periods=24, freq='h'))
        dates = pd.date_range(row.inicio_objetivo, periods=48, freq='h')
        target_hours.update(dates[np.isfinite(block['raw'][i]).any(axis=1)])
    return {'ventanas': len(block['x']), 'por_fuente': block['meta'].fuente.value_counts().to_dict(),
            'horas_entrada_unicas_calendario': len(input_hours),
            'horas_objetivo_unicas_calendario': len(target_hours),
            'celdas_objetivo_con_repeticiones': int(block['y'][..., block['x'].shape[-1]:].sum()),
            'inicio_origen': str(block['meta'].origen.min()), 'fin_origen': str(block['meta'].origen.max())}


def export_frames(frames, destination):
    """Rejilla horaria con NaN en huecos y procedencia de respaldos de luz."""
    result = pd.concat([frame.assign(fuente=source) for source, frame in frames.items()])
    result.index.name = 'fecha_hora'
    result.reset_index().to_csv(destination, index=False, encoding='utf-8-sig')


if __name__ == '__main__':
    frames, pars = load_local()
    scaler = fit_scaler(frames, TRAIN_END, 'lux')
    for n in [4, 3]:
        for name, start, end, stride, source in [('train', None, TRAIN_END, 1, None),
                                               ('val', TRAIN_END, VAL_END, 6, 'estacion_3'),
                                               ('test', VAL_END, TEST_END, 6, 'estacion_3')]:
            block = make_windows(frames, scaler, start, end, n_features=n, stride=stride, only_source=source)
            print(n, name, json.dumps(catalog(block), ensure_ascii=False), flush=True)
