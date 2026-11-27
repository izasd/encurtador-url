import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { somenteAutenticado } from './guardas';
import { Sessao } from './sessao';

describe('somenteAutenticado', () => {
  const sessao = { valida: vi.fn(), sair: vi.fn() };

  beforeEach(() => {
    sessao.valida.mockReset();
    sessao.sair.mockReset();

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Sessao, useValue: sessao }],
    });
  });

  /** Resultado da guarda ao abrir a URL: true ou o endereço para onde ela manda. */
  function abrir(url: string): unknown {
    const resultado = TestBed.runInInjectionContext(() =>
      somenteAutenticado({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
    return resultado instanceof UrlTree ? TestBed.inject(Router).serializeUrl(resultado) : resultado;
  }

  it('deixa passar quem tem sessão válida', () => {
    sessao.valida.mockReturnValue(true);

    expect(abrir('/links')).toBe(true);
  });

  it('manda para o login sem ?voltar= quando o destino é a página de links', () => {
    sessao.valida.mockReturnValue(false);

    expect(abrir('/links')).toBe('/entrar');
    expect(sessao.sair).toHaveBeenCalled();
  });

  it('guarda as outras páginas em ?voltar=, para voltar a elas depois do login', () => {
    sessao.valida.mockReturnValue(false);

    expect(abrir('/links/abc123/estatisticas')).toBe('/entrar?voltar=%2Flinks%2Fabc123%2Festatisticas');
  });
});
