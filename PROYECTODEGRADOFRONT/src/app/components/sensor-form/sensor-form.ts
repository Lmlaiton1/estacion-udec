import { Component, OnInit, ViewChild, ChangeDetectorRef   } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { DeviceService } from '../../service/device/device-service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_MODULES } from '../../shared/material';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';


@Component({
  selector: 'app-sensor-form',
  imports: [MATERIAL_MODULES, ReactiveFormsModule,CommonModule],
  templateUrl: './sensor-form.html',
  styleUrl: './sensor-form.scss'
})
export class SensorForm implements OnInit{
  sensors: any[] = []; // Lista de sensores
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  sortDirection: { [key: string]: 'asc' | 'desc' } = {};

  sensorForm!: FormGroup;
  unitForm!: FormGroup; 
  showAddUnitForm = false;
  isEditMode = false;
  selectedActuatorId: string | null = null;
  selectedStateId: string | null = null;

  systemTypes: any[] = [];
  deviceTypes: any[] = [];
  statusList: any[] = [];
  unitList: any[] = [];

  constructor(
    private _deviceService: DeviceService,
    private _snackBar: MatSnackBar,
    private fb: FormBuilder,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSensors();
    this.loadDropdowns();

    // Crear formulario vacío
    this.sensorForm = this.fb.group({
      device_Name: ['', Validators.required],
      mark: ['', Validators.required],
      sn: ['', Validators.required],
      description: ['', Validators.required],
      system_TypeId: ['', Validators.required],
      unitId: ['', Validators.required],
      max: ['', Validators.required],
      min: ['', Validators.required]
    });
    this.unitForm = this.fb.group({
      name: ['', Validators.required]
    });
  }

  goToSensors() {
    setTimeout(() => {
      this.router.navigate(['/menu/Sensors']);
    }, 0);
  }

  loadSensors(): void {
    this._deviceService.GetAllSensors().subscribe({
      next: (data) => {
        this.sensors = data;
        this.dataSource.data = this.sensors;
      },
      error: () => this._snackBar.open('Error cargando sensores', 'Cerrar', { duration: 3000 })
    });
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.cd.detectChanges(); 
  }

  loadDropdowns(): void {

    this._deviceService.GetSystems().subscribe({
      next: (res) => this.systemTypes = res,
      error: () => this._snackBar.open('Error cargando sistemas', 'Cerrar', { duration: 3000 })
    });

    this._deviceService.GetStates().subscribe({
      next: (res) => this.statusList = res,
      error: () => this._snackBar.open('Error cargando estados', 'Cerrar', { duration: 3000 })
    });

    this._deviceService.GetUnits().subscribe({
      next: (res) => this.unitList = res,
      error: () => this._snackBar.open('Error cargando unidades', 'Cerrar', { duration: 3000 })
    });
    
  }

  sortData(column: string) {
    // Alternar dirección
    const current = this.sortDirection[column];
    this.sortDirection = { [column]: current === 'asc' ? 'desc' : 'asc' };

    const direction = this.sortDirection[column];
    const sorted = [...this.dataSource.filteredData].sort((a, b) => {
      const valA = (a[column] || '').toString().toLowerCase();
      const valB = (b[column] || '').toString().toLowerCase();

      if (valA < valB) return direction === 'asc' ? -1 : 1;
      if (valA > valB) return direction === 'asc' ? 1 : -1;
      return 0;
    });

    this.dataSource.data = sorted;
  }

  createActuator(): void {
    if (this.sensorForm.invalid) return;
    const min = this.sensorForm.get('min')?.value;
    const max = this.sensorForm.get('max')?.value;

    if (min > max || max < min){
       
      this._snackBar.open('❌ Error actualizando regla (Valores inversos o erroneos)', 'Cerrar', { duration: 3000 });

    }else{    
      this._deviceService.createSensor(this.sensorForm.value).subscribe({
        next: () => {
          this._snackBar.open('Sensor agregado ✅', 'Cerrar', { duration: 3000 });
          this.loadSensors();
          this.resetFormState();
        },
        error: () => this._snackBar.open('Error al agregar', 'Cerrar', { duration: 3000 })
      });
    }
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value.trim().toLowerCase();
    this.dataSource.filter = filterValue;
  }

  editActuator(sensor: any): void {
    this.isEditMode = true;
    this.selectedActuatorId = sensor.id;
    this.selectedStateId = sensor.device_StatusId;
    this.sensorForm.patchValue(sensor);
  }

  updateActuator(): void {
    if (this.sensorForm.invalid || !this.selectedActuatorId) return;
    
    const min = this.sensorForm.get('min')?.value;
    const max = this.sensorForm.get('max')?.value;
    
    const payload = { ...this.sensorForm.value, device_StatusId: this.selectedStateId };

    if (min > max || max < min){
       
      this._snackBar.open('❌ Error actualizando regla (Valores inversos o erroneos)', 'Cerrar', { duration: 3000 });

    }else{

      this._deviceService.updateSensor(this.selectedActuatorId, payload).subscribe({
        next: () => {
          this._snackBar.open('Sensor actualizado ✅', 'Cerrar', { duration: 3000 });
          this.isEditMode = false;
          this.selectedActuatorId = null;
          this.selectedStateId = null;
          this.resetFormState();
          this.loadSensors();
        },
        error: () => this._snackBar.open('Error al actualizar', 'Cerrar', { duration: 3000 })
      });

    }
  }

  toggleEnable(sensor: any): void {
    const newStatusId = sensor.device_StatusName === 'Deshabilitado'
      ? this.statusList.find(s => s.state_Name === 'Encendido').id
      : this.statusList.find(s => s.state_Name === 'Deshabilitado').id;

    const payload = { ...sensor, device_StatusId: newStatusId };

    this._deviceService.updateSensor(sensor.id, payload).subscribe({
      next: () => {
        this._snackBar.open('Estado actualizado ✅', 'Cerrar', { duration: 3000 });
        this.loadSensors();
      },
      error: () => this._snackBar.open('Error al cambiar estado', 'Cerrar', { duration: 3000 })
    });
  }

  createUnit() {
    if (this.unitForm.valid) {
      const newUnit = this.unitForm.value;

      this._deviceService.createUnit(newUnit).subscribe(() => {
        this._snackBar.open('Unidad Agregada ✅', 'Cerrar', { duration: 3000 });
        this.loadDropdowns();
      });

      this.resetFormUnit();
      this.showAddUnitForm = false;
    }
  }

  cancelEdit(): void {
    this.isEditMode = false;
    this.selectedActuatorId = null;
    this.selectedStateId = null;
    this.resetFormState();
  }

  resetFormUnit(): void {
    this.unitForm.reset();
    this.unitForm.markAsPristine();
    this.unitForm.markAsUntouched();

    Object.keys(this.unitForm.controls).forEach(key => {
      const control = this.unitForm.get(key);
      control?.setErrors(null);
    });
  }

  private resetFormState(): void {
    this.sensorForm.reset();
    this.sensorForm.markAsPristine();
    this.sensorForm.markAsUntouched();

    Object.keys(this.sensorForm.controls).forEach(key => {
      const control = this.sensorForm.get(key);
      control?.setErrors(null);
    });
  }

}
