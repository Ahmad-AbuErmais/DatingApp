import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Member } from 'src/app/_model/Member';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {
  members$:Observable<Member[]>;
  constructor( private MemebersServices:MembersService) { }

  ngOnInit() {
    // this.loadingMembers();
    this.members$=this.MemebersServices.getMembers();
  }

  // loadingMembers()
  // {
  //    this.MemebersServices.getMembers().subscribe(member=>
  //     {
  //     this.members=member;

  //     })

  // }
  }
