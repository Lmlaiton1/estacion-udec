"""Pronóstico integrado: THL seleccionadas y lluvia LSTM mixta congelada."""
from pathlib import Path
import argparse
import json
import numpy as np
import pandas as pd
from tensorflow import keras
import datos_v12 as d
from modelo_v12 import mc_predict,physical_outputs
from pronostico_v12 import latest_input
from entrenar_pronostico import read,ridge_predict

ROOT=Path(__file__).resolve().parent
MODELS=ROOT/'modelos'


def emitir(dataset=d.LOCAL,destination=None,models=None,umbral_lluvia=.1,factor_sol=.65,origen=None):
    # models permite operar sobre una copia reentrenada; los umbrales del cielo se calibran con la retroalimentación.
    M=Path(models) if models else MODELS
    out=Path(destination) if destination else ROOT/'resultados'
    out.mkdir(parents=True,exist_ok=True)
    # El modelo de lluvia utiliza sus propios ajustes y escalas congelados.
    ra=read(ROOT/'lluvia/ajustes_estaciones_final.json')
    rainframes,_=d.load_local(saved=ra,cutoff=pd.Timestamp(ra['ajuste_hasta_exclusivo']),path=dataset)
    rx,origin=latest_input(rainframes,read(ROOT/'lluvia/transformacion_entradas.json'),4,origen)
    rainmodel=keras.models.load_model(ROOT/'lluvia/modelo.keras',compile=False)
    rn,_=physical_outputs(mc_predict(rainmodel,rx,passes=120),read(ROOT/'lluvia/transformacion.json'),1.,conditional=False)
    rain=rn[:,0,:,3].mean(0)
    keras.backend.clear_session()
    adj=read(M/'ajustes_operacion.json')
    frames,_=d.load_local(saved=adj,cutoff=pd.Timestamp(adj['ajuste_hasta_exclusivo']),path=dataset)
    scaler=read(M/'escala_operacion.json')
    x,_=latest_input(frames,scaler,3,origin)
    dates=pd.date_range(origin+pd.Timedelta(hours=1),periods=48,freq='h')
    night=(dates.hour<=4)|(dates.hour>=19)
    seasonal=np.tile(d.inverse(x[0],scaler),(2,1))
    seasonal[night,2]=0
    med=pd.read_csv(M/'medianas_operacion.csv',index_col=0)
    median=med.reindex(dates.hour)[d.FEATURES[:3]].to_numpy();median[night,2]=0
    candidates={'persistencia_24h':seasonal,'mediana_horaria':median}
    mapping=read(M/'mapeo.json')
    for name,spec in mapping.items():
        model=keras.models.load_model(M/spec['carpeta']/'modelo.keras',compile=False)
        history,_=latest_input(frames,scaler,spec['entradas'],origin)
        raw=mc_predict(model,history,passes=120)
        numeric,_=physical_outputs(raw,scaler,1.,conditional=False)
        point=numeric[:,0,:,:3].mean(0);point[night,2]=0
        candidates[name]=point
        candidates[name+'_mezcla50']=.5*point+.5*seasonal
        keras.backend.clear_session()
    block={'x':x,'meta':pd.DataFrame([{'inicio_objetivo':dates[0]}])}
    for alpha in [10,100]:
        name=f'ridge_{alpha}'
        candidates[name]=ridge_predict(read(M/(name+'_final.json')),block,scaler)[0]
    policy=read(M/'politica.json')
    point=np.stack([candidates[policy[var]][:,j] for j,var in enumerate(d.FEATURES[:3])],axis=-1)
    hourly=pd.DataFrame(point,index=dates,columns=d.FEATURES[:3])
    hourly.index.name='fecha_hora'
    hourly['lluv']=rain
    margins=pd.read_csv(M/'rangos_validacion.csv')
    for j,var in enumerate(d.FEATURES[:3]):
        width=np.array([margins[(margins.variable==var)&(margins.horizonte==('1-24h' if h<24 else '25-48h'))].margen_error_q90.iloc[0] for h in range(48)])
        lower=point[:,j]-width;upper=point[:,j]+width
        if var=='hum':lower=np.clip(lower,0,100);upper=np.clip(upper,0,100)
        if var=='rad':lower=np.maximum(lower,0);upper=np.maximum(upper,0);lower[night]=0;upper[night]=0
        hourly[var+'_limite_error_inferior']=lower
        hourly[var+'_limite_error_superior']=upper
    reference=pd.read_csv(M/'luz_referencia.csv',index_col=0)
    states=[]
    for t,v in hourly.iterrows():
        if v.lluv>=umbral_lluvia:state='Posible lluvia'
        elif 6<=t.hour<18:
            q=reference.rad.get(t.hour,np.nan)
            state='Señal de sol' if np.isfinite(q) and v.rad>=max(100,factor_sol*q) else 'Posible nubosidad'
        else:state='Cielo no determinado'
        states.append(state)
    hourly['condicion_orientativa']=states
    hourly['confianza_clase_calibrada']=np.nan
    hourly.to_csv(out/'pronostico_horario_48h.csv')
    day=origin.normalize()+pd.Timedelta(days=1)
    chosen=hourly[hourly.index.normalize()==day].copy()
    if len(chosen)!=24:raise ValueError('El horizonte no contiene el siguiente día completo')
    chosen['franja']=pd.cut(chosen.index.hour,[-1,5,11,17,23],labels=['Madrugada','Mañana','Tarde','Noche'])
    rows=[]
    for period,g in chosen.groupby('franja',observed=True):
        counts=g.condicion_orientativa.value_counts()
        state=str(counts.index[0]) if counts.iloc[0]>=3 else 'Condición variable'
        rows.append({'fecha':str(day.date()),'franja':str(period),'condicion_orientativa':state,
            'temperatura_media_c':float(g.tem.mean()),'temperatura_min_curva_c':float(g.tem.min()),
            'temperatura_max_curva_c':float(g.tem.max()),'humedad_media_pct':float(g.hum.mean()),
            'luz_media_lux':float(g.rad.mean()),'lluvia_era5_estimada_mm':float(g.lluv.sum()),
            'confianza_clase_calibrada':None,'horas':len(g)})
    daily=pd.DataFrame(rows);daily.to_csv(out/'pronostico_diario.csv',index=False)
    d.dump(out/'pronostico_metadatos.json',{'origen_intervalo_entrada':str(origin),
        'fin_exclusivo_entrada':str(origin+pd.Timedelta(hours=1)),'dia_pronosticado':str(day.date()),
        'primer_objetivo':str(dates[0]),'ultimo_objetivo':str(dates[-1]),
        'temperatura_humedad_luz':'estaciones locales; estación 3 de referencia',
        'lluvia':'LSTM mixta v16; entrada sensores, objetivo regional ERA5',
        'politica_thl':policy,'luz_unidad':'lux',
        'condicion':'regla descriptiva sobre lluvia y luz previstas; no clasificador validado de tres clases',
        'rangos_error':'percentil 90 de residuos absolutos en validación; cobertura de prueba en CSV, sin garantía de 90%',
        'confianza_clase_calibrada':False,'actualizado_con_datos_hasta':str(origin),
        'umbral_lluvia_mm_h':umbral_lluvia,'factor_sol':factor_sol})
    print('PRONÓSTICO',day.date(),'origen',origin,flush=True)
    return hourly,daily


if __name__=='__main__':
    parser=argparse.ArgumentParser(description='Usa el CSV horario con el esquema de datos/dataset_local_mejorado.csv, no CSV JSON crudo.')
    parser.add_argument('--dataset',type=Path,default=d.LOCAL)
    parser.add_argument('--salida',type=Path)
    parser.add_argument('--umbral-lluvia',type=float,default=.1)
    parser.add_argument('--factor-sol',type=float,default=.65)
    args=parser.parse_args();emitir(args.dataset,args.salida,umbral_lluvia=args.umbral_lluvia,factor_sol=args.factor_sol)
