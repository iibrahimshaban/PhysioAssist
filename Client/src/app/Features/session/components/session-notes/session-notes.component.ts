import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

import { SessionService } from '../../../../Core/Services/session.service';
import { Suggestion } from '../../../../Shared/Models/suggestion';

@Component({
  selector: 'app-session-notes',
  imports: [],
  templateUrl: './session-notes.component.html',
  styleUrl: './session-notes.component.css',
})
export class SessionNotesComponent {
  private sessionService = inject(SessionService);
  private destroyRef = inject(DestroyRef);

  notes = input.required<string>();
  audioFileUrl = input<string | null>(null);
  isUploadingAudio = input(false);

  notesChange = output<string>();
  record = output<void>();

  suggestions = signal<Suggestion[]>([]);

  private autocompleteSearch$ = new Subject<string>();

  private currentWordStart = 0;
  private currentWordEnd = 0;

  constructor() {
    this.initializeAutocomplete();
  }

  onRecord() {
    this.record.emit();
  }

  onTranscriptChange(event: Event) {
    const textarea = event.target as HTMLTextAreaElement;

    const value = textarea.value;
    const cursorPosition = textarea.selectionStart;

    this.notesChange.emit(value);

    const wordInfo = this.getWordAtCursor(value, cursorPosition);

    this.currentWordStart = wordInfo.start;
    this.currentWordEnd = wordInfo.end;

    if (wordInfo.word.length < 3) {
      this.clearSuggestions();
      return;
    }

    this.autocompleteSearch$.next(wordInfo.word);
  }

  onTranscriptKeyDown(event: KeyboardEvent, textarea: HTMLTextAreaElement) {
    if (event.key === 'Tab' && this.suggestions().length > 0) {
      event.preventDefault();

      this.applySuggestion(this.suggestions()[0], textarea);

      return;
    }

    if (event.key === 'Escape') {
      this.clearSuggestions();
    }
  }

  applySuggestion(suggestion: Suggestion, textarea: HTMLTextAreaElement) {
    const currentText = this.notes();

    const beforeWord = currentText.substring(0, this.currentWordStart);

    const afterWord = currentText.substring(this.currentWordEnd);

    const shouldAddSpace = afterWord.length === 0 || !afterWord.startsWith(' ');

    const space = shouldAddSpace ? ' ' : '';

    const updatedText = beforeWord + suggestion.term + space + afterWord;

    this.notesChange.emit(updatedText);
    this.clearSuggestions();

    setTimeout(() => {
      const newCursorPosition = beforeWord.length + suggestion.term.length + space.length;

      textarea.focus();

      textarea.setSelectionRange(newCursorPosition, newCursorPosition);
    });
  }

  private initializeAutocomplete() {
    this.autocompleteSearch$
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),

        switchMap((word) => {
          if (word.length < 3) {
            return of<Suggestion[]>([]);
          }

          return this.sessionService.getSuggestions(word, 5).pipe(
            catchError((error) => {
              console.error('Autocomplete error', error);

              return of<Suggestion[]>([]);
            }),
          );
        }),

        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((response) => {
        this.suggestions.set(response ?? []);
      });
  }

  private getWordAtCursor(text: string, cursorPosition: number) {
    let start = cursorPosition;
    let end = cursorPosition;

    while (start > 0 && !/\s/.test(text[start - 1])) {
      start--;
    }

    while (end < text.length && !/\s/.test(text[end])) {
      end++;
    }

    return {
      word: text.substring(start, end),
      start,
      end,
    };
  }

  private clearSuggestions() {
    this.suggestions.set([]);
  }
}
