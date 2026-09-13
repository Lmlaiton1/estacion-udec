import { Component, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { CommonModule } from '@angular/common';
import { DeviceService } from '../../service/device/device-service';
import { MatDialog } from '@angular/material/dialog';
import { SensorChartDialog } from '../sensor-chart-dialog/sensor-chart-dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { UserPost } from '../../models/UserPost';
import { Rolservice } from '../../service/rol/rolservice';
import { Authservice } from '../../service/auth/authservice';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-sensors',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './sensors.html',
  styleUrls: ['./sensors.scss']
})
export class Sensors implements OnDestroy, AfterViewInit {
  sensors: any[] = [];
  dataSource = new MatTableDataSource<any>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private refreshInterval: any;
  user!: UserPost | null;
  rolUsuario: string | null = null;

  constructor(private _deviceService: DeviceService, private dialog: MatDialog, private router: Router, private route: ActivatedRoute, public _authService: Authservice, public _RolService: Rolservice) {
    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._RolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
      });
    }
   }

  ngOnInit() {
    this.loadSensors();
    this.refreshInterval = setInterval(() => {
      this.loadSensors();
    }, 60000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }

  esAdministrador(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  loadSensors(): void {
    this._deviceService.getMeasurements().subscribe({
      next: (data) => {
        const rawSensors = Array.isArray(data) ? data : [];

        this.sensors = rawSensors.map((s: any) => {
          const measurements = Array.isArray(s.measurements) ? s.measurements.slice() : [];//.slice sirve para copiar el arreglo original

          measurements.sort((a: any, b: any) => {
            const ta = new Date(a.date).getTime() || 0;
            const tb = new Date(b.date).getTime() || 0;
            return ta - tb;
          });//ordenar las fechas de las mas antigua a las reciente, la mas reciente queda en la ultima posicion.

          const lastMeasurement = measurements.length ? measurements[measurements.length - 1] : null;

          return {
            ...s,//devolver los mismos datos con que arranco el objeto
            measurements,
            lastMeasurement
          };
        });
        this.dataSource.data = this.sensors;

        const openDialogs = this.dialog.openDialogs;
        const chartDialog = openDialogs.find(d => d.componentInstance instanceof SensorChartDialog);
        if (chartDialog) {
          const sensorInDialog = this.sensors.find(s => s.deviceId === chartDialog.componentInstance.sensorId);
          if (sensorInDialog) {
            chartDialog.componentInstance.updateChart(sensorInDialog.measurements);
          }
        }
      },
      error: (err) => {
        console.error('Error cargando sensores:', err);
      }
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  sensorTrack(index: number, sensor: any) {
    return sensor.id && sensor.id !== '' ? sensor.id : index;
  }

  trackById(index: number, item: any): string {
    return item.id;
  }

  formatValue(value: number): string{
     if(value == 1) return '< 75';

     return '>= 75';
  }

  format(unit: string): string {
    if (unit === 'PorcentajeH') return '%';
    if (unit === 'PorcentajeN') return '%';
    if (unit === 'Centigrados') return '°C';
    return unit;
  }

  goToSensorForm() {
    this.router.navigate(['../SensorForm'], { relativeTo: this.route });
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

  getMeasurementStatus(sensor: any): 'normal' | 'warning' | 'critical' {
    if (!sensor?.lastMeasurement?.date) return 'critical';

    const lastDate = new Date(sensor.lastMeasurement.date).getTime();
    const now = Date.now();
    const diffMs = now - lastDate;//milisegundos
    const diffHours = diffMs / (1000 * 60 * 60);

    if (diffHours <= 1) {
      return 'normal';
    } else if (diffHours > 1 && diffHours <= 24) {
      return 'warning';
    } else {
      return 'critical';
    }
  }


  openChart(sensor: any) {
    const dialogRef = this.dialog.open(SensorChartDialog, {
      width: '52vw',  // ancho dinámico en porcentaje de la ventana
      maxWidth: '90vw',
      data: {
        deviceName: sensor.deviceName,
        measurements: sensor.measurements
      }
    });

    dialogRef.componentInstance.sensorId = sensor.deviceId;
  }
}
