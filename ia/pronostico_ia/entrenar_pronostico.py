"""Entrena THL con estaciones, conserva lluvia mixta y selecciona sólo en validación."""
import os
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL','2')
from pathlib import Path
import json
import warnings
import numpy as np
import pandas as pd
import datos_v12 as d
import entrenar_v12 as trainer
from modelo_v12 import mc_predict,physical_outputs
from tensorflow import keras

ROOT=Path(__file__).resolve().parent
MODELS=ROOT/'modelos'
OUT=ROOT/'resultados'
trainer.OUT=MODELS
OUT.mkdir(exist_ok=True);MODELS.mkdir(exist_ok=True)


def read(p):return json.loads(Path(p).read_text(encoding='utf-8'))


def targets_thl(block):
    if block['x'].shape[-1]==4:
        block['y'][...,7]=0
        block['raw'][...,3]=np.nan
        block['event'][...,1]=0
    return block


def predict(name,block,passes=60):
    folder=MODELS/name
    scaler=read(folder/'transformacion.json')
    model=keras.models.load_model(folder/'modelo.keras',compile=False)
    raw=mc_predict(model,block['x'],passes=passes)
    values,_=physical_outputs(raw,scaler,1.,conditional=False)
    values=values[...,:3]
    for i,m in enumerate(block['meta'].itertuples(index=False)):
        hours=pd.date_range(m.inicio_objetivo,periods=48,freq='h').hour
        values[:,i,(hours<=4)|(hours>=19),2]=0
    keras.backend.clear_session()
    return values.mean(0)


def base(block,scaler,local,cutoff):
    past=d.inverse(block['x'],scaler)[...,:3]
    seasonal=np.tile(past,(1,2,1))
    train=local['estacion_3'].loc[local['estacion_3'].index<cutoff,d.FEATURES[:3]]
    med=train.groupby(train.index.hour).median()
    values=[]
    for m in block['meta'].itertuples(index=False):
        hours=pd.date_range(m.inicio_objetivo,periods=48,freq='h').hour
        values.append(med.reindex(hours).to_numpy())
    result={'persistencia_24h':seasonal,'mediana_horaria':np.asarray(values)}
    for value in result.values():
        for i,m in enumerate(block['meta'].itertuples(index=False)):
            hours=pd.date_range(m.inicio_objetivo,periods=48,freq='h').hour
            value[i,(hours<=4)|(hours>=19),2]=0
    return result


def ridge_design(block):
    # Sólo historia disponible, posición del horizonte y hora de salida.
    past=block['x'][...,:3]
    n=len(past)
    pattern=np.tile(past,(1,2,1))
    last=past[:,-1]
    hours=np.stack([pd.date_range(m.inicio_objetivo,periods=48,freq='h').hour.to_numpy()
                    for m in block['meta'].itertuples(index=False)])
    lead=np.arange(1,49)/48
    return np.concatenate([np.broadcast_to(past.reshape(n,1,-1),(n,48,72)),pattern,
        last[:,None,:]*np.exp(-np.arange(1,49)[None,:,None]/6),
        np.sin(2*np.pi*hours/24)[...,None],np.cos(2*np.pi*hours/24)[...,None],
        np.broadcast_to(lead[None,:,None],(n,48,1))],axis=-1)


def ridge_fit(block,scaler,alpha):
    X=ridge_design(block).reshape(-1,81)
    raw=d.transform(block['raw'],scaler)[...,:3].reshape(-1,3)
    specs=[]
    for j in range(3):
        ok=np.isfinite(raw[:,j]);a=X[ok];y=raw[ok,j]
        mean=a.mean(0);std=np.maximum(a.std(0),.05)
        z=np.c_[np.ones(len(a)),(a-mean)/std]
        penalty=np.eye(z.shape[1])*alpha;penalty[0,0]=0
        beta=np.linalg.solve(z.T@z+penalty,z.T@y)
        specs.append({'mean':mean.tolist(),'std':std.tolist(),'beta':beta.tolist(),'n':int(ok.sum())})
    return {'alpha':alpha,'variables':specs}


def ridge_predict(spec,block,scaler):
    x=ridge_design(block)
    result=np.zeros(x.shape[:2]+(3,))
    for j,s in enumerate(spec['variables']):
        z=(x-np.array(s['mean']))/np.array(s['std'])
        b=np.array(s['beta']);result[...,j]=b[0]+z@b[1:]
    result=d.inverse(result,scaler)
    for i,m in enumerate(block['meta'].itertuples(index=False)):
        hours=pd.date_range(m.inicio_objetivo,periods=48,freq='h').hour
        result[i,(hours<=4)|(hours>=19),2]=0
    return result


