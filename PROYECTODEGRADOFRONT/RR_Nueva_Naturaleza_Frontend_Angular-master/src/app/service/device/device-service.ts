import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Actuator } from '../../models/Actuator';
import { ActuatorPost } from '../../models/ActuatorPost';
import { ActionActuator } from '../../models/ActionActuator';
import { SensorMeasurement } from '../../models/SensorMeasurement';
import { ActuatorMode } from '../../models/ActuatorMode';
import { ActuatorModeState } from '../../models/ActuatorModeState';
import { UpdateControlRuleRequest } from '../../models/UpdateControlRuleRequest';
import { ControlRuleResponse } from '../../models/ControlRuleResponse';
import { Sensor } from '../../models/Sensor';
import { ActuatorAll } from '../../models/ActuatorAll';
import { States } from '../../models/States';
import { Systems } from '../../models/Systems';
import { ActuatorCreatePost } from '../../models/ActuatorCreatePost';
import { ActuatorUpdate } from '../../models/ActuatorUpdate';
import { SensorAll } from '../../models/SensorAll';
import { Unit } from '../../models/unit';
import { SensorCreatePost } from '../../models/SensorCreatePost';
import { SensorUpdate } from '../../models/SensorUpdate';
import { CreateUnit } from '../../models/CreateUnit';

@Injectable({
  providedIn: 'root'
})
export class DeviceService {
    private readonly apiUrl = environment.endPoint;
    private complement = "Api/ApiDevice";
    private complement1 = "Api/ApiDevice/GetActuators";
    private complement2 = "Api/ApiDevice/ActionActuator";
    private complement3 = "Api/ApiDevice/LastMeasurements";
    private complement4 = "Api/ApiDevice/GetModes";
    private complement5 = "Api/ApiDevice/UpdateMode";
    private complement6 = "Api/ApiDevice/By-device";
    private complement7 = "Api/ApiDevice/Update";
    private complement8 = "Api/ApiDevice/GetSensors";
    private complement9 = "Api/ApiDevice/GetAllActuators";
    private complement10 = "Api/ApiDevice/GetStates";
    private complement11 = "Api/ApiDevice/GetSystems";
    private complement12 = "Api/ApiDevice/CreateActuator";
    private complement13 = "Api/ApiDevice/UpdateActuador";
    private complement14 = "Api/ApiDevice/GetAllSensors";
    private complement15 = "Api/ApiDevice/GetUnits";
    private complement16 = "Api/ApiDevice/CreateSensor";
    private complement17 = "Api/ApiDevice/UpdateSensor";
    private complement18 = "Api/ApiDevice/CreateUnit";

    constructor(private _http: HttpClient) { }

    public getByDevice(deviceId: string): Observable<ControlRuleResponse[]> {
      return this._http.get<ControlRuleResponse[]>(`${this.apiUrl + this.complement6}/${deviceId}`);
    }

    public updateRule(ruleId: string, request: UpdateControlRuleRequest): Observable<ControlRuleResponse> {
      return this._http.put<ControlRuleResponse>(`${this.apiUrl + this.complement7}/${ruleId}`, request);
    }

    public updateActuador(actuatorId: string, request: ActuatorUpdate): Observable<ActuatorUpdate> {
      return this._http.put<ActuatorUpdate>(`${this.apiUrl + this.complement13}/${actuatorId}`, request);
    }
    public updateSensor(sensorId: string, request: SensorUpdate): Observable<SensorUpdate> {
      return this._http.put<SensorUpdate>(`${this.apiUrl + this.complement17}/${sensorId}`, request);
    }
  
    public getActuators(): Observable<Actuator[]> {
      return this._http.get<Actuator[]>(this.apiUrl + this.complement1);
    }

    public GetAllActuators(): Observable<ActuatorAll[]> {
      return this._http.get<ActuatorAll[]>(this.apiUrl + this.complement9);
    }

    public GetAllSensors(): Observable<SensorAll[]> {
      return this._http.get<SensorAll[]>(this.apiUrl + this.complement14);
    }

    public GetUnits(): Observable<Unit[]> {
      return this._http.get<Unit[]>(this.apiUrl + this.complement15);
    }

    public GetStates(): Observable<States[]> {
      return this._http.get<States[]>(this.apiUrl + this.complement10);
    }

    public GetSystems(): Observable<Systems[]> {
      return this._http.get<Systems[]>(this.apiUrl + this.complement11);
    }

    public getSensors(): Observable<Sensor[]> {
      return this._http.get<Sensor[]>(this.apiUrl + this.complement8);
    }

    public getAll(): Observable<ActuatorMode[]> {
      return this._http.get<ActuatorMode[]>(this.apiUrl + this.complement4);
    }

    public update(state: ActuatorModeState): Observable<ActuatorMode> {
      return this._http.put<ActuatorMode>(this.apiUrl + this.complement5, state);
    }

    public createActuator(actuator: ActuatorCreatePost): Observable<ActuatorMode> {
      return this._http.put<ActuatorMode>(this.apiUrl + this.complement12, actuator);
    }

    public createUnit(unit: CreateUnit): Observable<CreateUnit> {
      return this._http.put<CreateUnit>(this.apiUrl + this.complement18, unit);
    }

    public createSensor(actuator: SensorCreatePost): Observable<SensorCreatePost> {
      return this._http.put<SensorCreatePost>(this.apiUrl + this.complement16, actuator);
    }

    public getMeasurements(): Observable<SensorMeasurement[]> {
      return this._http.get<SensorMeasurement[]>(this.apiUrl + this.complement3);
    }
    
    public updateState(actuator : ActuatorPost): Observable<ActuatorPost> {
      return this._http.put<ActuatorPost>(this.apiUrl + this.complement, actuator);
    } 

    public turnOnOffActuator(actuator : ActionActuator): Observable<ActionActuator> {
      return this._http.put<ActionActuator>(this.apiUrl + this.complement2, actuator);
    } 
}