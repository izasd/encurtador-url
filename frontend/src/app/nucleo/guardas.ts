import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Sessao } from './sessao';

/** Páginas que exigem login: sem sessão válida, vai para o login e volta depois. */
export const somenteAutenticado: CanActivateFn = (_rota, estado) => {
  const sessao = inject(Sessao);
  if (sessao.valida()) {
    return true;
  }

  sessao.sair();
  return inject(Router).createUrlTree(['/entrar'], { queryParams: { voltar: estado.url } });
};

/** Login e cadastro: quem já entrou vai direto para os links. */
export const somenteAnonimo: CanActivateFn = () =>
  inject(Sessao).valida() ? inject(Router).createUrlTree(['/links']) : true;
