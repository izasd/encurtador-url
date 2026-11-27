import { Component } from '@angular/core';

/** O que o site faz, ao lado dos formulários de login e cadastro. */
@Component({
  selector: 'app-apresentacao',
  template: `
    <p class="frase">Encurte links e veja quem clicou.</p>

    <dl class="recursos">
      <div>
        <dt>Código seu ou gerado</dt>
        <dd>De 4 a 10 letras e números, ou 6 caracteres sorteados.</dd>
      </div>
      <div>
        <dt>QR Code em PNG</dt>
        <dd>Baixe a imagem para imprimir ou compartilhar.</dd>
      </div>
      <div>
        <dt>Estatísticas de cada clique</dt>
        <dd>Por dia, país, navegador, aparelho e site de origem.</dd>
      </div>
    </dl>
  `,
  styles: `
    :host {
      display: grid;
      align-content: start;
      gap: 2rem;
    }

    .frase {
      max-width: 22rem;
      font-size: 2rem;
      font-weight: 500;
      line-height: 1.25;
      letter-spacing: -0.01em;
    }

    .recursos {
      display: grid;
      gap: 1.25rem;
      margin: 0;
    }

    dt {
      font-weight: 700;
    }

    dd {
      margin: 0;
      color: var(--texto-secundario);
    }

    @media (max-width: 52rem) {
      :host {
        gap: 1.25rem;
      }

      .frase {
        font-size: 1.375rem;
      }
    }
  `,
})
export class Apresentacao {}
