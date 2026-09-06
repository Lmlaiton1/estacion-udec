import { Injectable } from "@angular/core";
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from "@angular/router";
import { Observable } from "rxjs";
import { Authservice } from "../service/auth/authservice";

@Injectable({providedIn: 'root'})
export class AuthGuard implements CanActivate{

    constructor(private router: Router, private _apiauth: Authservice){

    }

    canActivate(route: ActivatedRouteSnapshot){
     const user = this._apiauth.userData;
     if(user){
        return true;
     }
       this.router.navigate(['/login']);
       return false; 
    }
}