def regression(a,p):
    ok=np.isfinite(a)&np.isfinite(p);a,p=a[ok],p[ok]
    e=p-a;ss=np.sum((a-a.mean())**2)
    return {'mae':float(np.abs(e).mean()),'rmse':float(np.sqrt(np.mean(e**2))),
        'sesgo':float(e.mean()),'r2':float(1-np.sum(e**2)/ss) if ss>0 else None,'n_pares':len(a)}


def main():
    local,adj=d.load_local()
    scaler=d.fit_scaler(local,d.TRAIN_END,'lux')
    d.dump(MODELS/'ajustes_evaluacion.json',adj)
    blocks={}
    for n in [3,4]:
        blocks[n]={k:targets_thl(d.make_windows(local,scaler,start,end,n_features=n,
            stride=1 if k=='train' else 6,only_source=None if k=='train' else 'estacion_3'))
            for k,start,end in [('train',None,d.TRAIN_END),('val',d.TRAIN_END,d.VAL_END),
                               ('test',d.VAL_END,pd.Timestamp('2026-10-16'))]}
    specs=[('lstm_local',4,None,False),('lstm_historicos',4,ROOT/'preentrenado/historicos_evaluacion.keras',True),
           ('lstm_auxiliar',3,ROOT/'preentrenado/historicos_evaluacion.keras',True)]
    runs={}
    for name,n,pretrained,full in specs:
        with warnings.catch_warnings():
            warnings.simplefilter('ignore',RuntimeWarning)
            runs[name]=trainer.fit_run(name,blocks[n]['train'],blocks[n]['val'],scaler,n_features=n,
                pretrained=pretrained,full_transfer=full,pretrain=True,epochs=160)
    predictions={k:{} for k in ['val','test']}
    for split in predictions:
        common=blocks[4][split]['meta'].origen.tolist()
        for name,n,_,_ in specs:
            b=blocks[n][split] if n==4 else trainer.align(blocks[n][split],common)
            predictions[split][name]=predict(name,b)
        predictions[split].update(base(blocks[4][split],scaler,local,d.TRAIN_END))
    ridges={}
    for alpha in [10.,100.]:
        name=f'ridge_{int(alpha)}'
        ridges[name]=ridge_fit(blocks[3]['train'],scaler,alpha)
        d.dump(MODELS/(name+'_evaluacion.json'),ridges[name])
        for split in predictions:
            b=trainer.align(blocks[3][split],blocks[4][split]['meta'].origen.tolist())
            predictions[split][name]=ridge_predict(ridges[name],b,scaler)
    # Mezclas definidas a priori; la prueba no decide modelos ni pesos.
    for split,part in predictions.items():
        for name,_,_,_ in specs:
            part[name+'_mezcla50']=.5*part[name]+.5*part['persistencia_24h']
    valrows=[];policy={}
    for j,var in enumerate(d.FEATURES[:3]):
        for name,p in predictions['val'].items():
            valrows.append({'variable':var,'modelo':name}|regression(blocks[4]['val']['raw'][...,j],p[...,j]))
        policy[var]=min([r for r in valrows if r['variable']==var],key=lambda r:r['mae'])['modelo']
    pd.DataFrame(valrows).to_csv(OUT/'seleccion_validacion.csv',index=False)
    d.dump(MODELS/'politica.json',policy)
    for split in predictions:
        predictions[split]['sistema_seleccionado']=np.stack([predictions[split][policy[var]][...,j]
                                                  for j,var in enumerate(d.FEATURES[:3])],axis=-1)
    details=[];metrics=[]
    for split in predictions:
        b=blocks[4][split]
        for i,m in enumerate(b['meta'].itertuples(index=False)):
            for h,t in enumerate(pd.date_range(m.inicio_objetivo,periods=48,freq='h')):
                for j,var in enumerate(d.FEATURES[:3]):
                    actual=b['raw'][i,h,j]
                    if np.isfinite(actual):
                        details.append({'particion':split,'origen':m.origen,'fecha_objetivo':t,
                            'horizonte':h+1,'variable':var,'observado':float(actual)}|
                            {name:float(p[i,h,j]) for name,p in predictions[split].items()})
    detail=pd.DataFrame(details)
    detail.to_csv(OUT/'errores_detallados.csv',index=False)
    for split in predictions:
        for var in d.FEATURES[:3]:
            for label,lo,hi in [('1-48h',1,48),('1-24h',1,24),('25-48h',25,48)]:
                part=detail[detail.particion.eq(split)&detail.variable.eq(var)&detail.horizonte.between(lo,hi)]
                for name in predictions[split]:
                    metrics.append({'particion':split,'variable':var,'horizonte':label,'modelo':name,
                        'horas_distintas':int(part.fecha_objetivo.nunique())}|regression(part.observado.to_numpy(),part[name].to_numpy()))
    pd.DataFrame(metrics).to_csv(OUT/'metricas_regresion.csv',index=False)
    daylight=detail[(detail.fecha_objetivo.dt.hour>=6)&(detail.fecha_objetivo.dt.hour<18)&detail.variable.eq('rad')]
    pd.DataFrame([{'particion':split,'variable':'rad','modelo':name}|regression(
        daylight[daylight.particion.eq(split)].observado.to_numpy(),daylight[daylight.particion.eq(split)][name].to_numpy())
        for split in predictions for name in predictions[split]]).to_csv(OUT/'metricas_luz_diurna.csv',index=False)
    # Rangos de error absoluto a partir de validación, agrupados por hora objetivo
    # y bloque de horizonte para limitar la duplicación. Cobertura se informa.
    ranges=[];coverage=[]
    for var in d.FEATURES[:3]:
        for band,lo,hi in [('1-24h',1,24),('25-48h',25,48)]:
            va=detail[detail.particion.eq('val')&detail.variable.eq(var)&detail.horizonte.between(lo,hi)].copy()
            va['abserr']=(va.sistema_seleccionado-va.observado).abs()
            residual=va.groupby('fecha_objetivo').abserr.max()
            q=float(residual.quantile(.9,interpolation='higher'))
            ranges.append({'variable':var,'horizonte':band,'margen_error_q90':q,'horas_validacion':len(residual)})
            te=detail[detail.particion.eq('test')&detail.variable.eq(var)&detail.horizonte.between(lo,hi)]
            coverage.append({'variable':var,'horizonte':band,'cobertura_prueba':float(((te.sistema_seleccionado-te.observado).abs()<=q).mean()),
                             'n_pares':len(te),'margen':q})
    pd.DataFrame(ranges).to_csv(MODELS/'rangos_validacion.csv',index=False)
    pd.DataFrame(coverage).to_csv(OUT/'cobertura_rangos.csv',index=False)
    latest=max(g.index.max() for g in local.values())+pd.Timedelta(hours=1)
    full,adjfull=d.load_local(latest)
    finalscale=d.fit_scaler(full,latest,'lux')
    d.dump(MODELS/'ajustes_operacion.json',adjfull)
    d.dump(MODELS/'escala_operacion.json',finalscale)
    # Reajuste de cada LSTM seleccionada; también se conserva la mejor LSTM
    # según validación aunque una referencia simple gane en alguna variable.
    required={v.replace('_mezcla50','') for v in policy.values() if v.startswith('lstm_')}
    bestnet=min(runs,key=lambda k:runs[k]['score_validacion']);required.add(bestnet)
    mapping={}
    for name,n,_,fulltransfer in specs:
        if name not in required:continue
        b=targets_thl(d.make_windows(full,finalscale,end=latest,n_features=n))
        with warnings.catch_warnings():
            warnings.simplefilter('ignore',RuntimeWarning)
            trainer.fit_run('final_'+name,b,None,finalscale,n_features=n,
                pretrained=ROOT/'preentrenado/historicos_final.keras' if name!='lstm_local' else None,
                full_transfer=fulltransfer,pretrain=True,fixed=True,epochs=runs[name]['epoca_restaurada'])
        mapping[name]={'carpeta':'final_'+name,'entradas':n,'ventanas':d.catalog(b)}
    for name,spec in ridges.items():
        b=targets_thl(d.make_windows(full,finalscale,end=latest,n_features=3))
        d.dump(MODELS/(name+'_final.json'),ridge_fit(b,finalscale,spec['alpha']))
    train=full['estacion_3'].loc[full['estacion_3'].index<latest,d.FEATURES[:3]]
    train.groupby(train.index.hour).median().to_csv(MODELS/'medianas_operacion.csv')
    train.groupby(train.index.hour).quantile(.9).to_csv(MODELS/'luz_referencia.csv')
    d.dump(MODELS/'mapeo.json',mapping)
    d.dump(OUT/'resumen_entrenamiento.json',{'arquitectura':'LSTM variacional 8 unidades, decodificador bayesiano 16 unidades, horizonte 48h',
        'historicos':['NASA POWER','Open-Meteo','ERA5-Land'],
        'adaptacion_historica':'pesos históricos incluidos, luego ajuste local; radiación normalizada en dominios separados',
        'corridas':runs,'politica':policy,'modelos_finales':mapping,'lluvia':'pesos mixtos v16 preservados',
        'rad_unidad_local':'lux','clases_cielo_validadas':False,
        'particiones':{str(n):{k:d.catalog(b) for k,b in part.items()} for n,part in blocks.items()},
        'prueba_temporal':'objetivos 05/10–15/10 donde existen mediciones; nunca se usan para selección',
        'modelo_final_tiene_evaluacion_con_dias_nuevos':False})
    print('POLÍTICA',policy,flush=True)
    print(pd.DataFrame(metrics).query("particion=='test' and horizonte=='1-48h' and modelo=='sistema_seleccionado'").to_string(index=False),flush=True)
    from predecir import emitir
    emitir()
    from informe import generar
    generar()


if __name__=='__main__':main()
