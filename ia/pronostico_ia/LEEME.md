# Pronóstico IA

Abre **RESULTADOS.html** para ver el pronóstico, métricas y gráficas.
El método completo está en **RESULTADOS.md**.

- **predecir.py**: genera las siguientes 48 horas y el resumen del día con los modelos guardados.
- **entrenar_pronostico.py**: reproduce entrenamiento, validación y resultados.
- **verificar.py**: recalcula métricas y verifica modelos, fechas, lluvia y agregaciones.
- **resultados/**: CSV de métricas, errores, pronósticos y gráficas.
- **modelos/**: pesos y transformaciones de temperatura, humedad y luz.
- **lluvia/**: modelo mixto de lluvia conservado y evaluación binaria de ERA5.
- **datos/**: fuentes locales e históricas con sus unidades separadas.
- **preentrenado/**: pesos de inicialización histórica y metadatos.

Los demás módulos Python son funciones de apoyo. Para usar el modelo, ejecuta desde esta carpeta:

```powershell
python predecir.py
```

Con nuevos datos horarios que conserven el esquema del CSV incluido:

```powershell
python predecir.py --dataset datos_horarios.csv --salida resultados_nuevos
```

El proceso necesita 24 horas recientes completas de la estación 3. Las tres estaciones
participan en entrenamiento y pueden aportar luz medida simultáneamente como respaldo.
No se aceptan directamente CSV con JSON crudo; primero deben convertirse al formato horario.

La luz local se expresa en **lux**. La lluvia predice la variable regional ERA5 aprendida
desde sensores. Las condiciones de cielo son reglas orientativas y no incluyen porcentajes calibrados.
