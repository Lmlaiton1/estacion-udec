import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
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
  selector: 'app-actuator-form',
  imports: [MATERIAL_MODULES, ReactiveFormsModule,CommonModule],
  templateUrl: './actuator-form.html',
  styleUrl: './actuator-form.scss'
})
export class ActuatorForm implements OnInit {
  actuators: any[] = []; // Lista de actuadores
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  sortDirection: { [key: string]: 'asc' | 'desc' } = {};

  actuatorForm!: FormGroup;
  isEditMode = false;
  selectedActuatorId: string | null = null;
  selectedStateId: string | null = null;

  systemTypes: any[] = [];
  deviceTypes: any[] = [];
  statusList: any[] = [];

  constructor(
    private _deviceService: DeviceService,
    private _snackBar: MatSnackBar,
    private fb: FormBuilder,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadActuators();
    this.loadDropdowns();

    // Crear formulario vacío
    this.actuatorForm = this.fb.group({
      device_Name: ['', Validators.required],
      mark: ['', Validators.required],
      sn: ['', Validators.required],
      description: ['', Validators.required],
      system_TypeId: ['', Validators.required],
      auto: ['', Validators.required]
    });
  }

  goToActuator() {
    this.router.navigate(['/menu/Actuator']);
  }

  loadActuators(): void {
    
    this._deviceService.GetAllActuators().subscribe({
       next: (data) => {
        this.actuators = data;
        this.dataSource.data = this.actuators;
      },
      error: () => this._snackBar.open('Error cargando actuadores', 'Cerrar', { duration: 3000 })
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
    
  }

  createActuator(): void {
    if (this.actuatorForm.invalid) return;
    
    this._deviceService.createActuator(this.actuatorForm.value).subscribe({
      next: () => {
        this._snackBar.open('Actuador agregado ✅', 'Cerrar', { duration: 3000 });
        this.loadActuators();
        this.resetFormState();
      },
      error: () => this._snackBar.open('Error al agregar', 'Cerrar', { duration: 3000 })
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value.trim().toLowerCase();
    this.dataSource.filter = filterValue;
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

  // Editar actuador existente
  editActuator(actuator: any): void {
    this.isEditMode = true;
    this.selectedActuatorId = actuator.id;
    this.selectedStateId = actuator.device_StatusId;
    this.actuatorForm.patchValue(actuator);
  }

  updateActuator(): void {
    if (this.actuatorForm.invalid || !this.selectedActuatorId) return;
    
    const payload = { ...this.actuatorForm.value, device_StatusId: this.selectedStateId };

    this._deviceService.updateActuador(this.selectedActuatorId, payload).subscribe({
      next: () => {
        this._snackBar.open('Actuador actualizado ✅', 'Cerrar', { duration: 3000 });
        this.isEditMode = false;
        this.selectedActuatorId = null;
        this.selectedStateId = null;
        this.resetFormState();
        this.loadActuators();
      },
      error: () => this._snackBar.open('Error al actualizar', 'Cerrar', { duration: 3000 })
    });
  }

  toggleEnable(actuator: any): void {
    const newStatusId = actuator.device_StatusName === 'Deshabilitado'
      ? this.statusList.find(s => s.state_Name === 'Inactivo').id
      : this.statusList.find(s => s.state_Name === 'Deshabilitado').id;

    const payload = { ...actuator, device_StatusId: newStatusId };

    this._deviceService.updateActuador(actuator.id, payload).subscribe({
      next: () => {
        this._snackBar.open('Estado actualizado ✅', 'Cerrar', { duration: 3000 });
        this.loadActuators();
      },
      error: () => this._snackBar.open('Error al cambiar estado', 'Cerrar', { duration: 3000 })
    });
  }

  cancelEdit(): void {
    this.isEditMode = false;
    this.selectedActuatorId = null;
    this.selectedStateId = null;
    this.resetFormState();
  }
  private resetFormState(): void {
    this.actuatorForm.reset();
    this.actuatorForm.markAsPristine();
    this.actuatorForm.markAsUntouched();

    Object.keys(this.actuatorForm.controls).forEach(key => {
      const control = this.actuatorForm.get(key);
      control?.setErrors(null);
    });
  }

}
