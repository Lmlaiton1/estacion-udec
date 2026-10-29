"""Verifica separación temporal, selección, métricas, serialización y lluvia preservada."""
from pathlib import Path
import json
import hashlib
import numpy as np
import pandas as pd
import datos_v12 as d
from tensorflow import keras
from modelo_v12 import mc_predict,physical_outputs
from pronostico_v12 import latest_input

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'resultados'
MODELS=ROOT/'modelos'


def read(p):return json.loads(Path(p).read_text(encoding='utf-8'))


def main():
    checks=[]
    inputs=pd.read_csv(d.LOCAL)
    assert not inputs.duplicated(['estacion','fecha_hora']).any()
    checks.append('Sin duplicación estación/hora en la fuente local')
    summary=read(OUT/'resumen_entrenamiento.json')
    for name,spec in summary['corridas'].items():
        train=pd.read_csv(MODELS/name/'muestras_train.csv',parse_dates=['ultimo_objetivo_real','origen'])
        val=pd.read_csv(MODELS/name/'muestras_val.csv',parse_dates=['ultimo_objetivo_real','origen'])
        assert train.ultimo_objetivo_real.lt(d.TRAIN_END).all()
        assert val.ultimo_objetivo_real.lt(d.VAL_END).all()
        assert train.origen.lt(d.TRAIN_END).all()
    checks.append('Objetivos train/validación respetan cortes temporales')
    local,_=d.load_local()
    scale=d.fit_scaler(local,d.TRAIN_END,'lux')
    perturbed={k:g.copy() for k,g in local.items()}
    for g in perturbed.values():g.loc[g.index>=d.TRAIN_END,d.FEATURES]=np.nan
    same=d.fit_scaler(perturbed,d.TRAIN_END,'lux')
    assert scale==same
    checks.append('Datos futuros no cambian las estadísticas de entrenamiento')
    selection=pd.read_csv(OUT/'seleccion_validacion.csv')
    policy=read(MODELS/'politica.json')
    for var,name in policy.items():
        group=selection[selection.variable.eq(var)]
        assert np.isclose(group[group.modelo.eq(name)].mae.iloc[0],group.mae.min())
    checks.append('Política seleccionada únicamente con MAE de validación')
    detail=pd.read_csv(OUT/'errores_detallados.csv')
    metric=pd.read_csv(OUT/'metricas_regresion.csv')
    for r in metric[metric.horizonte.eq('1-48h')].itertuples(index=False):
        sub=detail[detail.particion.eq(r.particion)&detail.variable.eq(r.variable)]
        e=sub[r.modelo]-sub.observado
        assert np.isclose(np.abs(e).mean(),r.mae)
        assert np.isclose(np.sqrt(np.mean(e**2)),r.rmse)
        assert len(sub)==r.n_pares
    checks.append('MAE/RMSE y tamaños recalculados para todos los modelos')
    mapping=read(MODELS/'mapeo.json')
    sc=read(MODELS/'escala_operacion.json')
    adj=read(MODELS/'ajustes_operacion.json')
    full,_=d.load_local(saved=adj,cutoff=pd.Timestamp(adj['ajuste_hasta_exclusivo']))
    hourly=pd.read_csv(OUT/'pronostico_horario_48h.csv',parse_dates=['fecha_hora'])
    origin=hourly.fecha_hora.min()-pd.Timedelta(hours=1)
    for name,spec in mapping.items():
        model=keras.models.load_model(MODELS/spec['carpeta']/'modelo.keras',compile=False)
        x,_=latest_input(full,sc,spec['entradas'],origin)
        output=mc_predict(model,x,passes=3)
        num,_=physical_outputs(output,sc,1.,conditional=False)
        assert num.shape[:3]==(3,1,48) and np.isfinite(num).all()
        keras.backend.clear_session()
    checks.append('Modelos bayesianos guardados cargan y producen 48 horas finitas')
    ra=read(ROOT/'lluvia/ajustes_estaciones_final.json')
    rf,_=d.load_local(saved=ra,cutoff=pd.Timestamp(ra['ajuste_hasta_exclusivo']))
    rx,_=latest_input(rf,read(ROOT/'lluvia/transformacion_entradas.json'),4,origin)
    rainmodel=keras.models.load_model(ROOT/'lluvia/modelo.keras',compile=False)
    numeric,_=physical_outputs(mc_predict(rainmodel,rx,passes=120),read(ROOT/'lluvia/transformacion.json'),1.,conditional=False)
    assert np.allclose(numeric[:,0,:,3].mean(0),hourly.lluv,rtol=1e-6,atol=1e-7)
    checks.append('Lluvia idéntica a la inferencia del modelo mixto congelado')
    assert len(hourly)==48 and hourly.fecha_hora.diff().dropna().eq(pd.Timedelta(hours=1)).all()
    assert hourly.hum.between(0,100).all() and hourly.rad.ge(0).all() and hourly.lluv.ge(0).all()
    daily=pd.read_csv(OUT/'pronostico_diario.csv')
    assert len(daily)==4 and daily.horas.eq(6).all()
    for r in daily.itertuples(index=False):
        day=pd.Timestamp(r.fecha)
        lo={'Madrugada':0,'Mañana':6,'Tarde':12,'Noche':18}[r.franja]
        part=hourly[hourly.fecha_hora.ge(day+pd.Timedelta(hours=lo))&hourly.fecha_hora.lt(day+pd.Timedelta(hours=lo+6))]
        assert np.isclose(part.lluv.sum(),r.lluvia_era5_estimada_mm)
        assert np.isclose(part.tem.mean(),r.temperatura_media_c)
        assert np.isclose(part.hum.mean(),r.humedad_media_pct)
    checks.append('48 horas continuas y cuatro franjas agregadas correctamente')
    assert hourly.confianza_clase_calibrada.isna().all()
    assert read(OUT/'pronostico_metadatos.json')['luz_unidad']=='lux'
    checks.append('Unidades lux explícitas y porcentajes de cielo no inventados')
    result={'resultado':'correcto','cantidad':len(checks),'verificaciones':checks,
        'sha256_lluvia_preservada':hashlib.sha256((ROOT/'lluvia/modelo.keras').read_bytes()).hexdigest()}
    d.dump(OUT/'verificaciones.json',result)
    print(json.dumps(result,ensure_ascii=False,indent=2))


if __name__=='__main__':main()
