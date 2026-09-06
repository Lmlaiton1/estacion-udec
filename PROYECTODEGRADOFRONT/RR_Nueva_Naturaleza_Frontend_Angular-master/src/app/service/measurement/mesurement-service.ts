import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { IMeasurement } from '../../models/IMeasurement';
import { Observable } from 'rxjs';
import { format } from 'date-fns';

@Injectable({
  providedIn: 'root'
})
export class MeasurementService {
  private readonly apiUrl = environment.endPoint;
  constructor(private _http: HttpClient) { }

  public GetMeasurementsByDate(measurementTypeId: string, fechaInicio: Date, fechafin: Date): Observable<IMeasurement[]> {
    return this._http.get<IMeasurement[]>(this.apiUrl + `Api/ApiMeasurement/MeasurementByDate?measurementTypeId=${measurementTypeId}&startDate=${format(fechaInicio, "yyyy-MM-dd HH:mm:ss")}&endDate=${format(fechafin, "yyyy-MM-dd HH:mm:ss")}`);
  }

  public GetMeasurementsForPdf(measurementTypeId: string, fechaInicio: Date, fechafin: Date, chartBase64: string) {
    const body = {
      measurementTypeId: measurementTypeId,
      startDate: fechaInicio.toISOString(),
      endDate: fechafin.toISOString(),
      chartImage: chartBase64
    };

    return this._http.post(
      this.apiUrl + "Api/ApiMeasurement/MeasurementForPdf",
      body,
      {
        responseType: 'blob',
        observe: 'response' // para obtener también los encabezados
      }
    );
  }

  GenerateAllMeasurementsCsv(start: string, end: string, userId?: string): Observable<Blob> {
    const body = { startDate: start, endDate: end, userId: userId || null };
    return this._http.post(this.apiUrl + "Api/ApiMeasurement/GenerateAllMeasurementsCsv", body, {
      responseType: 'blob'
    });
  }
}
