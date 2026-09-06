import { Component, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { MeasurementType } from '../../service/MeasurementType/measurement-type';
import { IMeasurementType } from '../../models/IMeasurementType';
import { MeasurementService } from '../../service/measurement/mesurement-service';
import { IMeasurement } from '../../models/IMeasurement';
import { ApexAxisChartSeries, ApexChart, ApexXAxis, ApexYAxis, ApexTitleSubtitle, NgApexchartsModule } from "ng-apexcharts";
import { CommonModule } from '@angular/common';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { OnInit } from '@angular/core';
import { ChartComponent } from "ng-apexcharts";
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormControl} from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ChecklistService } from '../../service/checklist/checklist-service';

@Component({
  selector: 'app-report',
  imports: [MATERIAL_MODULES, NgApexchartsModule, CommonModule, ChartComponent, ReactiveFormsModule],
  templateUrl: './report.html',
  styleUrls: ['./report.scss']
})
export class Report implements OnInit {

  filterForm: FormGroup;
  showDownloadOptions = false;

  displayedColumns: string[] = ['date', 'value', 'unit'];

  // Usamos MatTableDataSource para que la paginación/filtrado funcione correctamente
  dataSource = new MatTableDataSource<IMeasurement>([]);

  // Como el paginator puede crearse sólo cuando showReport = true, lo dejamos nullable
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  @ViewChild("chartRef") chartRef!: ChartComponent;  // Referencia a la gráfica

  constructor(
    public _MeasurementTypeService: MeasurementType,
    public _measurementService: MeasurementService,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private _checklistService: ChecklistService
  ) {
    this.filterForm = this.fb.group({
      fechaInicio: ['', Validators.required],
      fechaFin: ['', Validators.required]
    });
  }

  showReport: boolean = false;
  measurementTypeId: string = '';
  startDate!: Date;
  endDate!: Date;
  measurementTypes: IMeasurementType[] = [];
  sensorControl = new FormControl('');
  filteredSensors: any[] = [];

  chart: ApexChart = {
    type: "line"
  }
  title: ApexTitleSubtitle = {
    text: "Gráfica",
    align: "left"
  }

  series: ApexAxisChartSeries = [];
  XAxis: ApexXAxis = { type: "datetime", categories: [] }
  Yaxis: ApexYAxis = { title: { text: '' } }

  ngOnInit(): void {
    this._MeasurementTypeService.GetAllMeasurementTypes().subscribe({
      next: (data) => {
        this.measurementTypes = data;
      },
      error: (err) => {
        console.error('Error cargando tipos de medición:', err);
      }
    });
    this.sensorControl.valueChanges.subscribe(value => {
      if (!value) {
        this.filteredSensors = this.measurementTypes;
        return;
      }

      // Si el valor es texto, filtra
      if (typeof value === 'string') {
        const filterValue = value.toLowerCase();
        this.filteredSensors = this.measurementTypes.filter(sensor =>
          sensor.name.toLowerCase().includes(filterValue)
        );
      } else {
        // Si el valor es un objeto (sensor seleccionado)
        this.filteredSensors = this.measurementTypes;
      }
    });
  }

  filterSensors(value: string) {
    const filterValue = value.toLowerCase();
    return this.measurementTypes.filter(sensor =>
      sensor.name.toLowerCase().includes(filterValue)
    );
  }

  showAllSensors() {
    this.filteredSensors = this.measurementTypes; // muestra todos las mediciones
  }

  displaySensorName(sensorID: any): string {
    var selectedSensor = this.measurementTypes.find(s => s.id === sensorID) || null;

    return selectedSensor && selectedSensor.name
      ? this.formatNameSensor(selectedSensor.name)
      : '';
  }

  formatNameSensor(device_Name: string): string {
    if (device_Name === 'TemperaturaAire') return 'Temperatura Ambiente';
    if (device_Name === 'HumedadAire') return 'Humedad Relativa';
    if (device_Name === 'Turbidez') return 'Turbidez TP1';
    if (device_Name === 'OxigenoDisuelto') return 'Oxigeno Disuelto TP1';
    if (device_Name === 'Conductividad') return 'Conductividad TP1';
    if (device_Name === 'Ph') return 'PH TP1';
    if (device_Name === 'TemperaturaAgua') return 'Temperatura Agua TP1';
    if (device_Name === 'Nivel') return 'Nivel TP1';
    return device_Name;
  }


