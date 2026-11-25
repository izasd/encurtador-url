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
