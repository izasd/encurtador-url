import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, signal } from '@angular/core';
import { CliquesPorDia } from '../../nucleo/api';
import { marcasDoEixo } from '../graficos/dados';

const ALTURA = 240;
const MARGEM = { topo: 28, direita: 16, base: 32, esquerda: 44 };
const LARGURA_POR_ROTULO_X = 80;

const dataPorExtenso = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC' });
const porExtenso = (dia: string) => dataPorExtenso.format(new Date(`${dia}T00:00:00Z`));

/**
 * Gráfico de área com os cliques de cada dia. O cursor encosta no dia mais próximo do ponteiro
 * (ou das setas do teclado) e mostra o valor; o botão "Ver tabela" mostra os mesmos dados em texto.
 */
@Component({
  selector: 'app-grafico-diario',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './grafico-diario.html',
  styleUrl: './grafico-diario.css',
})
export class GraficoDiario {
  readonly titulo = input.required<string>();
  /** Todos os dias do período, em ordem, inclusive os sem cliques (ver preencherDias). */
  readonly dias = input.required<CliquesPorDia[]>();

  protected readonly altura = ALTURA;
  protected readonly largura = signal(640);
  protected readonly ativo = signal<number | null>(null);
  protected readonly mostrarTabela = signal(false);

  constructor() {
    // O SVG é desenhado na largura real do cartão, para o texto não ser esticado nem encolhido.
    const elemento = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    const destruicao = inject(DestroyRef);

    afterNextRender(() => {
      if (typeof ResizeObserver === 'undefined') {
        return;
      }

      const observador = new ResizeObserver(([entrada]) =>
        this.largura.set(Math.max(280, Math.round(entrada.contentRect.width))),
      );
      observador.observe(elemento);
      destruicao.onDestroy(() => observador.disconnect());
    });
  }

  protected readonly escala = computed(() => {
    const dias = this.dias();
    const marcas = marcasDoEixo(Math.max(0, ...dias.map((d) => d.cliques)));
    const topo = marcas[marcas.length - 1];
    const x0 = MARGEM.esquerda;
    const x1 = this.largura() - MARGEM.direita;
    const y0 = ALTURA - MARGEM.base;
    const y1 = MARGEM.topo;
    const passo = dias.length > 1 ? (x1 - x0) / (dias.length - 1) : 0;

    return {
      x0, x1, y0, y1, marcas, passo,
      x: (indice: number) => (dias.length > 1 ? x0 + indice * passo : (x0 + x1) / 2),
      y: (valor: number) => y0 - (valor / topo) * (y0 - y1),
    };
  });

  protected readonly caminhos = computed(() => {
    const { x, y, y0 } = this.escala();
    const pontos = this.dias().map((d, i) => `${x(i).toFixed(1)},${y(d.cliques).toFixed(1)}`);
    if (pontos.length === 0) {
      return { linha: '', area: '' };
    }

    return {
      linha: `M${pontos.join('L')}`,
      area: `M${x(0).toFixed(1)},${y0}L${pontos.join('L')}L${x(pontos.length - 1).toFixed(1)},${y0}Z`,
    };
  });

  /** Datas do eixo X: tantas quantas couberem, igualmente espaçadas, sempre com a primeira e a última. */
  protected readonly rotulosX = computed(() => {
    const dias = this.dias();
    const total = dias.length;
    const { x0, x1 } = this.escala();
    const cabem = Math.max(2, Math.floor((x1 - x0) / LARGURA_POR_ROTULO_X));
    const intervalo = Math.max(1, Math.ceil((total - 1) / (cabem - 1)));

    const indices: number[] = [];
    for (let i = 0; i < total; i += intervalo) {
      indices.push(i);
    }

    const ultimo = total - 1;
    if (indices[indices.length - 1] !== ultimo) {
      // A última data entra no lugar da penúltima se as duas ficariam encostadas.
      if (ultimo - indices[indices.length - 1] < intervalo / 2) {
        indices.pop();
      }
      indices.push(ultimo);
    }

    return indices.map((i) => ({ indice: i, dia: dias[i].dia, ancora: this.ancora(i) }));
  });

  /** Rótulo seletivo: só o dia de pico recebe o número (o mais recente, se houver empate). */
  protected readonly pico = computed(() => {
    let indice = -1;
    let maior = 0;
    this.dias().forEach((d, i) => {
      if (d.cliques > 0 && d.cliques >= maior) {
        maior = d.cliques;
        indice = i;
      }
    });

    return indice < 0 ? null : { indice, valor: maior, ancora: this.ancora(indice) };
  });

  protected readonly pontoAtivo = computed(() => {
    const indice = this.ativo();
    if (indice === null) {
      return null;
    }

    const dia = this.dias()[indice];
    const { x, y } = this.escala();
    return { ...dia, x: x(indice), y: y(dia.cliques) };
  });

  protected readonly descricao = computed(() => {
    const dias = this.dias();
    const total = dias.reduce((soma, d) => soma + d.cliques, 0);
    return `${this.titulo()}: ${total} cliques entre ${porExtenso(dias[0].dia)} e ${porExtenso(dias[dias.length - 1].dia)}. ` +
      'Use as setas para ouvir os valores de cada dia, ou abra a tabela.';
  });

  protected readonly anuncio = computed(() => {
    const ponto = this.pontoAtivo();
    return ponto ? `${porExtenso(ponto.dia)}: ${ponto.cliques} ${ponto.cliques === 1 ? 'clique' : 'cliques'}` : '';
  });

  protected mover(evento: PointerEvent): void {
    const { x0, passo } = this.escala();
    const posicao = evento.clientX - (evento.currentTarget as Element).getBoundingClientRect().left;
    const indice = passo > 0 ? Math.round((posicao - x0) / passo) : 0;
    this.ativo.set(this.limitarIndice(indice));
  }

  protected tecla(evento: KeyboardEvent): void {
    const ultimo = this.dias().length - 1;
    const atual = this.ativo() ?? ultimo;
    const destinos: Record<string, number> = { ArrowLeft: atual - 1, ArrowRight: atual + 1, Home: 0, End: ultimo };
    const destino = destinos[evento.key];

    if (destino !== undefined) {
      evento.preventDefault();
      this.ativo.set(this.limitarIndice(destino));
    }
  }

  protected focar(): void {
    if (this.ativo() === null) {
      this.ativo.set(this.dias().length - 1);
    }
  }

  /** Linhas de 1px ficam nítidas quando passam pelo meio do pixel. */
  protected nitida(coordenada: number): number {
    return Math.round(coordenada) + 0.5;
  }

  /** Mantém o balão inteiro dentro da área do gráfico. */
  protected posicaoDoBalao(x: number): number {
    return Math.min(Math.max(x, 72), this.largura() - 72);
  }

  private ancora(indice: number): 'start' | 'middle' | 'end' {
    const ultimo = this.dias().length - 1;
    return indice === 0 && ultimo > 0 ? 'start' : indice === ultimo && ultimo > 0 ? 'end' : 'middle';
  }

  private limitarIndice(indice: number): number {
    return Math.min(this.dias().length - 1, Math.max(0, indice));
  }
}
