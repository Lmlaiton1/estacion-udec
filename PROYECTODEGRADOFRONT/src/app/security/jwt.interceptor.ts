import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Authservice } from "../service/auth/authservice";
import { Observable } from "rxjs";

@Injectable()
export class JwtInteceptor implements HttpInterceptor{

constructor(private _authService : Authservice){

}
intercept(request : HttpRequest<any>, next: HttpHandler) : Observable<HttpEvent<any>>{
   const user = this._authService.userData;

   if(user){
    request = request.clone({
        setHeaders: {
            Authorization: `Bearer ${user.token}`
        }
    })
   }

   return next.handle(request);
}
}