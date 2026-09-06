import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MATERIAL_MODULES } from '../../shared/material';

@Component({
  selector: 'app-inicio',
  imports: [MATERIAL_MODULES],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss'
})
export class Inicio {

  constructor(private router: Router) { }

  irANuevaNaturaleza(): void {
    this.router.navigate(['/menu']);
  }

  irAEstacion(): void {
    this.router.navigate(['/estacion']);
  }
}
