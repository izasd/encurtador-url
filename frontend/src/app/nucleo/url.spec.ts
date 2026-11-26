import { destinoSeguro, enderecoDoLinkCurto, urlLegivel } from './url';

describe('destinoSeguro', () => {
  it.each(['/links', '/links/abc123/estatisticas'])('aceita o caminho interno %s', (caminho) => {
    expect(destinoSeguro(caminho)).toBe(caminho);
  });

  it.each([null, undefined, '', 'https://site-falso.com', '//site-falso.com', '/\\site-falso.com', 'javascript:alert(1)'])(
    'troca %s pela página de links',
    (destino) => {
      expect(destinoSeguro(destino)).toBe('/links');
    },
  );
});

describe('urlLegivel', () => {
  it('volta os acentos e espaços que a API codifica', () => {
    expect(urlLegivel('https://exemplo.com/promo%C3%A7%C3%A3o%20de%20ver%C3%A3o')).toBe('https://exemplo.com/promoção de verão');
  });

  it('devolve a URL como está quando a codificação é inválida', () => {
    expect(urlLegivel('https://exemplo.com/%E0%A4%A')).toBe('https://exemplo.com/%E0%A4%A');
  });
});

describe('enderecoDoLinkCurto', () => {
  it('tira o protocolo e o código, deixando o endereço com a barra', () => {
    expect(enderecoDoLinkCurto('https://encurtador-4qz0.onrender.com/6Le97y', '6Le97y')).toBe('encurtador-4qz0.onrender.com/');
    expect(enderecoDoLinkCurto('http://localhost:5198/abc123', 'abc123')).toBe('localhost:5198/');
  });

  it('não repete o código quando o link curto não termina nele', () => {
    expect(enderecoDoLinkCurto('https://encurtador.com/outro', 'abc123')).toBe('');
  });
});
