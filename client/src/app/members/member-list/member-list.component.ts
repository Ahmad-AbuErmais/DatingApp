import { Component, OnInit } from '@angular/core';
import { Member } from 'src/app/_model/Member';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {
  members:Member[];
  constructor( private MemebersServices:MembersService) { }

  ngOnInit() {
    this.loadingMembers();
  }

  loadingMembers()
  {
     this.MemebersServices.getMembers().subscribe(member=>
      {
      this.members=member;

      })

  }
  }
