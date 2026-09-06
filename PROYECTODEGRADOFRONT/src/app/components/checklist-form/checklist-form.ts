import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormControl } from '@angular/forms';
import { ChecklistService } from '../../service/checklist/checklist-service';
import { DeviceService } from '../../service/device/device-service';
import { CommonModule } from '@angular/common';
import { MATERIAL_MODULES } from '../../shared/material';
import { ChangeDetectorRef } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Authservice } from '../../service/auth/authservice';
import { UserPost } from '../../models/UserPost';
import { Rolservice } from '../../service/rol/rolservice';
import { Actuator } from '../../models/Actuator';
import { Sensor } from '../../models/Sensor';
import { Router } from '@angular/router';
import { PageEvent } from '@angular/material/paginator';
import { NgApexchartsModule } from 'ng-apexcharts';


@Component({
  selector: 'app-checklist-form',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, ReactiveFormsModule, NgApexchartsModule],
  templateUrl: './checklist-form.html',
  styleUrls: ['./checklist-form.scss']
})
export class ChecklistFormComponent implements OnInit {
  checklistForm: FormGroup;
  filterForm: FormGroup;
  filterForm2: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';
  private usuarioIdActivo: string | null = null;
  filteredChecklists: any[] = [];
  showDownloadOptions = false;
  showDownloadOptions2 = false;
  showComparativa = false;
  showReporte = false;
  startDate: Date | null = null;
  endDate: Date | null = null;
  rolUsuario: string | null = null;
  user!: UserPost | null;
  fechaInicio: Date | null = null;
  fechaFin: Date | null = null;
  actuators: Actuator[] = [];
  sensors: Sensor[] = [];
  sensores: any[] = [];
  pageSize = 9;
  pageIndexSensors = 0;
  pageIndexActuators = 0;
  showReport: boolean = false;
  series: any[] = [];
  XAxis: any = {};
  Yaxis: any = {};
  unitSelected: string = '';
  private sensorControlSub: any;
  sensorControl = new FormControl('');
  filteredSensors: any[] = [];

