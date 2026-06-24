import { Routes } from '@angular/router';
import { NotFoundComponent } from './Shared/Components/not-found/not-found.component';
import { ServerErrorComponent } from './Shared/Components/server-error/server-error.component';
import { TestErrorComponent } from './Features/test-error/test-error.component';
import { WeatherComponent } from './Features/weather/weather.component';

export const routes: Routes = [
    {path: 'weather',component:WeatherComponent},
    {path: 'test-error',component: TestErrorComponent},
    {path: 'not-found',component: NotFoundComponent},
    {path: 'server-error',component: ServerErrorComponent},
];
