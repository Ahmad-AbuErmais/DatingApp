import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
RegisertMode=false;
users:any;
  constructor(private http:HttpClient) { }

  ngOnInit(){
    // this.getUsers()
  }
  RegisterToggle()
  {
    this.RegisertMode=true
  }
  // getUsers()
  // {
  //   this.http.get('https://localhost:5001/api/users').subscribe(user=>this.users=user)
  // }

  CancelRegisterMode(event:boolean)
  {
    this.RegisertMode=event;
  }

}
