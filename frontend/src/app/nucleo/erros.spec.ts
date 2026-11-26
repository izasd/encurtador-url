import { HttpErrorResponse } from '@angular/common/http';
import { lerErros } from './erros';

describe('lerErros', () => {
  it('usa a primeira mensagem de cada campo quando a API devolve erros de validação', () => {
    const erro = new HttpErrorResponse({
      status: 400,
      error: { title: 'One or more validation errors occurred.', errors: { email: ['Informe um e-mail válido.'], senha: ['Curta demais.', 'Outra'] } },
    });

    expect(lerErros(erro)).toEqual({
      geral: null,
      campos: { email: 'Informe um e-mail válido.', senha: 'Curta demais.' },
    });
  });

  it('usa o "detail" do ProblemDetails como mensagem geral', () => {
    const erro = new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Já existe uma conta com este e-mail.' } });

    expect(lerErros(erro)).toEqual({ geral: 'Já existe uma conta com este e-mail.', campos: {} });
  });

  it('avisa quando não consegue falar com o servidor', () => {
    expect(lerErros(new HttpErrorResponse({ status: 0 })).geral).toContain('Não foi possível falar com o servidor');
  });

  it('usa a mensagem padrão quando a resposta não tem corpo ou não é um erro HTTP', () => {
    expect(lerErros(new HttpErrorResponse({ status: 500, error: null }), 'Falhou.').geral).toBe('Falhou.');
    expect(lerErros(new Error('qualquer'), 'Falhou.').geral).toBe('Falhou.');
  });
});
