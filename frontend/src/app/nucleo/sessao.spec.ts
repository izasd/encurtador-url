import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LoginResposta } from './api';
import { Sessao } from './sessao';

const CHAVE = 'encurtador.sessao';

function respostaDeLogin(expiraEmMs: number): LoginResposta {
  return {
    token: 'token-de-teste',
    expiraEm: new Date(Date.now() + expiraEmMs).toISOString(),
    usuario: { nome: 'Ana Souza', email: 'ana@exemplo.com', ehVisitante: false },
  };
}

describe('Sessao', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('entra, guarda a sessão e passa a ser válida', async () => {
    const sessao = TestBed.inject(Sessao);

    const entrando = sessao.entrar('ana@exemplo.com', 'senha-forte-123');
    const requisicao = http.expectOne('/api/auth/login');
    expect(requisicao.request.body).toEqual({ email: 'ana@exemplo.com', senha: 'senha-forte-123' });
    requisicao.flush(respostaDeLogin(60 * 60 * 1000));
    await entrando;

    expect(sessao.valida()).toBe(true);
    expect(sessao.token()).toBe('token-de-teste');
    expect(sessao.usuario()?.nome).toBe('Ana Souza');
    expect(localStorage.getItem(CHAVE)).toContain('token-de-teste');
  });

  it('cadastra a conta e entra logo em seguida', async () => {
    const sessao = TestBed.inject(Sessao);

    const cadastrando = sessao.cadastrar('Ana Souza', 'ana@exemplo.com', 'senha-forte-123');
    http.expectOne('/api/auth/registrar').flush({ nome: 'Ana Souza', email: 'ana@exemplo.com', ehVisitante: false });
    const login = await vi.waitFor(() => http.expectOne('/api/auth/login'));
    login.flush(respostaDeLogin(60 * 60 * 1000));
    await cadastrando;

    expect(sessao.valida()).toBe(true);
  });

  it('recupera uma sessão salva que ainda vale', () => {
    localStorage.setItem(CHAVE, JSON.stringify(respostaDeLogin(60 * 1000)));

    expect(TestBed.inject(Sessao).valida()).toBe(true);
  });

  it('descarta uma sessão salva que já expirou', () => {
    localStorage.setItem(CHAVE, JSON.stringify(respostaDeLogin(-1000)));
    const sessao = TestBed.inject(Sessao);

    expect(sessao.valida()).toBe(false);
    expect(sessao.usuario()).toBeNull();
  });

  it('sai apagando o token', () => {
    localStorage.setItem(CHAVE, JSON.stringify(respostaDeLogin(60 * 1000)));
    const sessao = TestBed.inject(Sessao);

    sessao.sair();

    expect(sessao.token()).toBeNull();
    expect(localStorage.getItem(CHAVE)).toBeNull();
  });
});
