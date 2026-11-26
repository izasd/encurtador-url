import { DOCUMENT } from '@angular/common';
import { Component, inject } from '@angular/core';

/**
 * Demonstração do que o site faz, nas telas de login e cadastro: uma URL longa é "selecionada"
 * e vira um link curto. É a única animação automática do site e roda uma vez ao abrir a página.
 * O estilo sem animação já é o estado final, então quem pede menos movimento vê o resultado direto.
 */
@Component({
  selector: 'app-demonstracao',
  template: `
    <figure>
      <div class="exemplo" aria-hidden="true">
        <p class="longa"><span>{{ urlLonga }}</span></p>
        <p class="link-curto curta"><span class="endereco">{{ endereco }}</span><span class="codigo">{{ codigo }}</span></p>
      </div>
      <figcaption>
        Encurte links, escolha o código se quiser e veja quem clicou: em que dia, de que país,
        em qual navegador e aparelho.
      </figcaption>
    </figure>
  `,
  styles: `
    figure {
      display: grid;
      gap: 2rem;
      margin: 0;
    }

    .exemplo {
      display: grid;
      gap: 1.25rem;
    }

    /* A URL longa: pequena, quebrando em várias linhas, apagada no estado final. */
    .longa {
      max-width: 34rem;
      font-size: 1.125rem;
      line-height: 1.6;
      color: var(--texto-suave);
      overflow-wrap: anywhere;
      animation: apagar 1.6s 0.2s both;
    }

    /* A "seleção" percorre as linhas em sequência (o fundo de um elemento inline é contínuo). */
    .longa span {
      background: linear-gradient(var(--selecao), var(--selecao)) no-repeat left bottom / 100% 0%;
      animation: selecionar 1.6s 0.2s cubic-bezier(0.6, 0, 0.3, 1) both;
    }

    .curta {
      animation: surgir 0.45s 1.5s cubic-bezier(0.2, 0.7, 0.2, 1) both;
    }

    .curta .endereco {
      display: block;
      font-size: 1rem;
    }

    /* O sublinhado é desenhado pelo fundo, para poder crescer da esquerda para a direita. */
    .curta .codigo {
      font-size: clamp(3rem, 9vw, 4.5rem);
      text-decoration: none;
      background: linear-gradient(currentColor, currentColor) no-repeat left bottom 0.02em / 100% 0.08em;
      animation: sublinhar 0.5s 1.8s ease-out both;
    }

    figcaption {
      max-width: 30rem;
      font-size: 1.3125rem;
      line-height: 1.45;
      color: var(--texto-secundario);
    }

    @keyframes selecionar {
      0% { background-size: 0% 100%; }
      55%, 75% { background-size: 100% 100%; }
      100% { background-size: 100% 0%; }
    }

    @keyframes apagar {
      0%, 75% { color: var(--texto); }
    }

    @keyframes surgir {
      from { opacity: 0; transform: translateY(0.75rem); }
    }

    @keyframes sublinhar {
      from { background-size: 0% 0.08em; }
    }

    @media (max-width: 52rem) {
      figure {
        gap: 1.25rem;
      }

      .longa {
        font-size: 0.9375rem;
      }

      figcaption {
        font-size: 1.125rem;
      }
    }
  `,
})
export class Demonstracao {
  protected readonly urlLonga =
    'https://www.exemplo.com.br/blog/2026/11/como-acompanhar-os-cliques-dos-seus-links' +
    '?utm_source=newsletter&utm_medium=email&utm_campaign=novembro&utm_content=botao-principal';

  /** O endereço deste próprio site, como num link curto de verdade. */
  protected readonly endereco = `${inject(DOCUMENT).location.host}/`;

  protected readonly codigo = 'Hq7l0z';
}
