# Resultados del pronóstico IA

Ubicación: extensión de la Universidad de Cundinamarca en Facatativá. Referencia: estación 3.

## Métricas de prueba: temperatura, humedad y luz

| Variable | MAE | RMSE | R² | Horas distintas |
|---|---:|---:|---:|---:|
| Temperatura (°C) | 1.378 | 2.257 | 0.704 | 88 |
| Humedad (%) | 3.272 | 7.191 | 0.065 | 88 |
| Luz (lux) | 2189.984 | 4180.552 | 0.911 | 87 |

Las métricas usan pares origen/hora objetivo. Las horas se repiten desde distintos orígenes; no son observaciones independientes.
MAE mide error absoluto medio; RMSE da mayor peso a errores grandes; R² negativo indica peor desempeño que la media del conjunto evaluado.

## Selección y validación

- Temperatura (°C): lstm_historicos_mezcla50.
- Humedad (%): persistencia_24h.
- Luz (lux): mediana_horaria.

La selección de modelo y mezcla usa únicamente MAE de validación, no los errores de prueba. Los modelos finales se reajustan con todos los datos válidos; sus métricas con días posteriores aún no están disponibles.

## Entrenamiento y arquitectura

Entradas: 24 horas de temperatura, humedad, luz y lluvia publicada; el modelo auxiliar usa las primeras tres para aprovechar intervalos sin lluvia registrada. Salidas: 48 horas de temperatura, humedad y luz.
LSTM bayesiana variacional de 8 unidades, decodificador bayesiano de 16 unidades, posterior gaussiano de campo medio y regularización KL. La regresión usa ridge; las referencias usan repetición de 24 h y mediana por hora.
El horizonte incluye su posición y la hora de salida calculada desde el índice temporal. No se consulta una variable futura medida ni se añaden sensores.
Inicialización histórica con pesos NASA POWER, Open-Meteo y adaptación ERA5-Land; las escalas W/m² históricas y lux locales son independientes. Los pesos iniciales y sus metadatos se incluyen en preentrenado.

| Modelo | Ventanas train | Ventanas validación | Parámetros | Épocas | Época restaurada |
|---|---:|---:|---:|---:|---:|
| lstm_local | 389 | 6 | 1546 | 19 | 7 |
| lstm_historicos | 389 | 6 | 1546 | 19 | 7 |
| lstm_auxiliar | 664 | 10 | 1382 | 22 | 10 |

Train: objetivos anteriores al 30/09/2026. Validación: 30/09–04/10. Prueba: 05/10–15/10, donde hay mediciones válidas. Ajustes de estaciones, normalizaciones y medianas de evaluación se calculan sólo con train.
Se excluyen horas parciales, cobertura insuficiente y luz constante o cero anómalo diurno. Sólo se admite respaldo de luz medido simultáneamente por otra estación, con procedencia. No se interpolan huecos temporales.
La evaluación es retrospectiva; estos registros ya fueron inspeccionados en trabajos previos. No equivale a una evaluación prospectiva nueva.

## Lluvia preservada

Se mantienen los pesos y transformaciones de la LSTM mixta v16. Usa sensores recientes como entrada y lluvia regional ERA5 como objetivo. La lluvia parcial de estaciones no se transforma en total.
Prueba binaria ERA5 ≥0,1 mm/h: accuracy 56.56%; precisión lluvia 62.39%; recall 55.28%; F1 0.586.
Estas métricas de lluvia pertenecen a la prueba v16: 221 pares sobre 49 horas distintas. No se deben sumar a los pares de THL como si fueran una única evaluación.

## Pronóstico diario y condiciones

Último intervalo de entrada: 2026-10-15 13:00:00; día completo: 2026-10-16.
Se entrega CSV horario, resumen por madrugada/mañana/tarde/noche e imagen. Temperatura y humedad son promedios; lluvia se suma; luz se expresa en lux.
El cielo mostrado es una condición orientativa: lluvia prevista ≥0,1 mm/h; de día, luz relativa al percentil 90 horario; de noche, sin señal de lluvia, cielo no determinado. Estas reglas no constituyen un clasificador validado de soleado/nublado/lluvioso.
No se generan porcentajes de confianza de cielo. Accuracy, precision, recall y F1 corresponden únicamente a las categorías binarias de lluvia. Para las variables continuas se usan MAE, RMSE, sesgo y R².
Los rangos de error numéricos proceden del percentil 90 de residuos de validación, agrupados por hora y tramo de horizonte. La cobertura observada se documenta en cobertura_rangos.csv; no se garantiza cobertura de 90%. El rango térmico de la tarjeta es mínimo/máximo de la curva puntual.

## Uso

1. Instalar requirements.txt con Python 3.12.
2. Ejecutar `python predecir.py` para usar los modelos guardados.
3. Para nuevos registros: `python predecir.py --dataset datos_horarios.csv --salida resultados_nuevos`.
4. El CSV debe conservar el esquema horario de datos/dataset_local_mejorado.csv; un CSV con JSON crudo requiere preparación previa.
5. Para reproducir entrenamiento: `python entrenar_pronostico.py`. Para validar entrega: `python verificar.py`.
6. El proceso exige 24 horas recientes completas en la estación 3; no inventa entradas si faltan.

## Archivos principales

- resultados/metricas_regresion.csv y seleccion_validacion.csv.
- resultados/errores_detallados.csv y metricas_luz_diurna.csv.
- resultados/pronostico_horario_48h.csv y pronostico_diario.csv.
- resultados/pronostico_diario.png y gráficas de entrenamiento y error.
- modelos/: pesos, transformaciones, ajustes y política seleccionada.
- lluvia/: modelo mixto conservado, métricas y matriz de confusión.

## Atribución histórica

Contains modified Copernicus Climate Change Service Information 2026.
Neither the European Commission nor ECMWF is responsible for any use that may be made of the Copernicus Information or Data it contains.
ERA5-Land: https://developers.google.com/earth-engine/datasets/catalog/ECMWF_ERA5_LAND_HOURLY
NASA POWER y Open-Meteo: históricos proporcionados en el dataset original. Se conservan fuente y fechas. Puede existir dependencia entre productos; no son mediciones independientes del campus.
