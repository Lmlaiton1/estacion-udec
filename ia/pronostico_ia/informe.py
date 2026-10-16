"""Informe, gráficos científicos y tarjeta diaria a partir de resultados guardados."""
from pathlib import Path
import json
import html
import numpy as np
import pandas as pd
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'resultados'
LABELS={'tem':'Temperatura (°C)','hum':'Humedad (%)','rad':'Luz (lux)','lluv':'Lluvia ERA5 (mm/h)'}
NAMES={'lstm_local':'LSTM local','lstm_historicos':'LSTM con históricos','lstm_auxiliar':'LSTM auxiliar',
       'persistencia_24h':'Últimas 24 h','mediana_horaria':'Mediana por hora','sistema_seleccionado':'Sistema seleccionado',
       'ridge_10':'Regresión ridge (10)','ridge_100':'Regresión ridge (100)'}


def fmt(v,n=1):return f'{v:,.{n}f}'.replace(',','_').replace('.',',').replace('_','.')


def generar():
    plt.rcParams.update({'font.family':'DejaVu Sans','font.size':10,'axes.spines.top':False,'axes.spines.right':False})
    meta=json.loads((OUT/'resumen_entrenamiento.json').read_text(encoding='utf-8'))
    forecastmeta=json.loads((OUT/'pronostico_metadatos.json').read_text(encoding='utf-8'))
    detail=pd.read_csv(OUT/'errores_detallados.csv',parse_dates=['fecha_objetivo','origen'])
    metrics=pd.read_csv(OUT/'metricas_regresion.csv')
    test=detail[detail.particion.eq('test')]
    day=pd.read_csv(OUT/'pronostico_diario.csv')
    hourly=pd.read_csv(OUT/'pronostico_horario_48h.csv',parse_dates=['fecha_hora'])
    # Tarjeta con valores reales de salida, sin porcentajes de clase inventados.
    fig=plt.figure(figsize=(18,8),facecolor='white')
    fig.text(.025,.94,'Resultados del pronóstico diario',size=25,weight='bold',color='#20334b')
    fig.text(.025,.89,f"{forecastmeta['dia_pronosticado']} · Facatativá · Extensión Universidad de Cundinamarca",size=14,color='#40546c')
    fig.text(.025,.85,'Referencia: estación 3 · Temperatura, humedad y luz locales · Lluvia: modelo mixto con objetivo ERA5',size=11,color='#61758b')
    times=['00:00–06:00','06:00–12:00','12:00–18:00','18:00–00:00']
    for i,r in enumerate(day.itertuples(index=False)):
        x=.025+i*.245
        box=FancyBboxPatch((x,.16),.225,.62,boxstyle='round,pad=0.008,rounding_size=0.018',
            transform=fig.transFigure,facecolor='#f1f6fb',edgecolor='#d6e2ee')
        fig.add_artist(box)
        fig.text(x+.015,.735,r.franja.upper(),size=15,weight='bold',color='#20334b')
        fig.text(x+.015,.695,times[i],size=12,color='#61758b')
        state=r.condicion_orientativa
        if state=='Cielo no determinado':state='Cielo no\ndeterminado'
        fig.text(x+.015,.60,state,size=17,weight='bold',color='#405b75',linespacing=1.2)
        entries=[('Temperatura media',fmt(r.temperatura_media_c)+' °C'),
                 ('Rango de la curva horaria',fmt(r.temperatura_min_curva_c)+'–'+fmt(r.temperatura_max_curva_c)+' °C'),
                 ('Humedad media',fmt(r.humedad_media_pct)+' %'),
                 ('Lluvia acumulada estimada',fmt(r.lluvia_era5_estimada_mm,2)+' mm'),
                 ('Luz media',fmt(r.luz_media_lux,0)+' lux')]
        for k,(label,value) in enumerate(entries):
            y=.51-k*.063
            fig.text(x+.015,y,label,size=10,color='#61758b')
            fig.text(x+.208,y-.025,value,ha='right',size=13,weight='bold',color='#20334b')
    fig.text(.025,.09,'Condición orientativa derivada de lluvia y luz previstas. Sin probabilidad de cielo calibrada.',size=10,color='#61758b')
    fig.text(.025,.055,f"Entrada hasta el intervalo {forecastmeta['origen_intervalo_entrada']}. Rangos térmicos: mínimo y máximo de la curva, no intervalos de confianza.",size=10,color='#61758b')
    fig.savefig(OUT/'pronostico_diario.png',dpi=150);plt.close(fig)
    # Pronóstico de variables y rangos basados en errores de validación.
    fig,axs=plt.subplots(4,1,figsize=(12,11),sharex=True)
    for ax,var in zip(axs,['tem','hum','rad','lluv']):
        ax.plot(hourly.fecha_hora,hourly[var],color='#27759b',label='Pronóstico')
        if var!='lluv':
            ax.fill_between(hourly.fecha_hora,hourly[var+'_limite_error_inferior'],hourly[var+'_limite_error_superior'],alpha=.18,color='#27759b',label='Margen de error de validación')
        ax.set_ylabel(LABELS[var]);ax.grid(alpha=.2);ax.legend(loc='upper left')
    axs[0].set_title('Resultados: siguientes 48 horas')
    fig.autofmt_xdate();fig.tight_layout();fig.savefig(OUT/'pronostico_variables.png',dpi=150);plt.close(fig)
    # Curvas para las tres LSTM; se marca la época restaurada.
    fig,axs=plt.subplots(1,3,figsize=(15,4.5))
    for ax,(name,spec) in zip(axs,meta['corridas'].items()):
        h=pd.read_csv(ROOT/'modelos'/name/'historial.csv')
        ax.plot(h.epoch+1,h.loss,label='Entrenamiento');ax.plot(h.epoch+1,h.val_loss,label='Validación')
        ax.axvline(spec['epoca_restaurada'],ls=':',color='#545454',label='Época restaurada')
        ax.set(title=NAMES.get(name,name),xlabel='Época',ylabel='Pérdida THL + KL');ax.legend(fontsize=8)
    fig.tight_layout();fig.savefig(OUT/'curvas_entrenamiento.png',dpi=150);plt.close(fig)
    # Serie de prueba y dispersión. Huecos conservados al reindexar.
    fig,axs=plt.subplots(3,2,figsize=(13,10))
    for j,var in enumerate(['tem','hum','rad']):
        g=test[test.variable.eq(var)]
        series=g.groupby('fecha_objetivo')[['observado','sistema_seleccionado']].mean()
        series=series.reindex(pd.date_range(series.index.min(),series.index.max(),freq='h'))
        axs[j,0].plot(series.index,series.observado,label='Medido estación 3',lw=1.3)
        axs[j,0].plot(series.index,series.sistema_seleccionado,label='Predicho (media de orígenes)',lw=1.2)
        axs[j,0].set_ylabel(LABELS[var]);axs[j,0].legend(fontsize=8)
        axs[j,1].scatter(g.observado,g.sistema_seleccionado,s=9,alpha=.25)
        lo=min(g.observado.min(),g.sistema_seleccionado.min());hi=max(g.observado.max(),g.sistema_seleccionado.max())
        axs[j,1].plot([lo,hi],[lo,hi],'--',color='#555555');axs[j,1].set(xlabel='Observado',ylabel='Predicho',title=LABELS[var])
    fig.autofmt_xdate();fig.tight_layout();fig.savefig(OUT/'errores_prueba.png',dpi=150);plt.close(fig)
    fig,axs=plt.subplots(1,3,figsize=(14,4))
    for ax,var in zip(axs,['tem','hum','rad']):
        for split,label in [('val','Validación'),('test','Prueba')]:
            g=detail[detail.variable.eq(var)&detail.particion.eq(split)].copy()
            g['error']=(g.sistema_seleccionado-g.observado).abs()
            line=g.groupby('horizonte').error.mean()
            ax.plot(line.index,line,label=label)
        ax.set(title=LABELS[var],xlabel='Horas de anticipación',ylabel='MAE');ax.legend()
    fig.tight_layout();fig.savefig(OUT/'errores_por_horizonte.png',dpi=150);plt.close(fig)
    fig,axs=plt.subplots(1,3,figsize=(14,4))
    for ax,var in zip(axs,['tem','hum','rad']):
        g=test[test.variable.eq(var)];e=g.sistema_seleccionado-g.observado
        ax.hist(e,bins=25,color='#438ba5',alpha=.8);ax.axvline(0,color='#555555',ls='--')
        ax.set(title=LABELS[var],xlabel='Predicción − observación',ylabel='Comparaciones')
    fig.tight_layout();fig.savefig(OUT/'distribucion_errores.png',dpi=150);plt.close(fig)
    # Comparación de MAE, incluyendo todas las alternativas.
    fig,axs=plt.subplots(1,3,figsize=(16,6))
    for ax,var in zip(axs,['tem','hum','rad']):
        g=metrics[metrics.particion.eq('test')&metrics.variable.eq(var)&metrics.horizonte.eq('1-48h')].sort_values('mae')
        ax.barh(g.modelo,g.mae,color=['#228460' if n=='sistema_seleccionado' else '#82adc0' for n in g.modelo])
        ax.invert_yaxis();ax.set(title=LABELS[var],xlabel='MAE');ax.tick_params(axis='y',labelsize=8)
    fig.tight_layout();fig.savefig(OUT/'comparacion_modelos.png',dpi=150);plt.close(fig)
    selected=metrics[metrics.particion.eq('test')&metrics.horizonte.eq('1-48h')&metrics.modelo.eq('sistema_seleccionado')]
    rain=json.loads((ROOT/'lluvia/metricas_clasificacion_prueba.json').read_text(encoding='utf-8'))
    lines=['# Resultados del pronóstico IA','',
        'Ubicación: extensión de la Universidad de Cundinamarca en Facatativá. Referencia: estación 3.',
        '', '## Métricas de prueba: temperatura, humedad y luz','',
        '| Variable | MAE | RMSE | R² | Horas distintas |', '|---|---:|---:|---:|---:|']
    for r in selected.itertuples(index=False):
        lines.append(f'| {LABELS[r.variable]} | {r.mae:.3f} | {r.rmse:.3f} | {r.r2:.3f} | {r.horas_distintas} |')
    lines+=['','Las métricas usan pares origen/hora objetivo. Las horas se repiten desde distintos orígenes; no son observaciones independientes.',
        'MAE mide error absoluto medio; RMSE da mayor peso a errores grandes; R² negativo indica peor desempeño que la media del conjunto evaluado.',
        '', '## Selección y validación','']
    for var,name in meta['politica'].items():lines.append(f'- {LABELS[var]}: {name}.')
    lines+=['','La selección de modelo y mezcla usa únicamente MAE de validación, no los errores de prueba. Los modelos finales se reajustan con todos los datos válidos; sus métricas con días posteriores aún no están disponibles.',
        '', '## Entrenamiento y arquitectura','',
        'Entradas: 24 horas de temperatura, humedad, luz y lluvia publicada; el modelo auxiliar usa las primeras tres para aprovechar intervalos sin lluvia registrada. Salidas: 48 horas de temperatura, humedad y luz.',
        'LSTM bayesiana variacional de 8 unidades, decodificador bayesiano de 16 unidades, posterior gaussiano de campo medio y regularización KL. La regresión usa ridge; las referencias usan repetición de 24 h y mediana por hora.',
        'El horizonte incluye su posición y la hora de salida calculada desde el índice temporal. No se consulta una variable futura medida ni se añaden sensores.',
        'Inicialización histórica con pesos NASA POWER, Open-Meteo y adaptación ERA5-Land; las escalas W/m² históricas y lux locales son independientes. Los pesos iniciales y sus metadatos se incluyen en preentrenado.',
        '', '| Modelo | Ventanas train | Ventanas validación | Parámetros | Épocas | Época restaurada |',
        '|---|---:|---:|---:|---:|---:|']
    for name,s in meta['corridas'].items():lines.append(f"| {name} | {s['datos']['ventanas']} | {s['validacion']['ventanas']} | {s['parametros']} | {s['epocas']} | {s['epoca_restaurada']} |")
    lines+=['','Train: objetivos anteriores al 30/09/2026. Validación: 30/09–04/10. Prueba: 05/10–15/10, donde hay mediciones válidas. Ajustes de estaciones, normalizaciones y medianas de evaluación se calculan sólo con train.',
        'Se excluyen horas parciales, cobertura insuficiente y luz constante o cero anómalo diurno. Sólo se admite respaldo de luz medido simultáneamente por otra estación, con procedencia. No se interpolan huecos temporales.',
        'La evaluación es retrospectiva; estos registros ya fueron inspeccionados en trabajos previos. No equivale a una evaluación prospectiva nueva.',
        '', '## Lluvia preservada','',
        'Se mantienen los pesos y transformaciones de la LSTM mixta v16. Usa sensores recientes como entrada y lluvia regional ERA5 como objetivo. La lluvia parcial de estaciones no se transforma en total.',
        f"Prueba binaria ERA5 ≥0,1 mm/h: accuracy {rain['accuracy']:.2%}; precisión lluvia {rain['por_categoria'][1]['precision']:.2%}; recall {rain['por_categoria'][1]['recall']:.2%}; F1 {rain['por_categoria'][1]['f1_score']:.3f}.",
        'Estas métricas de lluvia pertenecen a la prueba v16: 221 pares sobre 49 horas distintas. No se deben sumar a los pares de THL como si fueran una única evaluación.',
        '', '## Pronóstico diario y condiciones','',
        f"Último intervalo de entrada: {forecastmeta['origen_intervalo_entrada']}; día completo: {forecastmeta['dia_pronosticado']}.",
        'Se entrega CSV horario, resumen por madrugada/mañana/tarde/noche e imagen. Temperatura y humedad son promedios; lluvia se suma; luz se expresa en lux.',
        'El cielo mostrado es una condición orientativa: lluvia prevista ≥0,1 mm/h; de día, luz relativa al percentil 90 horario; de noche, sin señal de lluvia, cielo no determinado. Estas reglas no constituyen un clasificador validado de soleado/nublado/lluvioso.',
        'No se generan porcentajes de confianza de cielo. Accuracy, precision, recall y F1 corresponden únicamente a las categorías binarias de lluvia. Para las variables continuas se usan MAE, RMSE, sesgo y R².',
        'Los rangos de error numéricos proceden del percentil 90 de residuos de validación, agrupados por hora y tramo de horizonte. La cobertura observada se documenta en cobertura_rangos.csv; no se garantiza cobertura de 90%. El rango térmico de la tarjeta es mínimo/máximo de la curva puntual.',
        '', '## Uso','',
        '1. Instalar requirements.txt con Python 3.12.',
        '2. Ejecutar `python predecir.py` para usar los modelos guardados.',
        '3. Para nuevos registros: `python predecir.py --dataset datos_horarios.csv --salida resultados_nuevos`.',
        '4. El CSV debe conservar el esquema horario de datos/dataset_local_mejorado.csv; un CSV con JSON crudo requiere preparación previa.',
        '5. Para reproducir entrenamiento: `python entrenar_pronostico.py`. Para validar entrega: `python verificar.py`.',
        '6. El proceso exige 24 horas recientes completas en la estación 3; no inventa entradas si faltan.',
        '', '## Archivos principales','',
        '- resultados/metricas_regresion.csv y seleccion_validacion.csv.',
        '- resultados/errores_detallados.csv y metricas_luz_diurna.csv.',
        '- resultados/pronostico_horario_48h.csv y pronostico_diario.csv.',
        '- resultados/pronostico_diario.png y gráficas de entrenamiento y error.',
        '- modelos/: pesos, transformaciones, ajustes y política seleccionada.',
        '- lluvia/: modelo mixto conservado, métricas y matriz de confusión.',
        '', '## Atribución histórica','',
        'Contains modified Copernicus Climate Change Service Information 2026.',
        'Neither the European Commission nor ECMWF is responsible for any use that may be made of the Copernicus Information or Data it contains.',
        'ERA5-Land: https://developers.google.com/earth-engine/datasets/catalog/ECMWF_ERA5_LAND_HOURLY',
        'NASA POWER y Open-Meteo: históricos proporcionados en el dataset original. Se conservan fuente y fechas. Puede existir dependencia entre productos; no son mediciones independientes del campus.']
    (ROOT/'RESULTADOS.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    images=['pronostico_diario.png','pronostico_variables.png','curvas_entrenamiento.png',
            'errores_prueba.png','errores_por_horizonte.png','distribucion_errores.png','comparacion_modelos.png']
    htmlbody=''.join(f'<h2>{html.escape(n.replace("_"," ").replace(".png",""))}</h2><img src="resultados/{n}">' for n in images)
    page='<!doctype html><meta charset="utf-8"><title>Resultados del pronóstico IA</title><style>body{font:16px system-ui;max-width:1250px;margin:32px auto;padding:0 24px;color:#20334b}img{width:100%;height:auto}table{border-collapse:collapse}td,th{padding:10px;border-bottom:1px solid #ddd}a{color:#216c8e}</style>'
    page+='<h1>Resultados del pronóstico IA</h1><p><a href="RESULTADOS.md">Informe y método</a> · <a href="resultados/metricas_regresion.csv">Métricas completas</a></p>'
    page+=selected[['variable','mae','rmse','r2','horas_distintas']].to_html(index=False,float_format=lambda x:f'{x:.3f}')
    page+=htmlbody+'<h2>Matriz de confusión de lluvia</h2><img src="lluvia/matriz_confusion_prueba.png"><p>Referencia ERA5 ≥0,1 mm/h; 49 horas distintas.</p>'
    (ROOT/'RESULTADOS.html').write_text(page,encoding='utf-8')
    print('INFORME Y GRÁFICAS GENERADOS',flush=True)


if __name__=='__main__':generar()
