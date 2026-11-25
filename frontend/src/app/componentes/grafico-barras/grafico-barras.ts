import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { CliquesPorCategoria } from '../../nucleo/api';
import { prepararBarras } from '../graficos/dados';

/**
 * Barras horizontais em ordem decrescente, numa cor só (as categorias não têm ordem própria).
 * Cada linha traz o nome, a barra e o número, então nenhum valor depende de passar o mouse.
 */
@Component({
  selector: 'app-grafico-barras',
  imports: [DecimalPipe],
  template: `
    <h2 class="titulo">{{ titulo() }}</h2>

    <ul class="barras">
      @for (item of itens(); track item.nome) {
        <li class="barra" [class.outros]="item.ehOutros">
          <span class="nome" [title]="item.nome">{{ item.nome }}</span>
          <span class="trilho"><span class="valor" [style.width.%]="item.largura"></span></span>
          <span class="numeros">
            <strong>{{ item.cliques | number }}</strong>
            <span class="visualmente-oculto">{{ item.cliques === 1 ? 'clique' : 'cliques' }},</span>
            {{ item.percentual }}
          </span>
        </li>
      }
    </ul>
  `,
  styleUrl: './grafico-barras.css',
})
export class GraficoBarras {
  readonly titulo = input.required<string>();
  readonly categorias = input.required<CliquesPorCategoria[]>();
  readonly total = input.required<number>();

  protected readonly itens = computed(() => prepararBarras(this.categorias(), this.total()));
}