  constructor(
    private fb: FormBuilder,
    private _checklistService: ChecklistService,
    private _deviceService: DeviceService,
    private cdr: ChangeDetectorRef,
    private snackBar: MatSnackBar,
    public _authService: Authservice,
    public _RolService: Rolservice,
    private router: Router
  ) {
    this.checklistForm = this.fb.group({
      usuarioId: ['', Validators.required],
      usuarioDisplay: ['', Validators.required],
      fecha: ['', Validators.required],
      observacion: ['', [Validators.required, Validators.pattern(/^[^;"\n\r]*$/)]],
      detallesSensores: this.fb.array([]),
      detallesActuadores: this.fb.array([])
    });

    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._RolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
      });
    }

    this.filterForm = this.fb.group({
      fechaInicio: ['', Validators.required],
      fechaFin: ['', Validators.required]
    });

    this.filterForm2 = this.fb.group({
      sensorId: ['', Validators.required],
      fechaInicio: ['', Validators.required],
      fechaFin: ['', Validators.required]
    });
  }

  filtrarCaracteres(event: any) {
    const valor = event.target.value;
    const valorFiltrado = valor.replace(/[;"\n\r]/g, '');
    if (valor !== valorFiltrado) {
      event.target.value = valorFiltrado;
      this.checklistForm.get('observacion')?.setValue(valorFiltrado, { emitEvent: false });
    }
  }


  ngOnInit() {
    this.loadCurrentUser();
    this.loadSensors();
    this.loadActuators();

    const now = new Date();
    this.checklistForm.patchValue({
      fecha: now.toLocaleString('sv-SE').replace(' ', 'T')
    });

    this.filteredSensors = this.sensors;

    this.sensorControl.valueChanges.subscribe(value => {
      if (!value) {
        this.filteredSensors = this.sensors;
        return;
      }

      // Si el valor es texto, filtra
      if (typeof value === 'string') {
        const filterValue = value.toLowerCase();
        this.filteredSensors = this.sensors.filter(sensor =>
          sensor.device_Name.toLowerCase().includes(filterValue)
        );
      } else {
        // Si el valor es un objeto (sensor seleccionado)
        this.filteredSensors = this.sensors;
      }

      // Sincroniza el valor con el formControl del formulario
      this.filterForm2.get('sensorId')?.setValue(
        value
      );
    });
  }

  get detallesSensores(): FormArray {
    return this.checklistForm.get('detallesSensores') as FormArray;
  }

  get detallesActuadores(): FormArray {
    return this.checklistForm.get('detallesActuadores') as FormArray;
  }

  filterSensors(value: string) {
    const filterValue = value.toLowerCase();
    return this.sensors.filter(sensor =>
      sensor.device_Name.toLowerCase().includes(filterValue)
    );
  }

  displaySensorName(sensorId: any): string {

    var selectedSensor = this.sensors.find(s => s.id === sensorId) || null;

    return selectedSensor && selectedSensor.device_Name
      ? this.formatNameSensor(selectedSensor.device_Name)
      : '';
  }

  showAllSensors() {
    this.filteredSensors = this.sensors; // muestra todos los sensores
  }

  onGenerateChecklistClick() {
    // Alterna entre mostrar y ocultar
    this.showDownloadOptions = !this.showDownloadOptions;

    // Forzar actualización en la vista cuando cambia el estado
    setTimeout(() => {
      this.cdr.detectChanges();
    }, 50);
  }

  onGenerateChecklistClick2() {
    // Alternar el panel y la gráfica
    this.showDownloadOptions2 = !this.showDownloadOptions2;
    this.showReport = false;       // Siempre ocultamos la gráfica al cerrar/abrir
    this.showComparativa = false;  // Si quieres ocultar también ese contenido (opcional)

    // Forzar refresco
    setTimeout(() => {
      this.cdr.detectChanges();
    }, 50);
  }

  loadCurrentUser() {
    const userData = localStorage.getItem('Usuario');
    if (userData) {
      const user = JSON.parse(userData);
      const id = user.Id ?? user.id ?? user.userId ?? user.IdUser;
      const fullName = `${user.userName} ${user.userLastName || ''}`.trim();

      this.usuarioIdActivo = id;
      this.checklistForm.patchValue({
        usuarioId: id,
        usuarioDisplay: fullName
      });
    }
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

  loadSensors(): void {
    this._deviceService.getSensors().subscribe({
      next: (res) => {
        this.sensors = res;
        this.cargarSensores(res);
      },
      error: () => this.errorMessage = 'Error cargando sensores'
    });
  }

  loadActuators(): void {
    this._deviceService.getActuators().subscribe({
      next: (res) => {
        this.actuators = res;
        this.cargarActuadores(res);
      },
      error: () => this.errorMessage = 'Error cargando actuadores'
    });
  }

  cargarSensores(sensores: Sensor[]) {
    this.detallesSensores.clear();
    sensores.forEach(sensor => {
      this.detallesSensores.push(this.fb.group({
        dispositivoId: [sensor.id, Validators.required],
        medicionSensor: ['', Validators.required],
        medicionManual: ['', Validators.required],
        estado: [false],
      }));
    });
  }

  cargarActuadores(actuadores: Actuator[]) {
    this.detallesActuadores.clear();
    actuadores.forEach(actuator => {
      this.detallesActuadores.push(this.fb.group({
        dispositivoId: [actuator.id, Validators.required],
        estado: [false],
        medicionSensor: [null],
        medicionManual: [null]
      }));
    });
  }

  onSubmit() {
    if (!this.checklistForm.valid) {
      this.errorMessage = 'Por favor complete todos los campos';
      return;
    }

    this.isSubmitting = true;
    const detallesRaw = [
      ...this.detallesSensores.value,
      ...this.detallesActuadores.value
    ];

    const detalles = detallesRaw.map((d: any) => ({
      dispositivoId: d.dispositivoId,
      estado: !!d.estado,
      medicionSensor: d.medicionSensor ?? null,
      medicionManual: d.medicionManual ?? null
    }));

    if (!this.usuarioIdActivo) {
      this.errorMessage = 'No hay usuario activo en sesión';
      this.isSubmitting = false;
      return;
    }

    const payload = {
      usuarioId: this.usuarioIdActivo,
      fecha: (this.checklistForm.value.fecha),
      observacion: this.checklistForm.value.observacion,
      detalles: detalles
    };

    this._checklistService.addChecklist(payload).subscribe({
      next: () => {
        this.isSubmitting = false;

        this.snackBar.open('✅ Checklist agregado correctamente', 'Cerrar', {
          duration: 3000
        });

        this.router.navigate(['/menu/Actuator']);

      },
      error: (err) => {
        this.errorMessage = 'Error al agregar checklist';
        this.isSubmitting = false;
        console.error(err);
      }
    });
  }

  esAdministrador(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  getPagedSensores() {
    const start = this.pageIndexSensors * this.pageSize;
    const end = start + this.pageSize;
    return this.detallesSensores.controls.slice(start, end);
  }

  getPagedActuadores() {
    const start = this.pageIndexActuators * this.pageSize;
    const end = start + this.pageSize;
    return this.detallesActuadores.controls.slice(start, end);
  }


  // Métodos para actualizar la página actual
  onPageChangeSensors(event: PageEvent): void {
    this.pageIndexSensors = event.pageIndex;
    this.cdr.detectChanges();
  }

  onPageChangeActuators(event: PageEvent): void {
    this.pageIndexActuators = event.pageIndex;
    this.cdr.detectChanges();
  }

  private updateSelectedUnit(sensorId: string | null) {
    if (!sensorId) {
      this.unitSelected = '';
    } else {
      const selected = this.sensors.find(s => s.id === sensorId);
      this.unitSelected = selected?.unit ?? '';
    }

    this.Yaxis = {
      title: { text: this.unitSelected || '' }
    };
  }


  generateGraph() {
    const sensorId = this.filterForm2.get('sensorId')?.value;
    const fechaInicio = this.filterForm2.get('fechaInicio')?.value;
    const fechaFin = this.filterForm2.get('fechaFin')?.value;

    if (!sensorId || !fechaInicio || !fechaFin) {
      this.snackBar.open('Debe seleccionar sensor y rango de fechas', 'Cerrar', {
        duration: 3000
      });
      return;
    }

    this.updateSelectedUnit(sensorId);

    const startIso = new Date(fechaInicio).toISOString();
    const endIso = new Date(fechaFin).toISOString();

    this._checklistService.getSensorMeasurements(sensorId, startIso, endIso)
      .subscribe({
        next: (measurements: any[]) => {

          //Mapear los datos
          const mappedMeasurements = measurements.map(m => ({
            fecha: m.fecha,
            medicionSensor: m.medicionSensor ?? null,
            medicionManual: m.medicionManual ?? null,
            sensorNombre: m.sensorNombre ?? ''
          }));

          console.log("📌 Datos mapeados:", mappedMeasurements);

          // Preparar las series para ApexCharts
          this.series = [
            {
              name: "Medición Sensor",
              data: mappedMeasurements.map(m => m.medicionSensor)
            },
            {
              name: "Medición Manual",
              data: mappedMeasurements.map(m => m.medicionManual)
            }
          ];

          // Eje X → fechas
          this.XAxis = {
            type: "datetime",
            categories: mappedMeasurements.map(m =>
              new Date(m.fecha + "Z").getTime()
            ),
            title: { text: 'Fecha' }
          };


          // Eje Y → no sabemos la unidad, así que lo dejamos genérico
          this.Yaxis = {
            title: { text: this.unitSelected || '' }
          };

          // Mostrar la gráfica
          this.showReport = true;
          this.cdr.detectChanges();

          this.snackBar.open('Gráfica generada correctamente', 'Cerrar', {
            duration: 3000
          });
        },
        error: (err) => {
          console.error('Error obteniendo mediciones:', err);
          this.snackBar.open('Error obteniendo mediciones', 'Cerrar', {
            duration: 3000
          });
        }
      });
  }


  descargarCSV() {
    const { fechaInicio, fechaFin } = this.filterForm.value;

    if (!fechaInicio || !fechaFin) {
      alert('Por favor selecciona las fechas de inicio y fin.');
      return;
    }

    const start = this.filterForm.get('fechaInicio')?.value;
    const end = this.filterForm.get('fechaFin')?.value;

    if (!start || !end) {
      alert('Por favor selecciona las fechas de inicio y fin.');
      return;
    }

    // Convertimos a ISO
    const startIso = new Date(start).toISOString().split('T')[0];
    const endIso = new Date(end).toISOString().split('T')[0];

    this._checklistService.descargarChecklistCSV(startIso, endIso).subscribe({
      next: (data: Blob) => {
        const blob = new Blob([data], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = `Checklist_Registro.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        this.snackBar.open('📥 CSV descargado con éxito', 'Cerrar', {
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
}