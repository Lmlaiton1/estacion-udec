import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_MODULES } from '../../shared/material';
import { WeatherService } from '../../service/weather/weather-service';
import { LecturaMeteoPdfRequest } from '../../models/LecturaMeteoPdfRequest';

@Component({
  selector: 'app-estacion-reporte',
  standalone: true,
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './estacion-reporte.html',
  styleUrls: ['./estacion-reporte.scss']
})
export class EstacionReporte {
  readonly variables = [
    { key: '', label: 'Todas las variables' },
    { key: 'temperatura', label: 'Temperatura' },
    { key: 'humedad', label: 'Humedad' },
    { key: 'radiacion', label: 'Radiación' },
    { key: 'velocidadViento', label: 'Velocidad del viento' },
    { key: 'direccion', label: 'Dirección del viento' },
    { key: 'lluvia', label: 'Lluvia' },
  ];

  estacionNumero: number | null = null;
  variable = '';
  fechaInicio: Date | null = null;
  fechaFin: Date | null = null;
  filtrarPorHora = false;
  horaInicio = '08:00';
  horaFin = '18:00';

  generando = false;

  constructor(private _weatherService: WeatherService, private _snackBar: MatSnackBar) { }

  nombreEstacion(numero: number | null): string {
    return numero === null ? 'Todas las estaciones' : `Estación ${numero}`;
  }

  generarPdf(): void {
    if (!this.fechaInicio || !this.fechaFin) {
      this._snackBar.open('Elegí fecha de inicio y fecha de fin', 'Cerrar', { duration: 3000 });
      return;
    }

    if (this.fechaInicio > this.fechaFin) {
      this._snackBar.open('La fecha de inicio no puede ser posterior a la fecha de fin', 'Cerrar', { duration: 3000 });
      return;
    }

    const filtro: LecturaMeteoPdfRequest = {
      estacionNumero: this.estacionNumero,
      variable: this.variable || null,
      fechaInicio: this.fechaInicio.toISOString(),
      fechaFin: this.fechaFin.toISOString(),
      horaInicio: this.filtrarPorHora ? `${this.horaInicio}:00` : null,
      horaFin: this.filtrarPorHora ? `${this.horaFin}:00` : null,
    };

    this.generando = true;
    this._weatherService.getLecturasParaReportePdf(filtro).subscribe({
      next: (response) => {
        this.generando = false;
        const blob = new Blob([response.body!], { type: response.headers.get('Content-Type') ?? 'application/pdf' });

        let filename = 'Reporte_Meteo.pdf';
        const contentDisposition = response.headers.get('Content-Disposition');
        if (contentDisposition) {
          const match = contentDisposition.match(/filename="?([^"]+)"?/);
          if (match) filename = match[1];
        }

        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = filename;
        link.click();
        window.URL.revokeObjectURL(link.href);
      },
      error: () => {
        this.generando = false;
        this._snackBar.open('No se pudo generar el reporte', 'Cerrar', { duration: 4000 });
      }
    });
  }
}
