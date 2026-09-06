import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Schedule } from '../../models/Schedule';
import { environment } from '../../../environments/environment';
import { SerialPortConfig } from '../../models/SerialPortConfig';

@Injectable({
  providedIn: 'root'
})
export class ScheduleService {

  private readonly apiUrl = environment.endPoint;
  private complement = "Api/ApiSchedules";
  private complement2 = "Api/ApiSchedules/Ports";
  private complement3 = "Api/ApiSchedules/Config";
  private complement4 = "Api/ApiSchedules/ConfigSerial";
  
  constructor(private http: HttpClient) {}

  getAll(): Observable<Schedule[]> {
    return this.http.get<Schedule[]>(this.apiUrl + this.complement);
  }

  getByDeviceId(deviceId: string): Observable<Schedule[]> {
    return this.http.get<Schedule[]>(`${this.apiUrl + this.complement}/device/${deviceId}`);
  }

  create(schedule: Schedule): Observable<Schedule> {
    return this.http.post<Schedule>(this.apiUrl + this.complement, schedule);
  }

  update(id: string, schedule: Schedule): Observable<Schedule> {
    return this.http.put<Schedule>(`${this.apiUrl + this.complement}/${id}`, schedule);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl + this.complement}/${id}`);
  }

  getAvailablePorts(): Observable<string[]> {
    return this.http.get<string[]>(this.apiUrl + this.complement2);
  }

  getSerialConfig(): Observable<SerialPortConfig> {
    return this.http.get<SerialPortConfig>(this.apiUrl + this.complement3);
  }

  saveSerialConfig(serial: SerialPortConfig): Observable<any> {
    return this.http.post(this.apiUrl + this.complement4, serial);
  }
}
