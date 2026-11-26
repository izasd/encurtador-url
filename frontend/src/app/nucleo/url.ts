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
 * Parte do link curto que vem antes do código, sem o protocolo, para exibir o endereço
 * pequeno e o código grande: "https://encurtador.com/abc123" vira "encurtador.com/".
 */
export function enderecoDoLinkCurto(urlCurta: string, codigo: string): string {
  const semProtocolo = urlCurta.replace(/^https?:\/\//i, '');
  return semProtocolo.endsWith(`/${codigo}`) ? semProtocolo.slice(0, -codigo.length) : '';
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
