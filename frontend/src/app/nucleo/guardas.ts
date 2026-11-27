import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Sessao } from './sessao';
import { DESTINO_PADRAO } from './url';

/**
 * Páginas que exigem login: sem sessão válida, vai para o login e volta depois. O destino só
 * vai no endereço (?voltar=) quando não é a página de links, para onde o login já leva.
 */
export const somenteAutenticado: CanActivateFn = (_rota, estado) => {
  const sessao = inject(Sessao);
  if (sessao.valida()) {
    return true;
  }

  sessao.sair();
  const queryParams = estado.url === DESTINO_PADRAO ? {} : { voltar: estado.url };
  return inject(Router).createUrlTree(['/entrar'], { queryParams });
};

/** Login e cadastro: quem já entrou vai direto para os links. */
export const somenteAnonimo: CanActivateFn = () =>
  inject(Sessao).valida() ? inject(Router).createUrlTree([DESTINO_PADRAO]) : true;
