import { Injectable, signal, WritableSignal } from '@angular/core';
import { PainPointDto } from '../models';

@Injectable({ providedIn: 'root' })
export class PainPointService {
  private readonly points: WritableSignal<PainPointDto[]> = signal([]);
  readonly painPoints = this.points.asReadonly();

  addPoint(point: PainPointDto): void {
    this.points.update(current => [...current, point]);
  }

  removePoint(index: number): void {
    this.points.update(current => current.filter((_, i) => i !== index));
  }

  updateIntensity(index: number, intensity: number): void {
    this.points.update(current =>
      current.map((p, i) => i === index ? { ...p, intensity } : p)
    );
  }

  updatePoint(index: number, point: PainPointDto): void {
    this.points.update(current =>
      current.map((p, i) => i === index ? { ...point } : p)
    );
  }

  clear(): void {
    this.points.set([]);
  }
}
