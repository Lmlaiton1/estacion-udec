import { Routes } from '@angular/router';
import { AuthGuard } from './security/auth.guard';
import { RoleGuard } from './security/role.guard';
import { Login } from './components/login/login';

// El resto de los componentes se carga con loadComponent (lazy): cada pantalla se descarga
// solo cuando se visita, en vez de meter toda la app (acuaponía + meteo) en el bundle inicial.
export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: 'forget-password',
    loadComponent: () => import('./components/forget-password/forget-password').then(m => m.ForgetPassword)
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'inicio',
    loadComponent: () => import('./components/inicio/inicio').then(m => m.Inicio),
    canActivate: [AuthGuard]
  },
  {
    path: 'Unauthorized',
    loadComponent: () => import('./components/unauthorized/unauthorized').then(m => m.Unauthorized)
  },
  { path: 'menu', redirectTo: 'menu/Actuator' },
  { path: 'audit-user', redirectTo: 'menu/audit-user' },

  {
    path: 'menu',
    loadComponent: () => import('./components/menu/menu').then(m => m.Menu),
    canActivate: [AuthGuard],
    children: [
      {
        path: 'Control',
        loadComponent: () => import('./components/control/control').then(m => m.Control),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'ActuatorForm',
        loadComponent: () => import('./components/actuator-form/actuator-form').then(m => m.ActuatorForm),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Actuator',
        loadComponent: () => import('./components/actuator/actuator').then(m => m.Actuator),
        canActivate: [AuthGuard]
      },
      {
        path: 'registerUser',
        loadComponent: () => import('./components/user-form/user-form').then(m => m.UserForm),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'ChecklistForm',
        loadComponent: () => import('./components/checklist-form/checklist-form').then(m => m.ChecklistFormComponent),
        canActivate: [AuthGuard]
      },
      {
        path: 'report',
        loadComponent: () => import('./components/report/report').then(m => m.Report),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'audit-user',
        loadComponent: () => import('./components/audit-user/audit-user').then(m => m.AuditUser),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Sensors',
        loadComponent: () => import('./components/sensors/sensors').then(m => m.Sensors),
        canActivate: [AuthGuard]
      },
      {
        path: 'Ia',
        loadComponent: () => import('./components/ia/ia').then(m => m.Ia),
        canActivate: [AuthGuard]
      },
      {
        path: 'SensorForm',
        loadComponent: () => import('./components/sensor-form/sensor-form').then(m => m.SensorForm),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
    ]
  },

  {
    path: 'estacion',
    loadComponent: () => import('./components/estacion-placeholder/estacion-placeholder').then(m => m.EstacionPlaceholder),
    canActivate: [AuthGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'Sensors' },
      {
        path: 'Sensors',
        loadComponent: () => import('./components/estacion-dashboard/estacion-dashboard').then(m => m.EstacionDashboard),
        canActivate: [AuthGuard]
      },
      {
        path: 'SensorForm',
        loadComponent: () => import('./components/estacion-catalogo/estacion-catalogo').then(m => m.EstacionCatalogo),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Umbrales',
        loadComponent: () => import('./components/estacion-umbrales/estacion-umbrales').then(m => m.EstacionUmbrales),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Auditoria',
        loadComponent: () => import('./components/estacion-auditoria/estacion-auditoria').then(m => m.EstacionAuditoria),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'registerUser',
        loadComponent: () => import('./components/user-form/user-form').then(m => m.UserForm),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Cultivo',
        loadComponent: () => import('./components/estacion-cultivo/estacion-cultivo').then(m => m.EstacionCultivo),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: ['Administrador', 'Operario'] }
      },
      {
        path: 'Ia',
        loadComponent: () => import('./components/estacion-ia/estacion-ia').then(m => m.EstacionIa),
        canActivate: [AuthGuard]
      },
      {
        path: 'report',
        loadComponent: () => import('./components/estacion-reporte/estacion-reporte').then(m => m.EstacionReporte),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: 'Administrador' }
      },
      {
        path: 'Estadistica',
        loadComponent: () => import('./components/estacion-estadistica/estacion-estadistica').then(m => m.EstacionEstadistica),
        canActivate: [AuthGuard, RoleGuard],
        data: { requiredRole: ['Administrador', 'Operario'] }
      },
    ]
  }
];
