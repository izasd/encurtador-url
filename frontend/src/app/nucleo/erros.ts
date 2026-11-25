import { HttpErrorResponse } from '@angular/common/http';

export interface ErrosDaApi {
  /** Mensagem para o formulário inteiro (ex.: "E-mail ou senha incorretos."). */
  geral: string | null;
  /** Primeira mensagem de cada campo inválido, como a API devolve em "errors". */
  campos: Record<string, string>;
}

interface ProblemDetails {
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Converte uma resposta de erro da API (no formato ProblemDetails) em mensagens para a tela. */
export function lerErros(erro: unknown, mensagemPadrao = 'Algo deu errado. Tente de novo.'): ErrosDaApi {
  if (!(erro instanceof HttpErrorResponse)) {
    return { geral: mensagemPadrao, campos: {} };
  }

  if (erro.status === 0) {
    return { geral: 'Não foi possível falar com o servidor. Verifique sua conexão.', campos: {} };
  }

  const problema: ProblemDetails = typeof erro.error === 'object' && erro.error !== null ? erro.error : {};

  const campos: Record<string, string> = {};
  for (const [campo, mensagens] of Object.entries(problema.errors ?? {})) {
    if (mensagens.length > 0) {
      campos[campo] = mensagens[0];
    }
  }

  const temErroDeCampo = Object.keys(campos).length > 0;
  return { geral: temErroDeCampo ? null : (problema.detail ?? mensagemPadrao), campos };
}
