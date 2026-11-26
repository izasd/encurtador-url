import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Sessao } from './sessao';

/** Envia o token nas chamadas à API e encerra a sessão quando a API não o aceita mais. */
export const autenticacaoInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  const token = sessao.token();

  if (!token || !requisicao.url.startsWith('/api/')) {
    return proximo(requisicao);
  }

  return proximo(requisicao.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    catchError((erro: unknown) => {
      // Token expirado ou inválido: volta para o login avisando o motivo.
      if (erro instanceof HttpErrorResponse && erro.status === 401) {
        sessao.sair();
        void router.navigate(['/entrar'], { queryParams: { sessao: 'expirada' } });
      }

      return throwError(() => erro);
    }),
  );
};
