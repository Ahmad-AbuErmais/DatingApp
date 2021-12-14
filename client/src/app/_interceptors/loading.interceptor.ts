import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { BusySpinnerService } from '../_services/busy-spinner.service';
import { delay, finalize } from 'rxjs/operators';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {

  constructor( private busyspinner:BusySpinnerService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    this.busyspinner.busy();

    return next.handle(request).pipe(
    delay(1000),
    finalize(()=>{
      this.busyspinner.idle()
    })
    );
  }
}
