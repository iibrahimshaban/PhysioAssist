import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, NgZone, signal, viewChild } from '@angular/core';
import { AiChatService } from '../../../Core/Services/ai-chat.service';
import { ChatSessionStorageService } from '../../../Core/Services/chat-session-storage.service';
import { AskAiPanelStateService } from '../../../Core/Services/ask-ai-panel-state.service';
import { FormsModule } from '@angular/forms';
import { ChatMessage, ChatSession, SearchMode } from '../../Models/chat.model';
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
  private readonly zone = inject(NgZone);
  readonly panelStateService = inject(AskAiPanelStateService);

  private readonly scrollAnchor = viewChild<ElementRef<HTMLDivElement>>('scrollAnchor');
  private activeStream: AbortController | null = null;

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
    afterRenderEffect(() => {
      this.activeSession(); // track dependency
      this.scrollAnchor()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
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

  /** Cancels an in-flight stream, e.g. from a "Stop generating" button. */
  stop(): void {
    this.activeStream?.abort();
  }

  async send(): Promise<void> {
    const text = this.queryText().trim();
    if (!text || this.loading()) return;

    let session = this.activeSession();
    if (!session) {
      session = this.sessionStore.createSession(this.mode());
      this.activeSessionId.set(session.id);
    }
    const sessionId = session.id;
    this.view.set('chat');

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      timestamp: Date.now(),
      status: 'sent',
    };
    this.sessionStore.appendMessage(sessionId, userMessage);
    this.queryText.set('');

    const pendingId = crypto.randomUUID();
    this.sessionStore.appendMessage(sessionId, {
      id: pendingId,
      role: 'assistant',
      content: '',
      timestamp: Date.now(),
      status: 'sending',
    });

    const controller = new AbortController();
    this.activeStream = controller;
    this.loading.set(true);

    let accumulated = '';
    try {
      // session.id IS the conversationId the backend keys its in-memory
      // ChatHistory on — no need to resend prior turns, the server has them.
      for await (const delta of this.aiChat.streamAsk(sessionId, text, controller.signal)) {
        accumulated += delta;
        // Wrapped in zone.run defensively: fetch's ReadableStream reader
        // isn't guaranteed to resume inside Angular's zone on every browser,
        // so without this the bubble can silently stop updating mid-stream.
        this.zone.run(() => {
          this.sessionStore.updateMessage(sessionId, pendingId, {
            content: accumulated,
            status: 'sending',
          });
        });
      }
      this.zone.run(() => {
        this.sessionStore.updateMessage(sessionId, pendingId, { status: 'sent' });
      });
    } catch (err) {
      const aborted = err instanceof DOMException && err.name === 'AbortError';
      this.zone.run(() => {
        this.sessionStore.updateMessage(sessionId, pendingId, {
          content: aborted ? accumulated || 'Stopped.' : accumulated || 'Something went wrong reaching the AI. Please try again.',
          status: aborted ? 'sent' : 'error',
        });
      });
    } finally {
      this.loading.set(false);
      this.activeStream = null;
    }
  }

  onEnter(event: KeyboardEvent): void {
    if (event.shiftKey)
      return; // allow multi-line input with shift+enter

    event.preventDefault();
    void this.send();
  }
}
