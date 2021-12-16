import { Component, Input, OnInit } from '@angular/core';
import { FileUploader } from 'ng2-file-upload';
import { take } from 'rxjs/operators';
import { Member } from 'src/app/_model/Member';
import { photo } from 'src/app/_model/photo';
import { User } from 'src/app/_model/User';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-photo-editor',
  templateUrl: './photo-editor.component.html',
  styleUrls: ['./photo-editor.component.css']
})
export class PhotoEditorComponent implements OnInit {
    @Input() member:Member
    uploader:FileUploader;
    hasBaseDropzoneOver=false;
    baseUrl=environment.ApiUrl;
user:User;
  constructor(private AccountSevice:AccountService,private MemberServices:MembersService)
  {
    this.AccountSevice.currentuser$.pipe(take(1)).subscribe(user=>this.user=user);
  }

  ngOnInit(): void {
    this.initializeUploder();
  }
  deletePhoto(photoId: number) {
    this.MemberServices.deletePhoto(photoId).subscribe(() => {
      this.member.photos = this.member.photos.filter(x => x.id !== photoId);
    })
  }
  setMAinPhoto(photo:photo)
  {
    this.MemberServices.SetMainPhoto(photo.id).subscribe(() => {
      this.user.photourl = photo.url;
      this.AccountSevice.SetCurrentUser(this.user);
      this.member.photoUrl = photo.url;
      this.member.photos.forEach(p => {
        if (p.isMain) p.isMain = false;
        if (p.id === photo.id) p.isMain = true;
      })
    })
  }
  fileOverBase(e:any)
  {
    this.hasBaseDropzoneOver=e;
  }
   initializeUploder()
   {
     this.uploader=new FileUploader({
       url:this.baseUrl+'users/add-photo',
       authToken:'Bearer '+this.user.token,
       isHTML5:true,
       allowedFileType:['image'],
       removeAfterUpload:true,
       autoUpload:false,
       maxFileSize:10*1024*1024
     })
     this.uploader.onAfterAddingFile=(file)=>{
       file.withCredentials=false;
     }
     this.uploader.onSuccessItem=(item,response,status,headers)=>{

      if(response)
      {
        const photo: photo = JSON.parse(response);
        this.member.photos.push(photo);
        if (photo.isMain) {
          this.user.photourl = photo.url;
          this.member.photoUrl = photo.url;
          this.AccountSevice.SetCurrentUser(this.user);
      }
     }
   }}

}
