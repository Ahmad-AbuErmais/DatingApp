import { Injectable } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root'
})
export class BusySpinnerService {
 busyspinnerCount=0;
  constructor(private SpinnerService:NgxSpinnerService)
  { }
  busy()
  {
    this.busyspinnerCount++;
    this.SpinnerService.show(undefined,{
      type:"line-scale-party",
      bdColor:'rgba(255,255,255,0)',
      color:'#333333'
    })
  }
  idle()
  {
    this.busyspinnerCount--;
    if(this.busyspinnerCount<=0)
    {
      this.busyspinnerCount=0;
      this.SpinnerService.hide();
    }
  }
}
