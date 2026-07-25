import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ChatMessage } from '../../Models/chat.model';
import { MarkdownPipe } from '../../Pipes/markdown-pipe';

@Component({
  selector: 'app-chat-message',
  imports: [MarkdownPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chat-message.component.html',
  styleUrl: './chat-message.component.css',
})
export class ChatMessageComponent {
  message = input.required<ChatMessage>();
}
