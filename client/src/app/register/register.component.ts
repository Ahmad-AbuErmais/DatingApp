import { Component, Input, OnInit, Output,EventEmitter } from '@angular/core';
import { AccountService } from '../_services/account.service';


@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
model:any={}
// @Input() UsersFromHomeComponent:any;
@Output() CancelRegister=new EventEmitter();
  constructor( private AcoountServices:AccountService) { }

  ngOnInit(): void {
  }
  register()
  {
this.AcoountServices.register(this.model).subscribe(response=>{
  console.log(response)
  this.cancel();
},
error=>{
  console.log(error)
}
)
  }
  cancel()
    {
      this.CancelRegister.emit(false);
    }
}
