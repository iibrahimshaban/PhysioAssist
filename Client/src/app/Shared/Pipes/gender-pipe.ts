import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'gender', standalone: true })
export class GenderPipe implements PipeTransform {
  transform(value: any): string {
    if (value === 0 || value === '0') return 'Male';
    if (value === 1 || value === '1') return 'Female';
    return value;
  }
}