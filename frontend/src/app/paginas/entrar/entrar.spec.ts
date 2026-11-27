import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import type { MockInstance } from 'vitest';
import { Sessao } from '../../nucleo/sessao';
import { Entrar } from './entrar';

function preencher(pagina: HTMLElement, seletor: string, valor: string): void {
  const campo = pagina.querySelector<HTMLInputElement>(seletor)!;
  campo.value = valor;
  campo.dispatchEvent(new Event('input'));
}

describe('Entrar', () => {
  const sessao = { entrar: vi.fn(), entrarComoVisitante: vi.fn() };
  let pagina: HTMLElement;
  let navegar: MockInstance<Router['navigateByUrl']>;

  beforeEach(async () => {
    sessao.entrar.mockReset();
    sessao.entrarComoVisitante.mockReset();

    await TestBed.configureTestingModule({
      imports: [Entrar],
      providers: [provideRouter([]), { provide: Sessao, useValue: sessao }],
    }).compileComponents();

    navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(Entrar);
    await fixture.whenStable();
    pagina = fixture.nativeElement as HTMLElement;
  });

  it('entra com e-mail e senha e vai para os links', async () => {
    sessao.entrar.mockResolvedValue(undefined);

    preencher(pagina, '#email', 'ana@exemplo.com');
    preencher(pagina, '#senha', 'senha-forte-123');
    pagina.querySelector('form')!.dispatchEvent(new Event('submit'));

    await vi.waitFor(() => expect(navegar).toHaveBeenCalledWith('/links'));
    expect(sessao.entrar).toHaveBeenCalledWith('ana@exemplo.com', 'senha-forte-123');
  });

  it('não chama a API com o formulário vazio', () => {
    pagina.querySelector('form')!.dispatchEvent(new Event('submit'));

    expect(sessao.entrar).not.toHaveBeenCalled();
  });

  it('mostra a mensagem da API quando o login falha', async () => {
    sessao.entrar.mockRejectedValue(
      new HttpErrorResponse({ status: 401, error: { detail: 'E-mail ou senha incorretos.' } }),
    );

    preencher(pagina, '#email', 'ana@exemplo.com');
    preencher(pagina, '#senha', 'senha-errada');
    pagina.querySelector('form')!.dispatchEvent(new Event('submit'));

    await vi.waitFor(() =>
      expect(pagina.querySelector('[role="alert"]')?.textContent).toContain('E-mail ou senha incorretos.'),
    );
    expect(navegar).not.toHaveBeenCalled();
  });

  it('entra como visitante', async () => {
    sessao.entrarComoVisitante.mockResolvedValue(undefined);

    pagina.querySelector<HTMLButtonElement>('button[type="button"]')!.click();

    await vi.waitFor(() => expect(navegar).toHaveBeenCalledWith('/links'));
    expect(sessao.entrarComoVisitante).toHaveBeenCalled();
  });

  it('mostra o erro da conta de visitante junto do botão de visitante, não no formulário', async () => {
    sessao.entrarComoVisitante.mockRejectedValue(
      new HttpErrorResponse({ status: 429, error: { detail: 'Muitas contas de visitante. Tente de novo em 1 hora.' } }),
    );

    pagina.querySelector<HTMLButtonElement>('.visitante button')!.click();

    await vi.waitFor(() =>
      expect(pagina.querySelector('.visitante [role="alert"]')?.textContent).toContain('Tente de novo em 1 hora.'),
    );
    expect(pagina.querySelector('form [role="alert"]')).toBeNull();
    expect(navegar).not.toHaveBeenCalled();
  });
});
