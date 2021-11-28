import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { User } from '../_model/User';
import { AccountService } from '../_services/account.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
model:any={}
// loggedin:boolean=false;
// CurrentUser$:Observable<User>;
  constructor(public account:AccountService) { }

  ngOnInit() {
    // this.getCurrentUser();
  // this.CurrentUser$=this.account.currentuser$
  }
  login()
  {
     this.account.login(this.model).subscribe(response=>{
      console.log(response)
      // this.loggedin=true
 }
 ,
 error=>{
   console.log(error)
 }
 );
  }
  logout()
  {
    this.account.logout();
    // this.loggedin=false;
  }
  // getCurrentUser()
  // {
  //   this.account.currentuser$.subscribe(user=>{
  //     this.loggedin=!!user
  //   },error=>{
  //     console.log(error)
  //   })
  // }

}
