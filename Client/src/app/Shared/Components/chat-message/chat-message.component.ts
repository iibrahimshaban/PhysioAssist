import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ChatMessage } from '../../Models/chat.model';

@Component({
  selector: 'app-chat-message',
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chat-message.component.html',
  styleUrl: './chat-message.component.css',
})
export class ChatMessageComponent {
  message = input.required<ChatMessage>();
}
