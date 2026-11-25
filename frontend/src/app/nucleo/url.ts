/**
 * Destino depois do login: só aceita caminhos internos ("/links/...") para que um link
 * malicioso como "/entrar?voltar=https://site-falso.com" não leve o usuário para fora do app.
 */
export function destinoSeguro(voltar: string | null | undefined): string {
  return voltar && voltar.startsWith('/') && !voltar.startsWith('//') && !voltar.startsWith('/\\')
    ? voltar
    : '/links';
}

/**
 * A API guarda as URLs codificadas ("promo%C3%A7%C3%A3o"); para exibir, volta os acentos
 * e espaços ("promoção"). O resultado só é usado como texto, nunca como HTML.
 */
export function urlLegivel(url: string): string {
  try {
    return decodeURI(url);
  } catch {
    return url;
  }
}
