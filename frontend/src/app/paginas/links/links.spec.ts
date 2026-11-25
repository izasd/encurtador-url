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
};

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

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Links],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Links);
    pagina = fixture.nativeElement as HTMLElement;

    (await vi.waitFor(() => http.expectOne('/api/links'))).flush([linkComAcento]);
    await vi.waitFor(() => expect(pagina.querySelector('.item')).not.toBeNull());
  });

  afterEach(() => http.verify());

  it('mostra a URL original legível, com os acentos', () => {
    expect(pagina.querySelector('.url-original')?.textContent).toBe('https://exemplo.com/promoção');
  });

  it('cria um link com código personalizado e recarrega a lista', async () => {
    preencher('#url', 'https://angular.dev/');
    preencher('#codigo', '  promo26 ');
    enviar();

    const criacao = await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }));
    expect(criacao.request.body).toEqual({ urlOriginal: 'https://angular.dev/', codigo: 'promo26' });
    criacao.flush({ ...linkComAcento, codigo: 'promo26', urlCurta: 'http://localhost:5198/promo26' });

    (await vi.waitFor(() => http.expectOne('/api/links'))).flush([linkComAcento]);
    await vi.waitFor(() => expect(pagina.querySelector('.criado')?.textContent).toContain('http://localhost:5198/promo26'));
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

  it('aceita uma URL com espaços e acentos, que a API normaliza', async () => {
    preencher('#url', 'https://exemplo.com/promoção de verão');
    enviar();

    const criacao = await vi.waitFor(() => http.expectOne({ method: 'POST', url: '/api/links' }));
    expect(criacao.request.body).toEqual({ urlOriginal: 'https://exemplo.com/promoção de verão', codigo: null });
    criacao.flush(linkComAcento);
    (await vi.waitFor(() => http.expectOne('/api/links'))).flush([linkComAcento]);
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
});
