import { inject, Injectable, Renderer2, RendererFactory2 } from '@angular/core';

import { DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
@Injectable({
  providedIn: 'root',
})
export class MyTranslateService {
  private readonly document = inject(DOCUMENT);

  private readonly translateService = inject(TranslateService);

  private readonly renderer: Renderer2 = inject(RendererFactory2).createRenderer(null, null);

  changeDirection(): void {
    const lang = localStorage.getItem('lang') ?? 'en';

    const direction = lang === 'ar' ? 'rtl' : 'ltr';

    this.renderer.setAttribute(this.document.documentElement, 'dir', direction);

    this.renderer.setAttribute(this.document.documentElement, 'lang', lang);
  }

  changeLang(lang: string): void {
    localStorage.setItem('lang', lang);

    this.translateService.use(lang);

    this.changeDirection();
  }
}
