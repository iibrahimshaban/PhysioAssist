import { Injectable, signal } from '@angular/core';
import { ChatMessage, ChatSession, SearchMode } from '../../Shared/Models/chat.model';

/**
 * Persists chat sessions entirely on the client (localStorage).
 *
 * Why localStorage and not sessionStorage: sessionStorage dies the moment the
 * tab closes, which feels broken for a "recent searches" panel a user expects
 * to still be there tomorrow (like a customer-service widget). We instead
 * enforce our own expiry (SESSION_TTL_MS) and a max session count, so old
 * junk still gets pruned automatically.
 *
 * If you'd rather it behave like a true single-tab session, swap
 * `window.localStorage` for `window.sessionStorage` below — nothing else
 * needs to change.
 */

@Injectable({
  providedIn: 'root',
})
export class ChatSessionStorageService {
  private readonly STORAGE_KEY = 'ask-ai:chat_sessions';
  private readonly MAX_SESSIONS = 25;
  private readonly SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000; // 7 days in milliseconds

  readonly sessions = signal<ChatSession[]>(this.readAndPrune());


  createSession(mode:SearchMode): ChatSession
  {
    const session: ChatSession = {
      id: crypto.randomUUID(),
      title: 'New search',
      mode,
      createdAt: Date.now(),
      updatedAt: Date.now(),
      messages: [],
    };

    this.sessions.update((list) => [session, ...list].slice(0, this.MAX_SESSIONS));
    this.persist();
    return session;
  }

  getSession(id: string): ChatSession | undefined {
    return this.sessions().find((s) => s.id === id);
  }

  appendMessage(sessionId: string, message: ChatMessage): void {
    this.sessions.update((list) =>
      list.map((s) => {
        if (s.id !== sessionId) return s;
        const messages = [...s.messages, message];
        const title = s.title === 'New search' ? this.deriveTitle(messages) : s.title;
        return { ...s, messages, title, updatedAt: Date.now() };
      }),
    );
    this.persist();
  }

  updateMessage(sessionId: string, messageId: string, patch: Partial<ChatMessage>): void {
    this.sessions.update((list) =>
      list.map((s) => {
        if (s.id !== sessionId) return s;
        return {
          ...s,
          messages: s.messages.map((m) => (m.id === messageId ? { ...m, ...patch } : m)),
          updatedAt: Date.now(),
        };
      }),
    );
    this.persist();
  }

  deleteSession(id: string): void {
    this.sessions.update((list) => list.filter((s) => s.id !== id));
    this.persist();
  }

  clearAll(): void {
    this.sessions.set([]);
    this.persist();
  }

  private deriveTitle(messages: ChatMessage[]): string {
    const firstUserMsg = messages.find((m) => m.role === 'user');
    if (!firstUserMsg) return 'New search';
    return firstUserMsg.content.length > 48
      ? firstUserMsg.content.slice(0, 48).trimEnd() + '…'
      : firstUserMsg.content;
  }


  private persist(): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.sessions()));
    } catch {
      // localStorage can throw in private-browsing/quota-exceeded situations.
      // Chat still works for the current page load; it just won't survive a refresh.
    }
  }

  private readAndPrune(): ChatSession[] {
    let raw: ChatSession[] = [];
    try {
      raw = JSON.parse(localStorage.getItem(this.STORAGE_KEY) ?? '[]');
    } catch {
      raw = [];
    }
    const cutoff = Date.now() - this.SESSION_TTL_MS;
    const fresh = raw.filter((s) => s.updatedAt >= cutoff).slice(0, this.MAX_SESSIONS);
    if (fresh.length !== raw.length) {
      try {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(fresh));
      } catch {
        /* ignore */
      }
    }
    return fresh.sort((a, b) => b.updatedAt - a.updatedAt);
  }
}
