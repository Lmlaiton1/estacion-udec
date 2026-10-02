import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MATERIAL_MODULES } from '../../shared/material';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Rolservice } from '../../service/rol/rolservice';
import { Rol } from '../../models/Rol';
import { UserService } from '../../service/user/user-service';
import { UserCreate } from '../../models/UserCreate';
import { IUser } from '../../models/IUser';
import { UserUpdate } from '../../models/UserUpdate';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-user-form',
  imports: [MATERIAL_MODULES, ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './user-form.html',
  styleUrl: './user-form.scss'
})
export class UserForm implements OnInit{

  userForm!: FormGroup;
  roles: Rol[] = [];
  newUser!: UserCreate;

  usuarios: IUser[] = [];
  columnasUsuarios = ['nombre', 'cedula', 'rol', 'activo', 'acciones'];

  formEdicionVisible = false;
  usuarioEnEdicion: IUser | null = null;
  formEdicion: UserUpdate = this.edicionVacia();

  constructor(private fb: FormBuilder, private _rolService: Rolservice, private _userService: UserService,private _snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.userForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(2)]],
      apellido: ['', [Validators.required, Validators.minLength(2)]],
      cedula: ['', [Validators.required, Validators.pattern(/^\d{6,10}$/)]],
      pregunta: ['', Validators.required],
      respuesta: ['', Validators.required],
      rol: ['', Validators.required]
    });

    this.loadRoles();
    this.loadUsuarios();
  }

  loadRoles(): void {
    this._rolService.getRols().subscribe({
      next: (roles) => this.roles = roles,
      error: (err) => console.error('Error cargando roles:', err)
    });
  }

  loadUsuarios(): void {
    this._userService.GetAllUsers().subscribe({
      next: (usuarios) => this.usuarios = usuarios,
      error: (err) => console.error('Error cargando usuarios:', err)
    });
  }

  nombreRol(rolId: string): string {
    return this.roles.find(r => r.id === rolId)?.name ?? '—';
  }

  onSubmit(): void {
    if (this.userForm.invalid) return;

    this.newUser = {
      name: this.userForm.value.nombre,
      last_Name: this.userForm.value.apellido,
      id_Card: this.userForm.value.cedula,
      rolId: this.userForm.value.rol,
      question_Type: this.userForm.value.pregunta,
      answer: this.userForm.value.respuesta
    };

    this._userService.addUser(this.newUser).subscribe({
      next: () => {
        this._snackBar.open( 'Usuario creado con éxito', 'Cerrar', {
          duration: 3000,
        });
        this.userForm.reset();
        this.loadUsuarios();
      },
      error: (err) => this._snackBar.open(err, 'Cerrar', {
          duration: 3000,
        })
    });
  }

  private edicionVacia(): UserUpdate {
    return { id: '', name: '', last_Name: '', id_Card: '', activo: true, rol: '' };
  }

  editarUsuario(usuario: IUser): void {
    this.usuarioEnEdicion = usuario;
    this.formEdicion = {
      id: usuario.id,
      name: usuario.name,
      last_Name: usuario.last_Name,
      id_Card: usuario.id_Card,
      activo: usuario.activo,
      rol: this.nombreRol(usuario.rolId)
    };
    this.formEdicionVisible = true;
  }

  cancelarEdicion(): void {
    this.formEdicionVisible = false;
  }

  guardarEdicion(): void {
    this._userService.updateUser(this.formEdicion).subscribe({
      next: () => {
        this._snackBar.open('Usuario actualizado', 'Cerrar', { duration: 3000 });
        this.formEdicionVisible = false;
        this.loadUsuarios();
      },
      error: (err) => this.mostrarError(err, 'No se pudo actualizar el usuario')
    });
  }

  alternarActivo(usuario: IUser): void {
    const payload: UserUpdate = {
      id: usuario.id,
      name: usuario.name,
      last_Name: usuario.last_Name,
      id_Card: usuario.id_Card,
      activo: !usuario.activo,
      rol: this.nombreRol(usuario.rolId)
    };

    this._userService.updateUser(payload).subscribe({
      next: () => {
        usuario.activo = !usuario.activo;
        this._snackBar.open(usuario.activo ? 'Usuario activado' : 'Usuario desactivado', 'Cerrar', { duration: 3000 });
      },
      error: (err) => this.mostrarError(err, 'No se pudo cambiar el estado del usuario')
    });
  }

  eliminarUsuario(usuario: IUser): void {
    if (!confirm(`¿Eliminar al usuario "${usuario.name} ${usuario.last_Name}"? Esta acción no se puede deshacer.`)) return;

    this._userService.deleteUser(usuario.id).subscribe({
      next: () => {
        this._snackBar.open('Usuario eliminado', 'Cerrar', { duration: 3000 });
        this.loadUsuarios();
      },
      error: (err) => this.mostrarError(err, 'No se pudo eliminar el usuario')
    });
  }

  private mostrarError(err: any, mensajePorDefecto: string): void {
    const mensaje = typeof err.error === 'string' ? err.error : (err?.error?.errorMessage || mensajePorDefecto);
    this._snackBar.open(mensaje, 'Cerrar', { duration: 4000 });
  }

}
