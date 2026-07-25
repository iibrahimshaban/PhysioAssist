import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HeaderComponent } from "../../Layout/header/header.component";
import { AskAsiPanelComponent } from "../../Shared/Components/ask-asi-panel/ask-asi-panel.component";
import { AskAsiButtonComponent } from "../../Shared/Components/ask-asi-button/ask-asi-button.component";
import { AuthService } from '../../Core/Services/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, HeaderComponent, AskAsiPanelComponent, AskAsiButtonComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  auth = inject(AuthService);
}
