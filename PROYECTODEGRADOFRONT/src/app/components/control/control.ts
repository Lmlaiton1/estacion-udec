import { Component, OnInit } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { CommonModule } from '@angular/common';
import { DeviceService } from '../../service/device/device-service';
import { FormBuilder, FormGroup, FormArray, Validators, FormControl } from '@angular/forms';
import { ControlRuleResponse } from '../../models/ControlRuleResponse';
import { UpdateControlRuleRequest } from '../../models/UpdateControlRuleRequest';
import { Actuator } from '../../models/Actuator';
import { ReactiveFormsModule } from '@angular/forms';
import { Sensor } from '../../models/Sensor';
import { ScheduleService } from '../../service/schedule/schedule-service';
import { Schedule } from '../../models/Schedule';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-control',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule, ReactiveFormsModule],
  templateUrl: './control.html',
  styleUrl: './control.scss'
})
export class Control implements OnInit {

  sensors: any[] = [];                  
  actuators: Actuator[] = [];           
  selectedSensorId: string | null = null;
  selectedSensor: Sensor | null = null;
  selectedSensor2: Sensor | null = null;

  rules: ControlRuleResponse[] = [];    
  form: FormGroup;   

  schedules: Schedule[] = [];
  scheduleForm!: FormGroup;
  dosificadores: Actuator[] = [];
  editingScheduleId: string | null = null;
  
  sensorControl = new FormControl('');
  filteredSensors: any[] = [];
  filteredActuators: any[] = [];


