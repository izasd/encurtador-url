import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

function salvarSessao(nome: string): void {
  localStorage.setItem(
    'encurtador.sessao',
    JSON.stringify({
      token: 'token',
      expiraEm: new Date(Date.now() + 60_000).toISOString(),
      usuario: { nome, email: 'ana@exemplo.com', ehVisitante: false },
    }),
  );
}

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('mostra a marca e esconde o menu da conta sem login', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const pagina = fixture.nativeElement as HTMLElement;

    expect(pagina.querySelector('.logo')?.textContent).toContain('encurtador');
    expect(pagina.querySelector('.conta')).toBeNull();
  });

  it('mostra o nome do usuário como texto, sem interpretar HTML (proteção contra XSS)', async () => {
    salvarSessao('<img src=x onerror=alert(1)>');

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const pagina = fixture.nativeElement as HTMLElement;

    expect(pagina.querySelector('.conta-nome')?.textContent).toBe('<img src=x onerror=alert(1)>');
    expect(pagina.querySelector('img')).toBeNull();
  });
});
