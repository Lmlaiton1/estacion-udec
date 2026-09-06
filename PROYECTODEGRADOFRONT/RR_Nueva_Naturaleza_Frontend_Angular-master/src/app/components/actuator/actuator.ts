import { Component, ViewChild, AfterViewInit } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { Actuator as ActuatorModel } from '../../models/Actuator';
import { CommonModule } from '@angular/common';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';
import { Dialog } from '../dialog/dialog';
import { MatDialog } from '@angular/material/dialog';
import { DeviceService } from '../../service/device/device-service';
import { AuditCreate } from '../../models/AuditCreate';
import { EventCreate } from '../../models/EventCreate';
import { ActuatorPost } from '../../models/ActuatorPost';
import { ActionActuator } from '../../models/ActionActuator';
import { AuditService } from '../../service/audit/audit-service';
import { EventService } from '../../service/event/event-service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActuatorMode } from '../../models/ActuatorMode';
import { ActuatorModeState } from '../../models/ActuatorModeState';
import { MatCheckboxChange } from '@angular/material/checkbox';
import { Authservice } from '../../service/auth/authservice';
import { Rolservice } from '../../service/rol/rolservice';
import { UserPost } from '../../models/UserPost';
import { Router } from '@angular/router';
import { StateService } from '../../service/state/state-service';
import { SdTransfer } from '../../service/sd/sd-transfer';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

type ActuatorWithMode = ActuatorModel & {
  mode?: ActuatorMode;
};

@Component({
  selector: 'app-actuator',
  imports: [MATERIAL_MODULES, CommonModule],
  templateUrl: './actuator.html',
  styleUrl: './actuator.scss'
})

export class Actuator implements AfterViewInit{
  actuators: ActuatorWithMode[] = [];
  dataSource = new MatTableDataSource<ActuatorWithMode>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  newAudit!: AuditCreate;
  state!: ActuatorModeState;
  newEvent!: EventCreate;
  updateState!: ActuatorPost;
  actionActuator!: ActionActuator;
  private refreshInterval: any;
  user!: UserPost | null;
  rolUsuario: string | null = null;
  dosificando: boolean = false; 
  sd: boolean = false; 

