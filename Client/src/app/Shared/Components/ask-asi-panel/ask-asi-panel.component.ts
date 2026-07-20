import { afterNextRender, ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { AiChatService } from '../../../Core/Services/ai-chat.service';
import { ChatSessionStorageService } from '../../../Core/Services/chat-session-storage.service';
import { AskAiPanelStateService } from '../../../Core/Services/ask-ai-panel-state.service';
import { FormsModule } from '@angular/forms';
import { ChatMessage, ChatSession, SearchMode } from '../../Models/chat.model';
import { finalize } from 'rxjs';
import { ChatMessageComponent } from '../chat-message/chat-message.component';

type PanelView = 'search' | 'chat';

@Component({
  selector: 'app-ask-asi-panel',
  imports: [FormsModule, ChatMessageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ask-asi-panel.component.html',
  styleUrl: './ask-asi-panel.component.css',
})
export class AskAsiPanelComponent {
  private readonly aiChat = inject(AiChatService);
  private readonly sessionStore = inject(ChatSessionStorageService);
  readonly panelStateService = inject(AskAiPanelStateService);

  private readonly scrollAnchor = viewChild<ElementRef<HTMLDivElement>>('scrollAnchor');

  readonly view = signal<PanelView>('search');
  readonly mode = signal<SearchMode>('smart');
  readonly queryText = signal('');
  readonly loading = signal(false);
  readonly activeSessionId = signal<string | null>(null);

  readonly sessions = this.sessionStore.sessions;

  readonly recentSessions = computed(() => {
    return this.sessions()
               .filter(session => session.messages.length > 0)
               .slice(0,8);
  });

  readonly activeSession = computed<ChatSession | undefined>(() =>
    this.sessions().find((s) => s.id === this.activeSessionId()),
  );

  constructor() {
    // Auto-scroll to the newest message whenever the active thread changes.
    effect(() => {
      this.activeSession(); // track dependency
      afterNextRender(() => {
        this.scrollAnchor()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
      });
    });
  }

  close(): void {
    this.panelStateService.close();
  }

  setMode(mode: SearchMode): void {
    this.mode.set(mode);
  }

  openSession(sessionId: string): void {
    this.activeSessionId.set(sessionId);
    this.view.set('chat');
  }

  startNewChat(): void {
    this.activeSessionId.set(null);
    this.queryText.set('');
    this.view.set('search');
  }

  deleteSession(event: Event, sessionId: string): void {
    event.stopPropagation();
    this.sessionStore.deleteSession(sessionId);
    this.aiChat.clear(sessionId).subscribe();
    if (this.activeSessionId() === sessionId) {
      this.startNewChat();
    }
  }

  send(): void {
    const text = this.queryText().trim();
    if (!text || this.loading()) return;

    let session = this.activeSession();
    if (!session) {
      session = this.sessionStore.createSession(this.mode());
      this.activeSessionId.set(session.id);
    }
    this.view.set('chat');

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      timestamp: Date.now(),
      status: 'sent',
    };
    this.sessionStore.appendMessage(session.id, userMessage);
    this.queryText.set('');

    const pendingId = crypto.randomUUID();
    const pendingMessage: ChatMessage = {
      id: pendingId,
      role: 'assistant',
      content: '…',
      timestamp: Date.now(),
      status: 'sending',
    };
    this.sessionStore.appendMessage(session.id, pendingMessage);

    // session.id IS the conversationId the backend keys its in-memory
    // ChatHistory on — no need to resend prior turns, the server has them.

    this.loading.set(true);
    this.aiChat
      .ask(session.id, text)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (res) => {
          this.sessionStore.updateMessage(session!.id, pendingId, {
            content: res.answer,
            status: 'sent',
          });
        },
        error: () => {
          this.sessionStore.updateMessage(session!.id, pendingId, {
            content: 'Something went wrong reaching the AI. Please try again.',
            status: 'error',
          });
        },
      });
  }

  onEnter(event: Event): void {
    event.preventDefault();
    this.send();
  }
}
