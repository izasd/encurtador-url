import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { autenticacaoInterceptor } from './autenticacao-interceptor';
import { Sessao } from './sessao';

describe('autenticacaoInterceptor', () => {
  let http: HttpClient;
  let controle: HttpTestingController;

  beforeEach(() => {
    localStorage.setItem(
      'encurtador.sessao',
      JSON.stringify({
        token: 'token-da-ana',
        expiraEm: new Date(Date.now() + 60_000).toISOString(),
        usuario: { nome: 'Ana', email: 'ana@exemplo.com', ehVisitante: false },
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([autenticacaoInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controle = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controle.verify();
    localStorage.clear();
  });

  it('envia o token nas chamadas à API', () => {
    http.get('/api/links').subscribe();

    expect(controle.expectOne('/api/links').request.headers.get('Authorization')).toBe('Bearer token-da-ana');
  });

  it('não envia o token para outros endereços', () => {
    http.get('https://exemplo.com/dados').subscribe();

    expect(controle.expectOne('https://exemplo.com/dados').request.headers.has('Authorization')).toBe(false);
  });

  it('quando a API recusa o token (401), encerra a sessão e volta para o login', () => {
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    http.get('/api/links').subscribe({ error: () => undefined });
    controle.expectOne('/api/links').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(TestBed.inject(Sessao).token()).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/entrar'], { queryParams: { sessao: 'expirada' } });
  });
});
