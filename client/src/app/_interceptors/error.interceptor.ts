import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { NavigationExtras, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError } from 'rxjs/operators';
import { ThrowStmt } from '@angular/compiler';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor( private router :Router,private Toaster:ToastrService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError(error =>{
        if(error)
        {
          switch(error.status)
          {
            case 400:
              if(error.error.errors){
                const modelStatesError=[];
                for(const key in error.error.errors)
                {
                  if(error.error.errors[key])
                  {modelStatesError.push(error.error.errors[key])}
                }
                throw modelStatesError.flat();
              }
              else{
                this.Toaster.error(error.statusText,error.status)
              }

              break;
              case 401:
                this.Toaster.error(error.statusText,error.status)
                break;
               case 404:
                 this.router.navigateByUrl('/not-found')
                 break;
                case 500:
                  const NaviagteExtres:NavigationExtras={state:{error:error.error}}
                  this.router.navigateByUrl('/server-error',NaviagteExtres)
                  break;
                default:
                  this.Toaster.error("something Wrong Went Happend")
                  console.log(error)
                  break;
          }

        }
       return throwError(error)
      })
    );
  }
}