  onGenerateChecklistClick() {
    this.showDownloadOptions = true;

    setTimeout(() => {
      this.cdr.detectChanges();
    }, 50);
  }

  descargarCSV() {
    const { fechaInicio, fechaFin } = this.filterForm.value;

    if (!fechaInicio || !fechaFin) {
      alert('Por favor selecciona las fechas de inicio y fin.');
      return;
    }

    // Convertimos a ISO corto (YYYY-MM-DD)
    const startIso = new Date(fechaInicio).toISOString().split('T')[0];
    const endIso = new Date(fechaFin).toISOString().split('T')[0];

    this._measurementService.GenerateAllMeasurementsCsv(startIso, endIso).subscribe({
      next: (data: Blob) => {
        const blob = new Blob([data], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = `Mediciones_${startIso}_a_${endIso}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        this.snackBar.open('📥 CSV de mediciones descargado con éxito', 'Cerrar', {
          duration: 3000
        });
      },
      error: (err) => {
        console.error('Error al descargar CSV:', err);
        this.snackBar.open('❌ Error al descargar CSV', 'Cerrar', {
          duration: 3000
        });
      }
    });
  }

  onSubmit() {
    const measurementTypeId = this.measurementTypeId;
    const fechaInicio = this.startDate;
    const fechafin = this.endDate;

    this._measurementService.GetMeasurementsByDate(measurementTypeId, fechaInicio, fechafin)
      .subscribe({
        next: (measurements: any[]) => {
          // Mapeamos para extraer la unidad y mantener una estructura plana
          const mappedMeasurements: IMeasurement[] = measurements.map(m => ({
            id: m.id,
            value: m.value,
            date: m.date,
            unit: m.measurement_Type?.unit_Measurement?.name ?? '' // extraemos la unidad
          }));

          // 1) Actualizamos el dataSource
          this.dataSource.data = mappedMeasurements;

          // 2) Actualizamos la gráfica
          this.series = [
            {
              name: "Mediciones",
              data: mappedMeasurements.map(m => m.value)
            }
          ];
          this.XAxis = {
            type: "datetime", categories: mappedMeasurements.map(m => m.date),
            title: { text: 'Fecha y hora' }
          };

          this.Yaxis = { title: { text: mappedMeasurements[0]?.unit ?? '' } };

          // 3) Mostramos la tabla y luego conectamos el paginator
          this.showReport = true;
          this.cdr.detectChanges();

          if (this.paginator) {
            this.dataSource.paginator = this.paginator;
            this.paginator.pageSize = 15;
          }
        },
        error: (err) => {
          console.error('Error cargando mediciones:', err);
        }
      });
  }

  onPDF() {
    const measurementTypeId = this.measurementTypeId;
    const fechaInicio = this.startDate;
    const fechafin = this.endDate;

    if (this.chartRef) {
      this.chartRef.dataURI().then((res) => {
        if ("imgURI" in res) {
          const imgURI = res.imgURI;
          this._measurementService.GetMeasurementsForPdf(measurementTypeId, fechaInicio, fechafin, imgURI)
            .subscribe({
              next: (response) => {
                // Crear Blob con tipo correcto
                const blob = new Blob([response.body!], { type: response.headers.get('Content-Type')! });

                // Obtener el nombre del archivo desde Content-Disposition si existe
                const contentDisposition = response.headers.get('Content-Disposition');
                let filename = 'Reporte.pdf';
                if (contentDisposition) {
                  const match = contentDisposition.match(/filename="?([^"]+)"?/);
                  if (match) filename = match[1];
                }

                // Crear enlace temporal para forzar descarga con diálogo
                const link = document.createElement('a');
                link.href = window.URL.createObjectURL(blob);
                link.download = filename;
                link.click();
                window.URL.revokeObjectURL(link.href);

              },
              error: async (err) => {
                if (err.error instanceof Blob) {
                  const text = await err.error.text();
                  console.error("Error detallado:", text);
                } else {
                  console.error("Error al generar el PDF", err);
                }
              }
            });
        }
      });
    }
  }
}
