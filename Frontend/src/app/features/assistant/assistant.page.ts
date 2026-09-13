import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state';
import { AssistantService } from '../../core/services/assistant.service';
import { ToastService } from '../../core/services/toast.service';
import { ConversationSummary, ToolCallTrace } from '../../core/models/assistant';
import { firstDetail } from '../../core/interceptors/api-error';

interface ChatTurn {
  question: string;
  answer: string;
  trace: ToolCallTrace[];
  pending: boolean;
  error: boolean;
}

@Component({
  selector: 'app-assistant',
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
    EmptyStateComponent,
  ],
  templateUrl: './assistant.page.html',
  styleUrl: './assistant.page.scss',
})
export class AssistantPage implements OnInit {
  private readonly assistant = inject(AssistantService);
  private readonly toast = inject(ToastService);

  readonly threads = signal<ConversationSummary[]>([]);
  readonly activeId = signal<string | null>(null);
  readonly turns = signal<ChatTurn[]>([]);
  readonly status = signal('');
  readonly asking = signal(false);
  readonly loadingThreads = signal(true);
  readonly question = signal('');

  async ngOnInit(): Promise<void> {
    await this.refreshThreads();
  }

  async refreshThreads(): Promise<void> {
    this.loadingThreads.set(true);
    try {
      this.threads.set(await this.assistant.conversations());
    } catch (error) {
      this.toast.error(firstDetail(error));
    } finally {
      this.loadingThreads.set(false);
    }
  }

  newChat(): void {
    this.activeId.set(null);
    this.turns.set([]);
    this.status.set('');
    this.question.set('');
  }

  async openThread(id: string): Promise<void> {
    if (this.asking()) {
      return;
    }
    try {
      const detail = await this.assistant.conversation(id);
      this.activeId.set(detail.id);
      this.turns.set(
        detail.turns.map((turn) => ({
          question: turn.question,
          answer: turn.answer,
          trace: this.parseTrace(turn.toolCalls),
          pending: false,
          error: false,
        })),
      );
      this.status.set('');
    } catch (error) {
      this.toast.error(firstDetail(error));
    }
  }

  async ask(): Promise<void> {
    const text = this.question().trim();
    if (!text || this.asking()) {
      return;
    }

    this.asking.set(true);
    this.status.set('Thinking…');
    this.question.set('');

    const turn: ChatTurn = { question: text, answer: '', trace: [], pending: true, error: false };
    this.turns.update((turns) => [...turns, turn]);

    try {
      await this.assistant.ask(text, this.activeId(), {
        onStatus: (message) => this.status.set(message),
        onToken: (chunk) => {
          turn.answer += chunk;
          this.turns.update((turns) => [...turns]);
        },
        onDone: async (conversationId, trace, answer) => {
          if (!turn.answer.trim() && answer.trim()) {
            turn.answer = answer;
          }
          turn.trace = trace;
          turn.pending = false;
          this.activeId.set(conversationId);
          this.status.set('');
          this.asking.set(false);
          this.turns.update((turns) => [...turns]);
          await this.refreshThreads();
        },
        onError: (message) => {
          turn.pending = false;
          turn.error = true;
          this.status.set('');
          this.asking.set(false);
          this.toast.error(message);
        },
      });
    } catch (error) {
      turn.pending = false;
      turn.error = true;
      this.status.set('');
      this.asking.set(false);
      this.toast.error(firstDetail(error));
    }
  }

  private parseTrace(raw: string): ToolCallTrace[] {
    try {
      const parsed = JSON.parse(raw) as ToolCallTrace[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
}
