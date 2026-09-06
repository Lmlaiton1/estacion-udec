import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuditCreate } from '../../models/AuditCreate';
import { IAudit } from '../../models/IAudit';
import { format } from 'date-fns';

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiAudit";

  constructor(private _http: HttpClient) { }

  public addAudit(audit: AuditCreate): Observable<AuditCreate> {
    return this._http.post<AuditCreate>(this.apiUrl + this.complement, audit);
  }

  public GetAuditByUserDate(userId: string, fechaInicio: Date, fechafin: Date): Observable<IAudit[]> {
    return this._http.get<IAudit[]>(this.apiUrl + `Api/ApiAudit/AuditByUserDate?userId=${userId}&startDate=${format(fechaInicio, "yyyy-MM-dd HH:mm:ss")}&endDate=${format(fechafin, "yyyy-MM-dd HH:mm:ss")}`);
  }

  public GetAuditByUserDateForPdf(userId: string, fechaInicio: Date, fechafin: Date, chartBase64: string) {
    const body = {
      userId: userId,
      startDate: fechaInicio.toISOString(),
      endDate: fechafin.toISOString(),
      chartImage: chartBase64
    };

    return this._http.post(
      this.apiUrl + "Api/ApiAudit/AuditByUserDateForPdf",
      body,
      {
        responseType: 'blob',
        observe: 'response' // para obtener también los encabezados
      }
    );
  }
}
