// Formatos das respostas da API (backend/src/Encurtador.Api).

export interface Usuario {
  nome: string;
  email: string;
  ehVisitante: boolean;
}

export interface LoginResposta {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}

export type SituacaoDoLink = 'disponivel' | 'desativado' | 'expirado';

export interface Link {
  codigo: string;
  urlOriginal: string;
  urlCurta: string;
  criadoEm: string;
  ativo: boolean;
  /** Instante (UTC) em que o link para de redirecionar; null quando não expira. */
  expiraEm: string | null;
  /** Calculada pela API, para não depender do relógio do navegador. */
  situacao: SituacaoDoLink;
}

export interface CliquesPorDia {
  /** Data no formato aaaa-mm-dd, no fuso pedido à API. */
  dia: string;
  cliques: number;
}

export interface CliquesPorCategoria {
  nome: string;
  cliques: number;
}

export interface Estatisticas {
  codigo: string;
  urlOriginal: string;
  urlCurta: string;
  fusoHorario: string;
  totalDeCliques: number;
  porDia: CliquesPorDia[];
  porPais: CliquesPorCategoria[];
  porNavegador: CliquesPorCategoria[];
  porDispositivo: CliquesPorCategoria[];
  porReferer: CliquesPorCategoria[];
}
