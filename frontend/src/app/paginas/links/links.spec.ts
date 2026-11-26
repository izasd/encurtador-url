import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Link } from '../../nucleo/api';
import { Links } from './links';

const linkComAcento: Link = {
  codigo: 'abc123',
  urlOriginal: 'https://exemplo.com/promo%C3%A7%C3%A3o',
  urlCurta: 'http://localhost:5198/abc123',
  criadoEm: '2026-11-25T12:00:00Z',
  ativo: true,
  expiraEm: null,
  situacao: 'disponivel',
};

const linkDesativado: Link = { ...linkComAcento, codigo: 'off123', urlCurta: 'http://localhost:5198/off123', ativo: false, situacao: 'desativado' };
const linkExpirando: Link = { ...linkComAcento, codigo: 'exp123', urlCurta: 'http://localhost:5198/exp123', expiraEm: '2026-12-31T12:00:00Z' };
const linkExpirado: Link = { ...linkComAcento, codigo: 'old123', urlCurta: 'http://localhost:5198/old123', expiraEm: '2026-11-01T12:00:00Z', situacao: 'expirado' };
const todos = [linkComAcento, linkDesativado, linkExpirando, linkExpirado];

describe('Links', () => {
  let http: HttpTestingController;
  let pagina: HTMLElement;

  function preencher(seletor: string, valor: string): void {
    const campo = pagina.querySelector<HTMLInputElement>(seletor)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  }

  function enviar(): void {
    pagina.querySelector('form')!.dispatchEvent(new Event('submit'));
  }

  function botao(texto: string, dentroDe: ParentNode = pagina): HTMLButtonElement {
    const encontrado = [...dentroDe.querySelectorAll('button')].find((b) => b.textContent?.trim() === texto);
    if (!encontrado) {
      throw new Error(`Botão "${texto}" não encontrado`);
    }
    return encontrado;
  }

  /** Abre o painel "Gerenciar" do primeiro link (abc123) e devolve o painel. */
  async function abrirGerenciar(): Promise<HTMLElement> {
    botao('Gerenciar', pagina.querySelector('.item')!).click();

    // vi.waitFor só tenta de novo quando a função lança erro, por isso o throw.
    return vi.waitFor(() => {
      const painel = pagina.querySelector<HTMLElement>('#gerenciar-abc123');
      if (!painel) {
        throw new Error('O painel "Gerenciar" ainda não abriu.');
      }
      return painel;
    });
  }

  async function responderRecarga(): Promise<void> {
    (await vi.waitFor(() => http.expectOne('/api/links'))).flush(todos);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Links],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Links);
    pagina = fixture.nativeElement as HTMLElement;

    await responderRecarga();
    await vi.waitFor(() => expect(pagina.querySelectorAll('.item')).toHaveLength(4));
  });

  afterEach(() => http.verify());

  it('mostra a URL original legível, com os acentos', () => {
    expect(pagina.querySelector('.url-original')?.textContent).toBe('https://exemplo.com/promoção');
  });

  it('mostra a situação de cada link', () => {
    const selos = [...pagina.querySelectorAll('.selo')].map((s) => s.textContent?.trim());

    expect(selos).toEqual(['Desativado', 'Expira em 31/12/2026', 'Expirado']);
  });

  it('cria um link com código personalizado e recarrega a lista', async () => {
    preencher('#url', 'https://angular.dev/');
    preencher('#codigo', '  promo26 ');
    enviar();

    const criacao = await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }));
    expect(criacao.request.body).toEqual({ urlOriginal: 'https://angular.dev/', codigo: 'promo26' });
    criacao.flush({ ...linkComAcento, codigo: 'promo26', urlCurta: 'http://localhost:5198/promo26' });

    await responderRecarga();
    await vi.waitFor(() => expect(pagina.querySelector('.criado')?.textContent).toContain('http://localhost:5198/promo26'));
  });

  it('aceita uma URL com espaços e acentos, que a API normaliza', async () => {
    preencher('#url', 'https://exemplo.com/promoção de verão');
    enviar();

    const criacao = await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }));
    expect(criacao.request.body).toEqual({ urlOriginal: 'https://exemplo.com/promoção de verão', codigo: null });
    criacao.flush(linkComAcento);
    await responderRecarga();
  });

  it('mostra no campo do código quando ele já está em uso (409)', async () => {
    preencher('#url', 'https://angular.dev/');
    preencher('#codigo', 'github');
    enviar();

    (await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }))).flush(
      { title: 'Conflict', status: 409, detail: "O código 'github' já está em uso." },
      { status: 409, statusText: 'Conflict' },
    );

    await vi.waitFor(() => expect(pagina.querySelector('#erro-codigo')?.textContent).toContain('já está em uso'));
  });

  it('mostra quanto esperar quando o limite de criação é atingido (429)', async () => {
    preencher('#url', 'https://angular.dev/');
    enviar();

    (await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }))).flush(
      { title: 'Too Many Requests', status: 429, detail: 'Muitas requisições em pouco tempo. Tente de novo em 40 segundos.' },
      { status: 429, statusText: 'Too Many Requests' },
    );

    await vi.waitFor(() => expect(pagina.querySelector('.alerta-erro')?.textContent).toContain('Tente de novo em 40 segundos'));
  });

  it('não envia um código personalizado inválido', async () => {
    preencher('#url', 'https://angular.dev/');
    preencher('#codigo', 'a-b');
    enviar();

    await vi.waitFor(() => expect(pagina.querySelector('#erro-codigo')?.textContent).toContain('4 a 10 letras'));
    http.expectNone({ method: 'POST', url: '/api/links' });
  });

  it('copia a URL curta e confirma no botão', async () => {
    const escrever = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText: escrever }, configurable: true });

    pagina.querySelector<HTMLButtonElement>('.acoes button')!.click();

    await vi.waitFor(() => expect(pagina.querySelector('.acoes button')?.textContent).toContain('Copiado!'));
    expect(escrever).toHaveBeenCalledWith('http://localhost:5198/abc123');
  });

  it('desativa o link pelo painel Gerenciar, enviando só o que mudou', async () => {
    const painel = await abrirGerenciar();
    const ativo = painel.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    ativo.checked = false;
    ativo.dispatchEvent(new Event('change'));

    const alteracao = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: '/api/links/abc123' }));
    expect(alteracao.request.body).toEqual({ ativo: false });
    alteracao.flush({ ...linkComAcento, ativo: false, situacao: 'desativado' });
    await responderRecarga();
  });

  it('define a expiração para o fim do dia escolhido, no fuso de quem usa', async () => {
    const painel = await abrirGerenciar();
    painel.querySelector<HTMLInputElement>('input[type="date"]')!.value = '2099-12-31';
    botao('Salvar data', painel).click();

    const alteracao = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: '/api/links/abc123' }));
    expect(alteracao.request.body).toEqual({ expiraEm: new Date('2099-12-31T23:59:59').toISOString() });
    alteracao.flush({ ...linkComAcento, expiraEm: '2099-12-31T23:59:59Z' });
    await responderRecarga();
  });

  it('não aceita uma data de expiração no passado', async () => {
    const painel = await abrirGerenciar();
    painel.querySelector<HTMLInputElement>('input[type="date"]')!.value = '2020-01-01';
    botao('Salvar data', painel).click();

    await vi.waitFor(() => expect(painel.querySelector('[role="alert"]')?.textContent).toContain('data futura'));
    http.expectNone({ method: 'PATCH', url: '/api/links/abc123' });
  });

  it('só exclui o link depois da confirmação', async () => {
    const painel = await abrirGerenciar();
    botao('Excluir link', painel).click();

    await vi.waitFor(() => expect(painel.textContent).toContain('Excluir este link de vez?'));
    http.expectNone({ method: 'DELETE', url: '/api/links/abc123' });

    botao('Excluir', painel).click();
    (await vi.waitFor(() => http.expectOne({ method: 'DELETE', url: '/api/links/abc123' }))).flush(null, {
      status: 204,
      statusText: 'No Content',
    });
    await responderRecarga();
  });
});
