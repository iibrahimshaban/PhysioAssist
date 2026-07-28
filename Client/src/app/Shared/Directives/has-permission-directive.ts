import { Directive, Input, TemplateRef, ViewContainerRef, OnInit } from '@angular/core';
import { AuthService } from '../../Core/Services/auth.service';

@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective implements OnInit {
  private permissions: string[] = [];
  private hasView = false;

  @Input() set appHasPermission(value: string | string[]) {
    this.permissions = Array.isArray(value) ? value : [value];
    this.updateView();
  }

  constructor(
    private templateRef: TemplateRef<unknown>,
    private viewContainer: ViewContainerRef,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.updateView();
  }

  private updateView(): void {
    const hasAccess =
      this.permissions.length === 0 ||
      this.permissions.every(p => this.auth.hasPermission(p));

    if (hasAccess && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasAccess && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }
}