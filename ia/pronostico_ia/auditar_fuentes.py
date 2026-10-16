from pathlib import Path
import hashlib
import json
import pandas as pd

ROOT=Path(__file__).resolve().parent
result={}
for name,date in [('dataset_local_mejorado.csv','fecha_hora'),('historicos_externos_Wm2.csv','fecha_hora'),
                  ('era5_gee_Wm2.csv','fecha_hora'),('era5_original.csv','fecha_utc')]:
    path=ROOT/'datos'/name
    frame=pd.read_csv(path)
    item={'filas':len(frame),'inicio':str(frame[date].min()),'fin':str(frame[date].max()),
          'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'columnas':list(frame)}
    if 'fuente' in frame:item['por_fuente']=frame.fuente.value_counts().to_dict()
    result[name]=item
result['politica']={'radiacion_local':'lux','radiacion_externa':'W/m²',
    'lluvia_local':'mm publicados parciales; se usa como entrada, no como objetivo de lluvia',
    'lluvia_objetivo':'ERA5-Land regional; modelo mixto v16 preservado',
    'preentrenamiento':'pesos históricos incluidos, no se repite entrenamiento histórico desde cero en esta carpeta',
    'etiquetas_clima_originales_usadas_para_entrenar':'no',
    'coordenadas_era5_aproximadas':[-74.354,4.813],
    'ultima_ubicacion_exacta_campus_verificada':False}
(ROOT/'datos/FUENTES.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print({k:v['filas'] for k,v in result.items() if 'filas' in v})
