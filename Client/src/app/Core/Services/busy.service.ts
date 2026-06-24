import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class BusyService {
  busyCount:number = 0;
  loading = signal<boolean>(false);
  busy(){
    this.busyCount++;
    this.loading.set(true);
  }

  idle(){
    this.busyCount--;
    if (this.busyCount <= 0){
      this.busyCount = 0;
      this.loading.set(false);
    }
  }
}
