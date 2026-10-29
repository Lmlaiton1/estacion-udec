from pathlib import Path
import os
import nbformat as nbf
from nbclient import NotebookClient

ROOT=Path(__file__).resolve().parent
os.environ['IPYTHONDIR']=str(ROOT/'.ipython')
nb=nbf.v4.new_notebook()
nb.metadata.kernelspec={'display_name':'Python 3','language':'python','name':'python3'}
nb.cells=[
    nbf.v4.new_markdown_cell('''# Resultados del pronóstico IA

Facatativá, extensión de la Universidad de Cundinamarca. Referencia: estación 3.
Este cuaderno carga los resultados y modelos ya entrenados; no vuelve a entrenar al abrirlo.
Para reproducir el ajuste completo: `python entrenar_pronostico.py`.
Para nuevas entradas horarias: `python predecir.py --dataset datos_horarios.csv`.
El método, las unidades y el alcance de la evaluación están en **RESULTADOS.md**.'''),
    nbf.v4.new_code_cell('''from pathlib import Path
import json
import pandas as pd
from IPython.display import display, Image, Markdown
root = Path.cwd()
if not (root / 'resultados').exists():
    raise RuntimeError('Ejecuta este cuaderno desde la carpeta pronostico ia')
metrics = pd.read_csv(root / 'resultados/metricas_regresion.csv')
selected = metrics[(metrics.particion == 'test') & (metrics.modelo == 'sistema_seleccionado')]
display(selected[['variable', 'horizonte', 'mae', 'rmse', 'r2', 'n_pares', 'horas_distintas']])'''),
    nbf.v4.new_markdown_cell('## Validación y arquitectura'),
    nbf.v4.new_code_cell('''summary = json.loads((root / 'resultados/resumen_entrenamiento.json').read_text(encoding='utf-8'))
display(pd.DataFrame([{'modelo': name, 'ventanas_train': spec['datos']['ventanas'],
                      'ventanas_val': spec['validacion']['ventanas'], 'parametros': spec['parametros'],
                      'epocas': spec['epocas'], 'epoca_restaurada': spec['epoca_restaurada']}
                     for name, spec in summary['corridas'].items()]))
display(pd.read_csv(root / 'resultados/seleccion_validacion.csv'))
display(pd.read_csv(root / 'resultados/cobertura_rangos.csv'))
display(Image(filename=str(root / 'resultados/curvas_entrenamiento.png')))'''),
    nbf.v4.new_markdown_cell('## Pronóstico del siguiente día'),
    nbf.v4.new_code_cell('''display(pd.read_csv(root / 'resultados/pronostico_diario.csv'))
display(Image(filename=str(root / 'resultados/pronostico_diario.png')))
display(Image(filename=str(root / 'resultados/pronostico_variables.png')))'''),
    nbf.v4.new_markdown_cell('## Errores y clasificación binaria de lluvia\n\nTHL se evalúa como regresión. La matriz de lluvia conserva la prueba de v16 contra ERA5 ≥0,1 mm/h. No es una matriz de soleado/nublado/lluvioso.'),
    nbf.v4.new_code_cell('''display(Image(filename=str(root / 'resultados/errores_prueba.png')))
display(Image(filename=str(root / 'resultados/errores_por_horizonte.png')))
display(Image(filename=str(root / 'resultados/distribucion_errores.png')))
display(Image(filename=str(root / 'lluvia/matriz_confusion_prueba.png')))
rain = json.loads((root / 'lluvia/metricas_clasificacion_prueba.json').read_text(encoding='utf-8'))
print('Accuracy de lluvia:', rain['accuracy'])
display(pd.DataFrame(rain['por_categoria']))''')]
client=NotebookClient(nb,timeout=180,kernel_name='python3',resources={'metadata':{'path':str(ROOT)}})
client.execute()
assert not any(o.output_type=='error' for cell in nb.cells if cell.cell_type=='code' for o in cell.get('outputs',[]))
nbf.write(nb,ROOT/'Resultados_Pronostico_IA.ipynb')
print('Cuaderno ejecutado: 4 celdas de código, sin errores')
