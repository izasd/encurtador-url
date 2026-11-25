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

export interface Link {
  codigo: string;
  urlOriginal: string;
  urlCurta: string;
  criadoEm: string;
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
