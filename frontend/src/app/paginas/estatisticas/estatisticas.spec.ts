import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Estatisticas as DadosDeEstatisticas } from '../../nucleo/api';
import { Estatisticas } from './estatisticas';

const estatisticas: DadosDeEstatisticas = {
  codigo: 'abc123',
  urlOriginal: 'https://exemplo.com/promo%C3%A7%C3%A3o',
  urlCurta: 'http://localhost:5198/abc123',
  fusoHorario: 'America/Sao_Paulo',
  totalDeCliques: 1284,
  porDia: [
    { dia: '2026-11-20', cliques: 1000 },
    { dia: '2026-11-22', cliques: 284 },
  ],
  porPais: [
    { nome: 'BR', cliques: 1200 },
    { nome: 'Desconhecido', cliques: 84 },
  ],
  porNavegador: [{ nome: 'Chrome', cliques: 1284 }],
  porDispositivo: [{ nome: 'Celular', cliques: 1284 }],
  porReferer: [{ nome: 'Direto', cliques: 1284 }],
};

describe('Estatisticas', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<Estatisticas>;
  let pagina: HTMLElement;

  async function abrir(): Promise<TestRequest> {
    fixture = TestBed.createComponent(Estatisticas);
    fixture.componentRef.setInput('codigo', 'abc123');
    pagina = fixture.nativeElement as HTMLElement;
    return vi.waitFor(() => http.expectOne((r) => r.url === '/api/links/abc123/estatisticas'));
  }

  beforeEach(async () => {
    // Mesmo idioma do app (app.config.ts): números como 1.284.
    registerLocaleData(localePt);
    await TestBed.configureTestingModule({
      imports: [Estatisticas],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('pede as estatísticas no fuso do navegador', async () => {
    const requisicao = await abrir();

    expect(requisicao.request.params.get('fusoHorario')).toBe(Intl.DateTimeFormat().resolvedOptions().timeZone);
    requisicao.flush(estatisticas);
  });

  it('mostra o total em destaque, a URL legível e os países por extenso', async () => {
    (await abrir()).flush(estatisticas);

    await vi.waitFor(() => expect(pagina.querySelector('.total-numero')?.textContent).toBe('1.284'));
    expect(pagina.querySelector('.url-original')?.textContent).toBe('https://exemplo.com/promoção');
    expect(pagina.textContent).toContain('Brasil');
    expect(pagina.textContent).toContain('Desconhecido');
  });

  it('oferece os dados do gráfico diário em uma tabela, com os dias sem cliques', async () => {
    (await abrir()).flush(estatisticas);
    await vi.waitFor(() => expect(pagina.querySelector('app-grafico-diario')).not.toBeNull());

    pagina.querySelector<HTMLButtonElement>('app-grafico-diario .link-botao')!.click();

    await vi.waitFor(() => expect(pagina.querySelectorAll('app-grafico-diario tbody tr').length).toBeGreaterThan(2));
    const celulas = [...pagina.querySelectorAll('app-grafico-diario tbody td:last-child')].map((c) => c.textContent?.trim());
    expect(celulas).toContain('0');
    expect(celulas).toContain('1.000');
  });

  it('avisa quando o link não existe ou é de outra conta', async () => {
    (await abrir()).flush({ status: 404, detail: 'Nenhum link encontrado.' }, { status: 404, statusText: 'Not Found' });

    await vi.waitFor(() => expect(pagina.querySelector('h1')?.textContent).toContain('Link não encontrado'));
  });

  it('mostra um aviso amigável quando ainda não há cliques', async () => {
    (await abrir()).flush({ ...estatisticas, totalDeCliques: 0, porDia: [], porPais: [], porNavegador: [], porDispositivo: [], porReferer: [] });

    await vi.waitFor(() => expect(pagina.textContent).toContain('Ainda não há cliques'));
    expect(pagina.querySelector('app-grafico-diario')).toBeNull();
  });
});