  constructor(
    private _deviceService: DeviceService,
    private fb: FormBuilder, private _scheduleService: ScheduleService, private _snackBar: MatSnackBar,
  ) {
    this.form = this.fb.group({
      rules: this.fb.array([])   // aquí está el FormArray
    });

    this.scheduleForm = this.fb.group({
      deviceId: ['', Validators.required],
      hour: ['', [Validators.required, Validators.min(0), Validators.max(23)]],
      minute: ['', [Validators.required, Validators.min(0), Validators.max(59)]],
      durationSeconds: ['', [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    // 1. Traer sensores
    this._deviceService.getSensors().subscribe({
      next: (data) => this.sensors = data,
      error: (err) => this._snackBar.open('Error cargando sensores', 'Cerrar', { duration: 3000 })
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
    });

    // 2. Traer actuadores
    this._deviceService.getActuators().subscribe({
      next: (data) => {
        this.actuators = data;
        // Filtrar solo dosificadores
        this.dosificadores = this.actuators.filter(a =>
          a.device_Name.toLowerCase().includes('dosificador')
        );
      },
      error: (err) => this._snackBar.open('Error cargando actuadores', 'Cerrar', { duration: 3000 })
    });
    this.filteredActuators = this.actuators;
    this.loadSchedules();
  }

  filterSensors(value: string) {
    const filterValue = value.toLowerCase();
    return this.sensors.filter(sensor =>
      sensor.device_Name.toLowerCase().includes(filterValue)
    );
  }

  filterActuators(event: Event) {
    const input = event.target as HTMLInputElement;
    const term = input.value.toLowerCase().trim();

    if (!term) {
      // Si el campo está vacío → mostrar todos
      this.filteredActuators = [...this.actuators];
    } else {
      // Filtra normalmente
      this.filteredActuators = this.actuators.filter(a =>
        a.device_Name.toLowerCase().includes(term)
      );
    }
  }

 displaySensorName(sensor: any): string {
    return sensor && sensor.device_Name
      ? this.formatNameSensor(sensor.device_Name)
      : '';
  }
  showAllSensors() {
    this.filteredSensors = this.sensors; // muestra todos los sensores
  }
  onSelectSensor(sensor: any): void {
    this.selectedSensorId = sensor.id; 
    this.selectedSensor = sensor;

    this._deviceService.getByDevice(sensor.id).subscribe({
      next: (data: ControlRuleResponse[]) => {
        this.rules = data;
        this.loadRulesIntoForm();
        this.filteredActuators = this.actuators;
      },
      error: () =>
        this._snackBar.open('Error cargando reglas', 'Cerrar', { duration: 3000 }),
    });
  }
 private loadRulesIntoForm(): void {
    this.rulesForm.clear();
    this.rules.forEach(rule => {
      this.rulesForm.push(this.fb.group({
        id: [rule.id],
        min: [rule.min, Validators.required],
        max: [rule.max, Validators.required],
        actuatorIds: [rule.actuators.map(a => a.id)], 
        triggerTypes: this.fb.group(              
          Object.fromEntries(
            this.actuators.map(act => [
              act.id,
              rule.actuators.find(a => a.id === act.id)?.triggerType || 'Exceso'
            ])
          )
        )
      }));
    });
  }

  format(unit: string): string {
    if (unit === 'PorcentajeH' || unit === 'PorcentajeN') return '%';
    if (unit === 'Centigrados') return '°C';
    return unit;
  }

  getActuatorsFormArray(ruleIndex: number): FormArray {
    return this.rulesForm.at(ruleIndex).get('actuators') as FormArray;
  }

  updateRule(index: number): void {
    const ruleForm = this.rulesForm.at(index) as FormGroup;
    const selectedIds = ruleForm.get('actuatorIds')?.value || [];
    const triggerTypes = ruleForm.get('triggerTypes')?.value || {};
    const min = ruleForm.get('min')?.value;
    const max = ruleForm.get('max')?.value;

    if (min > max || max < min){
       
      this._snackBar.open('❌ Error actualizando regla (Valores inversos o erroneos)', 'Cerrar', { duration: 3000 });

    }else{
      const dto: UpdateControlRuleRequest = {
        min: ruleForm.get('min')?.value,
        max: ruleForm.get('max')?.value,
        actuators: selectedIds.map((id: string) => ({
          id: id,
          triggerType: triggerTypes[id] || 'Exceso'
        }))
      };

      const ruleId = ruleForm.get('id')?.value;

      this._deviceService.updateRule(ruleId, dto).subscribe({
        next: () => this._snackBar.open('✅ Regla actualizada con éxito', 'Cerrar', { duration: 3000 }),
        error: (err) => this._snackBar.open('❌ Error actualizando regla', 'Cerrar', { duration: 3000 })
      });
    }
  

    
  }

  get rulesForm(): FormArray<FormGroup> {
    return this.form.get('rules') as FormArray<FormGroup>;
  }

  trackByRuleId(index: number, group: FormGroup): any {
    return group.get('id')?.value;
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

  loadSchedules(): void {
    this._scheduleService.getAll().subscribe({
      next: (data) => this.schedules = data,
      error: (err) => console.error('Error cargando horarios:', err)
    });
  }

  editSchedule(schedule: Schedule): void {
    this.editingScheduleId = schedule.id!;
    this.scheduleForm.patchValue({
      deviceId: schedule.deviceId,
      hour: schedule.hour,
      minute: schedule.minute,
      durationSeconds: schedule.durationSeconds
    });
  }

  saveSchedule(): void {
    const updatedSchedule = this.scheduleForm.value;

    if (this.editingScheduleId) {

      this._scheduleService.update(this.editingScheduleId, updatedSchedule).subscribe({
        next: () => {
          this._snackBar.open('✅ Horario actualizado', 'Cerrar', { duration: 3000 });
          this.editingScheduleId = null;
          this.scheduleForm.reset();
          this.loadSchedules();
        },
        error: (err) => {
          this._snackBar.open('❌ Error al actualizar horario', 'Cerrar', { duration: 3000 });
          console.error(err);
        }
      });
    } else {
      // Modo crear
      this._scheduleService.create(updatedSchedule).subscribe({
        next: () => {
          this._snackBar.open('✅ Horario creado', 'Cerrar', { duration: 3000 });
          this.scheduleForm.reset();
          this.loadSchedules();
        },
        error: (err) => {
          this._snackBar.open('❌ Error crear horario', 'Cerrar', { duration: 3000 });
          console.error(err);
        }
      });
    }
  }

  deleteSchedule(id: string): void {
    this._scheduleService.delete(id).subscribe({
      next: () => {
        this._snackBar.open('🗑️ Horario eliminado', 'Cerrar', { duration: 3000 });
        this.loadSchedules();
      },
      error: (err) => {
          this._snackBar.open('❌ Error crear horario', 'Cerrar', { duration: 3000 });
          console.error(err);
      }
    });
  }
}
