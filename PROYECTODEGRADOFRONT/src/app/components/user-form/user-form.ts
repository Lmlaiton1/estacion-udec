import { Component, OnInit } from '@angular/core';
import { MATERIAL_MODULES } from '../../shared/material';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Rolservice } from '../../service/rol/rolservice';
import { Rol } from '../../models/Rol';
import { UserService } from '../../service/user/user-service';
import { UserCreate } from '../../models/UserCreate';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-user-form',
  imports: [MATERIAL_MODULES, ReactiveFormsModule],
  templateUrl: './user-form.html',
  styleUrl: './user-form.scss'
})
export class UserForm implements OnInit{

  userForm!: FormGroup;
  roles: Rol[] = [];
  newUser!: UserCreate;

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
  }

  loadRoles(): void {
    this._rolService.getRols().subscribe({
      next: (roles) => this.roles = roles,
      error: (err) => console.error('Error cargando roles:', err)
    });
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
        //console.log('Usuario creado con éxito');
        this._snackBar.open( 'Usuario creado con éxito', 'Cerrar', {
          duration: 3000, 
        });
        this.userForm.reset();
      },
      error: (err) => this._snackBar.open(err, 'Cerrar', {
          duration: 3000, 
        })
    });
  }

}
