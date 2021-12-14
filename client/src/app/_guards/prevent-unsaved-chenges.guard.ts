import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanDeactivate, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { EditMemberComponent } from '../members/edit-member/edit-member.component';

@Injectable({
  providedIn: 'root'
})
export class PreventUnsavedChengesGuard implements CanDeactivate<unknown> {
  canDeactivate(
    component: EditMemberComponent): boolean {
      if(component.EditForm.dirty)
      return confirm('Are You Sure You want to continue ? any Data Unsaved ewill be lost')
    return true;
  }

}
