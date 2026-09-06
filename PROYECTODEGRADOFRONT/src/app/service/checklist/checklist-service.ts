import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Checklist } from '../../models/Checklist';  // Crea esta interfaz si no la tienes

@Injectable({
  providedIn: 'root'
})
export class ChecklistService {
  private readonly baseUrl = `${environment.endPoint}Api/ApiChecklist`;

  constructor(private http: HttpClient) { }

  public addChecklist(data: Checklist): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}`, data);
  }

  public getChecklist(id: string): Observable<Checklist> {
    return this.http.get<Checklist>(`${this.baseUrl}/${id}`);
  }

  public getAllChecklists(): Observable<Checklist[]> {
    return this.http.get<Checklist[]>(`${this.baseUrl}/GetAllChecklists`);
  }

  public getChecklistsByDate(startDate: string, endDate: string): Observable<Checklist[]> {
    return this.http.get<Checklist[]>(`${this.baseUrl}/GetChecklistsByDate?startDate=${startDate}&endDate=${endDate}`);
  }

  public getChecklistsByUser(userId: string): Observable<Checklist[]> {
    return this.http.get<Checklist[]>(`${this.baseUrl}/GetChecklistsByUser?userId=${userId}`);
  }

  public getSensorMeasurements(deviceId: string, startDate: string, endDate: string): Observable<any[]> {
  return this.http.get<any[]>(`${this.baseUrl}/GetSensorMeasurements`, {
    params: {
      deviceId: deviceId,
      startDate: startDate,
      endDate: endDate
    }
  });
}

  descargarChecklistCSV(start: string, end: string, userId?: string): Observable<Blob> {
    const body = { startDate: start, endDate: end, userId: userId || null };
    return this.http.post(`${this.baseUrl}/DownloadChecklistCsv`, body, {
      responseType: 'blob'
    });
  }
}
