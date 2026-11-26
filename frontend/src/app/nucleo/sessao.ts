import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LoginResposta, Usuario } from './api';

const CHAVE_ARMAZENAMENTO = 'encurtador.sessao';

/** Estado do login. Fica no localStorage para sobreviver a recarregamentos da página. */
@Injectable({ providedIn: 'root' })
export class Sessao {
  private readonly http = inject(HttpClient);
  private readonly atual = signal<LoginResposta | null>(lerSessaoSalva());

  readonly usuario = computed<Usuario | null>(() => this.atual()?.usuario ?? null);
  readonly token = computed<string | null>(() => this.atual()?.token ?? null);

  /** Se há uma sessão e o token ainda não expirou (conferido no momento da chamada). */
  valida(): boolean {
    const atual = this.atual();
    return atual !== null && new Date(atual.expiraEm).getTime() > Date.now();
  }

  async entrar(email: string, senha: string): Promise<void> {
    this.iniciar(await firstValueFrom(this.http.post<LoginResposta>('/api/auth/login', { email, senha })));
  }

  async cadastrar(nome: string, email: string, senha: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/registrar', { nome, email, senha }));
    await this.entrar(email, senha);
  }

  async entrarComoVisitante(): Promise<void> {
    this.iniciar(await firstValueFrom(this.http.post<LoginResposta>('/api/auth/visitante', null)));
  }

  sair(): void {
    localStorage.removeItem(CHAVE_ARMAZENAMENTO);
    this.atual.set(null);
  }

  private iniciar(resposta: LoginResposta): void {
    localStorage.setItem(CHAVE_ARMAZENAMENTO, JSON.stringify(resposta));
    this.atual.set(resposta);
  }
}

function lerSessaoSalva(): LoginResposta | null {
  try {
    const salva = JSON.parse(localStorage.getItem(CHAVE_ARMAZENAMENTO) ?? 'null') as LoginResposta | null;
    return salva && new Date(salva.expiraEm).getTime() > Date.now() ? salva : null;
  } catch {
    return null;
  }
}
