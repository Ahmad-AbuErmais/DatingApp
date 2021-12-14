import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
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
  constructor(public account:AccountService,private route:Router,private toastrServices:ToastrService ) { }

  ngOnInit() {
    // this.getCurrentUser();
  // this.CurrentUser$=this.account.currentuser$
  }
  login()
  {
     this.account.login(this.model).subscribe(response=>{
      console.log(response)
      this.route.navigateByUrl('/members')
      // this.loggedin=true
 }
 );
  }
  logout()
  {
    this.account.logout();
    this.route.navigateByUrl('/');
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
