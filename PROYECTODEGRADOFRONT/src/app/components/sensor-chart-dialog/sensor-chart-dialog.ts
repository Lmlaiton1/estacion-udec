import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NgApexchartsModule, ApexAxisChartSeries, ApexChart, ApexXAxis, ApexYAxis, ApexTitleSubtitle } from "ng-apexcharts";
import { MATERIAL_MODULES } from '../../shared/material';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sensor-chart-dialog',
  standalone: true,
  imports: [NgApexchartsModule, MATERIAL_MODULES, CommonModule],
  templateUrl: './sensor-chart-dialog.html',
  styleUrls: ['./sensor-chart-dialog.scss']
})
export class SensorChartDialog {
  sensorId!: number;

  chartOptions!: {
    series: ApexAxisChartSeries;
    chart: ApexChart;
    xaxis: ApexXAxis;
    yaxis: ApexYAxis;
    title: ApexTitleSubtitle;
  };

  constructor(
    public dialogRef: MatDialogRef<SensorChartDialog>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.buildChart(data.measurements, data.deviceName, data.unit);
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

  private buildChart(measurements: any[], deviceName: string, unit: string) {
    const sorted = [...(measurements ?? [])].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
    );

    this.chartOptions = {
      series: [
        {
          name: deviceName,
          data: sorted.map((m: any) => m.value)
        }
      ],
      chart: {
        type: "line",
        height: 350,
        width: 650,
        toolbar: { show: true }
      },
      xaxis: {
        categories: sorted.map((m: any) => m.date),
        labels: { show: false },
        axisBorder: { show: false },
        axisTicks: { show: false }
      },
      yaxis: {
        title: {
          text: measurements[0]?.unit ?? '',
          rotate: -90,
          style: {
            fontSize: '14px',
            fontWeight: 'bold',
            color: '#333'
          }
        },
        labels: {
          minWidth: 30,
          formatter: (val) => val.toString()
        }
      },
      title: {
        text: `Últimas ${sorted.length} mediciones`,
        align: "center"
      }
    };
  }

  updateChart(measurements: any[]) {
    this.buildChart(measurements, this.data.deviceName, this.data.unit);
  }
}
