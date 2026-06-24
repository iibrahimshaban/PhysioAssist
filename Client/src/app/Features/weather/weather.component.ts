import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { environment } from '../../../environments/environment.development';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-weather',
  imports: [DatePipe],
  templateUrl: './weather.component.html',
  styleUrl: './weather.component.css',
})

export class WeatherComponent implements OnInit {
  private http = inject(HttpClient);
 
  forecasts = signal<WeatherForecast[]>([]);
 
  ngOnInit() {
    this.http.get<WeatherForecast[]>(environment.apiUrl + 'weatherforecast').subscribe({
      next: data => this.forecasts.set(data),
      error: err => console.error(err)
    });
  }
 
  getSummaryClass(summary: string): string {
    const map: Record<string, string> = {
      Freezing: 'freezing', Bracing: 'bracing', Chilly: 'chilly',
      Cool: 'cool', Mild: 'mild', Warm: 'warm',
      Balmy: 'balmy', Hot: 'hot', Sweltering: 'sweltering', Scorching: 'scorching'
    };
    return map[summary] ?? '';
  }
}
