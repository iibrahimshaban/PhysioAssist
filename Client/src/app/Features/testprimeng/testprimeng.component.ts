import { Component } from '@angular/core';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
@Component({
  selector: 'app-testprimeng',
  imports: [CardModule, ButtonModule],
  templateUrl: './testprimeng.component.html',
  styleUrl: './testprimeng.component.css',
})
export class TestprimengComponent {}