  constructor(public _authService: Authservice, public _RolService: Rolservice, private dialog: MatDialog, private _deviceService: DeviceService, private _auditService: AuditService, private _snackBar: MatSnackBar,
    private _eventService: EventService, private router: Router, private _StateService: StateService, private _SdTransfer : SdTransfer) {
    this.user = this._authService.getTokenUserInfo();
    if (this.user?.rolId) {
      this._RolService.getRol(this.user.rolId).subscribe(data => {
        this.rolUsuario = data.name;
      });
    }
  }

   ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }

  esAdministrador(): boolean {
    return this.rolUsuario === 'Administrador';
  }

  ngOnInit(): void {
    this._StateService.startConnection(); 
    this._SdTransfer.startConnection();
    this.loadActuators();
    // Auto-refresh cada 60 segundos (60000 ms)
    /*
    this.refreshInterval = setInterval(() => {
      this.loadActuators();
    }, 5000);*/
    // Simulación de datos obtenidos desde base de datos
    //this.actuators = [
    //  { id: '1', device_Name: 'Ventilador', systemId: 'S1', systemName: 'Sistema Clima', device_StatusId: '1', device_StatusName: 'Apagado' },
    //  { id: '2', device_Name: 'Bomba de agua', systemId: 'S1', systemName: 'Sistema Riego', device_StatusId: '1', device_StatusName: 'Apagado' },
    //  { id: '3', device_Name: 'Luz LED', systemId: 'S2', systemName: 'Sistema Iluminación', device_StatusId: '1', device_StatusName: 'Apagado' },
    //  { id: '4', device_Name: 'Calentador', systemId: 'S3', systemName: 'Sistema Temperatura', device_StatusId: '1', device_StatusName: 'Apagado' },
    //  { id: '5', device_Name: 'Extractor', systemId: 'S1', systemName: 'Sistema Clima', device_StatusId: '1', device_StatusName: 'Apagado' },
    //  { id: '6', device_Name: 'Alimentador', systemId: 'S4', systemName: 'Sistema Alimentación', device_StatusId: '1', device_StatusName: 'Apagado' }
    //];
    this._StateService.estadoCambio$.subscribe(data => {
      if (data) {
        console.log('📡 Cambio recibido desde backend:', data);
        this.procesarCambioTiempoReal(data.device, data.estado);
      }
    });

    this._SdTransfer.estado$.subscribe(estado => {
      if (!estado) return;

      if (!this.sd && estado === 'Iniciando') {
        this.sd = true; 
        this._snackBar.open('💾 Enviando datos desde la SD. Actuadores temporalmente deshabilitados.', 'Cerrar', {
          duration: 4000,
        });
      } 
      else if (this.sd && estado === 'Finalizado') {
        this.sd = false;
        this._snackBar.open('✅ Transferencia desde SD completada. Puedes usar los actuadores.', 'Cerrar', {
          duration: 3000,
        });
      }
    });
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

   goToActuatorForm() {
    this.router.navigate(['/menu/ActuatorForm']);
  }


  loadActuators(): void {
    this._deviceService.getActuators().subscribe({
      next: (devices) => {
        this._deviceService.getAll().subscribe({
          next: (modes) => {
            this.actuators = devices.map(d => {
              const mode = modes.find(m => m.deviceId === d.id);
              return { ...d, mode };
            });
            this.dataSource.data = this.actuators;
          },
          error: (err) => console.error('Error cargando modos:', err)
        });
      },
      error: (err) => {
        console.error('Error cargando actuadores:', err);
      }
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  trackById(index: number, item: ActuatorWithMode): string {
    return item.id;
  }

  toggleAutoMode(actuator: ActuatorWithMode, event: MatCheckboxChange): void {
    if (!actuator.mode) return;

    this.state = {
      id: actuator.id,
      isAutoMode: event.checked   
    };

    this._deviceService.update(this.state).subscribe({
      next: (updated) => {
        actuator.mode = updated;
        console.log('Modo automático actualizado', updated);
      },
      error: (err) => {
        console.error('Error actualizando modo automático', err);
        this._snackBar.open('Error al cambiar el modo automático', 'Cerrar', { duration: 3000 });
        // revertir si falla
        event.source.checked = actuator.mode?.isAutoMode ?? false;
      }
    });
  }

  format(name: string): string {
    if (name === 'Oxigeno') return 'Motor Oxigeno';
    if (name === 'Hidroponia') return 'Bomba Hidroponia';
    if (name === 'Sumidero') return 'Bomba Sumidero';
    if (name === 'Luces') return 'Luces Tanques';
    if (name === 'DosificadorTP2') return 'Dosificador TP2';
    if (name === 'DosificadorTP1') return 'Dosificador TP1';
    return name;
  }

  procesarCambioTiempoReal(device: string, estado: string): void {
    const esDosificador = device.toLowerCase().includes('dosificador');

    if (!this.dosificando && esDosificador && estado === 'Activo') {
      this.dosificando = true;
      this._snackBar.open('⚠️ Dosificación en curso. Los actuadores están deshabilitados temporalmente.', 'Cerrar', {
        duration: 4000,
      });

    } else if (this.dosificando && esDosificador && estado === 'Inactivo') {
      // Finalizó la dosificación
      this.dosificando = false;
      this._snackBar.open('✅ Dosificación finalizada. Puedes controlar los actuadores de nuevo.', 'Cerrar', {
        duration: 3000,
      });
    }

    // Siempre actualizamos el estado del actuador que cambió
    const actuator = this.actuators.find(a => a.device_Name === device);
    if (actuator) {
      actuator.device_StatusName = estado;
    }
  }

  reset(){
    this.actionActuator = {
        device_Name: "RESET",
        state: "Activo"
    }

    this._deviceService.turnOnOffActuator(this.actionActuator).subscribe({
        next: () => {
          this._snackBar.open("RESET HECHO","Cerrar",{
            duration: 3000,
          })
        },
        error: (err) => this._snackBar.open(err, 'Cerrar', {
          duration: 3000,
        })
    });
  }

  actuatorTrack(index: number, actuator: any) {
    return actuator.id && actuator.id !== '' ? actuator.id : index;
  }


  toggleActuator(actuator: ActuatorModel, event: MatSlideToggleChange): void {
    const currentState: 'Activo' | 'Inactivo' = actuator.device_StatusName === 'Activo' ? 'Activo' : 'Inactivo';
    const targetState: 'Activo' | 'Inactivo' = event.checked ? 'Activo' : 'Inactivo';
    console.log(this.actuators);

    // Abrir diálogo pidiendo motivo
    const dialogRef = this.dialog.open(Dialog, {
      width: '420px',
      data: {
        device_Name: actuator.device_Name,
        currentState,
        targetState
      }
    });

    // Opcional: deshabilitar el toggle mientras decide
    event.source.disabled = true;

    dialogRef.afterClosed().subscribe(async (result) => {
      // Rehabilitar toggle
      event.source.disabled = false;

      if (!result) {
        // Canceló: volver a estado visual anterior
        event.source.checked = currentState === 'Activo';
        return;
      }

      // Confirmó: actualizamos localmente
      actuator.device_StatusName = targetState;
      //actuator.device_StatusId = targetState === 'Activo' ? '2' : '1';

      const payload = {
        actuatorId: actuator.id,
        actuatorName: actuator.device_Name,
        newStatusName: actuator.device_StatusName,
        reasonType: result.reasonType, // 'Mantenimiento' | 'Pruebas' | 'Rutina' | 'Otra'
        reasonText: result.reasonText, // texto final
        timestamp: new Date().toISOString()
      };

      this.newAudit = {
        action: `Cambio estado actuador ${payload.actuatorName} a ${payload.newStatusName}`,
        observation: payload.reasonType === 'Otra' ? payload.reasonText : payload.reasonType,
        deviceId: payload.actuatorId
      }

      this.newEvent = {
        state: payload.newStatusName,
        deviceName: payload.actuatorName,
        deviceId: payload.actuatorId
      }

      this.updateState = {
        id: payload.actuatorId,
        device_State: payload.newStatusName
      }

      this.actionActuator = {
        device_Name: payload.actuatorName,
        state: payload.newStatusName
      }


      //Accionar actuador en el cultivo
      this._deviceService.turnOnOffActuator(this.actionActuator).subscribe({
        next: () => {
          //guardado de auditoria
          this._auditService.addAudit(this.newAudit).subscribe({
            next: () => {
              console.log('Auditoria guardada')
            },
            error: (err) => this._snackBar.open(err, 'Cerrar', {
              duration: 3000,
            })
          });

          //guardado de evento
          this._eventService.addEvent(this.newEvent).subscribe({
            next: () => {
              console.log('Evento guardado')
            },
            error: (err) => this._snackBar.open(err, 'Cerrar', {
              duration: 3000,
            })
          });

          //guardado del estado en la tabla dispositivos
          this._deviceService.updateState(this.updateState).subscribe({
            next: () => {
              console.log('Estado actualizado')
            },
            error: (err) => this._snackBar.open(err, 'Cerrar', {
              duration: 3000,
            })
          });
        },
        error: (err) => this._snackBar.open(err, 'Cerrar', {
          duration: 3000,
        })
      });

    });
  }
}



