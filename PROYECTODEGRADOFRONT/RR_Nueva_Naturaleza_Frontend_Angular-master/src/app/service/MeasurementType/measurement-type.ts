import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { IMeasurementType } from '../../models/IMeasurementType';

@Injectable({
  providedIn: 'root'
})
export class MeasurementType {
  private readonly apiUrl = environment.endPoint;
  constructor(private _http: HttpClient) { }

  public GetAllMeasurementTypes(): Observable<IMeasurementType[]> {
    return this._http.get<IMeasurementType[]>(this.apiUrl + "Api/ApiMeasurement_Type");
  }
}
