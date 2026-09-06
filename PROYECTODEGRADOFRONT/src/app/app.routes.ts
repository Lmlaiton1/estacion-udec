import { Routes } from '@angular/router';
import { AuthGuard } from './security/auth.guard';
import { RoleGuard } from './security/role.guard';
import { Login } from './components/login/login';
import { Inicio } from './components/inicio/inicio';
import { EstacionPlaceholder } from './components/estacion-placeholder/estacion-placeholder';
import { Unauthorized } from './components/unauthorized/unauthorized';
import { Actuator } from './components/actuator/actuator';
import { Menu } from './components/menu/menu';
import { UserForm } from './components/user-form/user-form';
import { Sensors } from './components/sensors/sensors';
import { Report as ReportComponent } from './components/report/report';
import { ForgetPassword } from './components/forget-password/forget-password';
import { AuditUser } from './components/audit-user/audit-user';
import { Control } from './components/control/control';
import { ActuatorForm } from './components/actuator-form/actuator-form';
import { SensorForm } from './components/sensor-form/sensor-form';
import { ChecklistFormComponent } from './components/checklist-form/checklist-form';
import { Ia } from './components/ia/ia';


export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'forget-password', component: ForgetPassword },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'inicio', component: Inicio, canActivate: [AuthGuard] },
  { path: 'estacion', component: EstacionPlaceholder, canActivate: [AuthGuard] },
  { path: 'Unauthorized', component: Unauthorized },
  { path: 'menu', redirectTo: 'menu/Actuator' },
  { path: 'audit-user', redirectTo: 'menu/audit-user' },

  {
    path: 'menu',
    component: Menu,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'Control',
        component: Control,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'ActuatorForm',
        component: ActuatorForm,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Actuator',
        component: Actuator,
        canActivate: [AuthGuard]
      },
      {
        path: 'registerUser',
        component: UserForm,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'ChecklistForm',
        component: ChecklistFormComponent,
        canActivate: [AuthGuard]
      },
      {
        path: 'report',
        component: ReportComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'audit-user',
        component: AuditUser,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Sensors',
        component: Sensors,
        canActivate: [AuthGuard]
      },
      {
        path: 'Ia',
        component: Ia,
        canActivate: [AuthGuard]
      },
      {
        path: 'SensorForm',
        component: SensorForm,
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
    ]
  }
];
