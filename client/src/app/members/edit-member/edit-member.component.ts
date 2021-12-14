import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs/operators';
import { Member } from 'src/app/_model/Member';
import { User } from 'src/app/_model/User';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-edit-member',
  templateUrl: './edit-member.component.html',
  styleUrls: ['./edit-member.component.css']
})
export class EditMemberComponent implements OnInit {
 member:Member;
 @ViewChild("editForm") EditForm:NgForm;
 user:User;
 @HostListener('window:beforeunload',['$event'])UnloadNotifaction($event:any){
      if(this.EditForm.dirty)
      {
        $event.returnValue=true;
      }
 }
  constructor(private AccountService:AccountService,private MemberService:MembersService, private ToasterServices:ToastrService)
  {
    this.AccountService.currentuser$.pipe(take(1)).subscribe(user=>{
      this.user=user;
    })
  }

  ngOnInit(): void {
    this.loadMember()
  }

   loadMember()
   {
     this.MemberService.getMember(this.user.username).subscribe(member=>{
       this.member=member;
     })
   }
   updateMember()
   {
    this.MemberService.updateMember(this.member).subscribe(()=>{
      this.ToasterServices.success("Your Success In Update Your Profile")
      this.EditForm.reset(this.member)
    })

   }
}
