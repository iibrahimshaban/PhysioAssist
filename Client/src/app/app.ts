import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './Shared/Components/toast-container/toast-container.component';
import { LoadingBarComponent } from './Shared/Components/loading-bar/loading-bar.component';
import { TranslateService } from '@ngx-translate/core';
import { MyTranslateService } from './Core/Services/my-translate.service';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, LoadingBarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly title = signal('Client');

  private translate = inject(TranslateService);
  private myTranslateService = inject(MyTranslateService);

  constructor() {
    this.translate.addLangs(['ar', 'en']);

    const savedLang = localStorage.getItem('lang');

    if (savedLang && (savedLang === 'ar' || savedLang === 'en')) {
      this.translate.use(savedLang);
    } else {
      const browserLang = this.translate.getBrowserLang();
      const detectedLang = browserLang === 'ar' ? 'ar' : 'en';

      this.translate.use(detectedLang);
      localStorage.setItem('lang', detectedLang);
    }

    this.myTranslateService.changeDirection();
  }
}
