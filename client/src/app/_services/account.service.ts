import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import{map} from 'rxjs/operators'
import { environment } from 'src/environments/environment';
import { User } from '../_model/User';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
baseUrl=environment.ApiUrl;
private currentusersource=new ReplaySubject<User>(1);
currentuser$=this.currentusersource.asObservable();
  constructor(private http:HttpClient) { }
  login(model:any){

    return this.http.post(this.baseUrl+"AccountUser/login",model).pipe(
      map((response:User)=>{
        const user=response;
        if(user)
        {
//
          this.SetCurrentUser(user)
        }
      })
    );


  }
  register(model:any)
  {
    return this.http.post(this.baseUrl+"accountuser/register",model).pipe(
      map((user:User)=>{
        if(user)
        {
          // localStorage.setItem('user',JSON.stringify(user))
          // this.currentusersource.next(user);
          this.SetCurrentUser(user);
        }
      })
    )
  }
  SetCurrentUser(user:User){
    localStorage.setItem('user',JSON.stringify(user))
    this.currentusersource.next(user);
  }
  logout()
  {
    this.currentusersource.next(null);
    localStorage.removeItem('user')
  }
}
