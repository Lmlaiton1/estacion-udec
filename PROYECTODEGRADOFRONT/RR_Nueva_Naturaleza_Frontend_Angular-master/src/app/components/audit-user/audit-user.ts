import { Component } from '@angular/core';
import { IAudit } from '../../models/IAudit';
import { AuditService } from '../../service/audit/audit-service';
import { UserService } from '../../service/user/user-service';
import { IUser } from '../../models/IUser';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';
import { ChangeDetectorRef } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-audit-user',
  imports: [MATERIAL_MODULES, CommonModule, ReactiveFormsModule],
  templateUrl: './audit-user.html',
  styleUrl: './audit-user.scss'
})
export class AuditUser {

  displayedColumns: string[] = ['date', 'device', 'action', 'observation'];

  // Usamos MatTableDataSource para que la paginación/filtrado funcione correctamente
  dataSource = new MatTableDataSource<IAudit>([]);

  // Como el paginator puede crearse sólo cuando showReport = true, lo dejamos nullable
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;

  constructor(
    public _auditService: AuditService,
    public _userService: UserService,
    private cdr: ChangeDetectorRef
  ) { }

  showReport: boolean = false;
  userId: string = '';
  startDate!: Date;
  endDate!: Date;
  users: IUser[] = [];
  userControl = new FormControl('');
  filteredUser: any[] = [];

  ngOnInit(): void {
    this._userService.GetAllUsers().subscribe({
      next: (data) => {
        this.users = data;
      },
      error: (err) => {
        console.error('Error cargando usuarios:', err);
      }
    });
    this.userControl.valueChanges.subscribe(value => {
      if (!value) {
        this.filteredUser = this.users;
        return;
      }

      // Si el valor es texto, filtra
      if (typeof value === 'string') {
        const filterValue = value.toLowerCase();
        this.filteredUser = this.users.filter(user =>
          user.name.toLowerCase().includes(filterValue) ||
          user.last_Name.toLowerCase().includes(filterValue)
        );
      } else {
        // Si el valor es un objeto (sensor seleccionado)
        this.filteredUser = this.users;
      }
    });
  }

  showAllUser() {
    this.filteredUser = this.users; // muestra todos las mediciones
  }

  displayUserName(userID: any): string {
    var selectedUser = this.users.find(s => s.id === userID) || null;

    return selectedUser?.last_Name && selectedUser.name
      ?  selectedUser.name + " " + selectedUser?.last_Name
      : '';
  }


  onSubmit() {
    const userId = this.userId;
    const fechaInicio = this.startDate;
    const fechafin = this.endDate;

    this._auditService.GetAuditByUserDate(userId, fechaInicio, fechafin)
      .subscribe({
        next: (Audits: any[]) => {
          const mappedAudits: IAudit[] = Audits.map(m => ({
            id: m.id,
            date: m.date,
            device: m.device?.device_Name ?? '',
            action: m.action,
            observation: m.observation 
          }));

          // 1) Actualizamos el dataSource
          this.dataSource.data = mappedAudits;
          
          this.showReport = true;
          this.cdr.detectChanges();

          if (this.paginator) {
            this.dataSource.paginator = this.paginator;
            this.paginator.pageSize = 15;
          }
        },
        error: (err) => {
          console.error('Error cargando auditorias:', err);
        }
      });
  }

  onPDF() {
    const userId = this.userId;
    const fechaInicio = this.startDate;
    const fechafin = this.endDate;

    this._auditService.GetAuditByUserDateForPdf(userId, fechaInicio, fechafin, '')
      .subscribe({
        next: (response: any) => {
          const blob = new Blob([response.body], { type: response.headers.get('Content-Type') });
          const contentDisposition = response.headers.get('Content-Disposition');
          let filename = 'ReporteAuditUser.pdf';
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
        error: async (err) => {
          if (err.error instanceof Blob) {
            const text = await err.error.text();
            console.error("Error detallado:", text);
          } else {
            console.error("Error al generar el PDF", err);
          }
        }
      });
  }
}
