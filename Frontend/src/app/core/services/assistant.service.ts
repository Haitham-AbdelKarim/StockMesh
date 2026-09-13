import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ConversationDetail, ConversationSummary } from '../models/assistant';
import { TokenStoreService } from './token-store.service';

export interface SseHandler {
  onStatus?: (message: string) => void;
  onToken?: (text: string) => void;
  onDone?: (
    conversationId: string,
    toolCalls: { name: string; args: Record<string, unknown> }[],
    answer: string,
  ) => void;
  onError?: (message: string) => void;
}

@Injectable({ providedIn: 'root' })
export class AssistantService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenStoreService);

  private get base(): string {
    return `${environment.apiBase}/api/v1/assistant`;
  }

  conversations(): Promise<ConversationSummary[]> {
    return lastValueFrom(this.http.get<ConversationSummary[]>(`${this.base}/conversations`));
  }

  conversation(id: string): Promise<ConversationDetail> {
    return lastValueFrom(this.http.get<ConversationDetail>(`${this.base}/conversations/${id}`));
  }

  async ask(question: string, conversationId: string | null, handler: SseHandler): Promise<void> {
    const token = this.tokens.accessToken();

    const response = await fetch(`${this.base}/ask`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({ question, conversationId }),
    });

    if (!response.ok || !response.body) {
      const detail = await this.readProblemDetail(response);
      handler.onError?.(detail);
      return;
    }

    await this.readStream(response.body, handler);
  }

  private async readProblemDetail(response: Response): Promise<string> {
    try {
      const body = (await response.json()) as { detail?: string; title?: string };
      return body.detail ?? body.title ?? `Request failed (${response.status}).`;
    } catch {
      return `Request failed (${response.status}).`;
    }
  }

  private async readStream(stream: ReadableStream<Uint8Array>, handler: SseHandler): Promise<void> {
    const reader = stream.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let event = '';
    let data = '';

    const dispatch = () => {
      if (!event || !data) {
        return;
      }

      try {
        const payload = JSON.parse(data) as {
          message?: string;
          text?: string;
          answer?: string;
          toolCalls?: { name: string; args: Record<string, unknown> }[];
          conversationId?: string;
        };

        if (event === 'status') {
          handler.onStatus?.(payload.message ?? '');
        } else if (event === 'token') {
          handler.onToken?.(payload.text ?? '');
        } else if (event === 'done') {
          handler.onDone?.(payload.conversationId ?? '', payload.toolCalls ?? [], payload.answer ?? '');
        } else if (event === 'error') {
          handler.onError?.(payload.message ?? 'The assistant failed to respond.');
        }
      } catch {
        handler.onError?.('Could not parse the assistant response.');
      } finally {
        event = '';
        data = '';
      }
    };

    for (;;) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const frames = buffer.split('\n\n');
      buffer = frames.pop() ?? '';

      for (const frame of frames) {
        for (const line of frame.split('\n')) {
          if (line.startsWith('event:')) {
            event = line.slice(6).trim();
          } else if (line.startsWith('data:')) {
            data += line.slice(5).trim();
          }
        }
        dispatch();
      }
    }

    dispatch();
  }
}
