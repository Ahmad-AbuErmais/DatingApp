import { Component, Input, OnInit, Output,EventEmitter } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
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
  constructor( private AcoountServices:AccountService,private ToasterServices:ToastrService) { }

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
  this.ToasterServices.error(error.error)
}
)
  }
  cancel()
    {
      this.CancelRegister.emit(false);
    }
}
