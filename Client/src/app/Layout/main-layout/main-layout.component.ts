import { Component } from '@angular/core';
import { HeaderComponent } from "../header/header.component";
import { RouterOutlet } from '@angular/router';
import { AskAsiPanelComponent } from "../../Shared/Components/ask-asi-panel/ask-asi-panel.component";
import { AskAsiButtonComponent } from "../../Shared/Components/ask-asi-button/ask-asi-button.component";

@Component({
  selector: 'app-main-layout',
  imports: [HeaderComponent, RouterOutlet, AskAsiPanelComponent, AskAsiButtonComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css',
})
export class MainLayoutComponent {}
