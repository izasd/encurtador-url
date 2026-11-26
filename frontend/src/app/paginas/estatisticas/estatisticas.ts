import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse, httpResource } from '@angular/common/http';
import { Component, computed, effect, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GraficoBarras } from '../../componentes/grafico-barras/grafico-barras';
import { GraficoDiario } from '../../componentes/grafico-diario/grafico-diario';
import { hojeNoFuso, nomeDoPais, preencherDias } from '../../componentes/graficos/dados';
import { Estatisticas as DadosDeEstatisticas } from '../../nucleo/api';
import { enderecoDoLinkCurto, urlLegivel } from '../../nucleo/url';

@Component({
  selector: 'app-estatisticas',
  imports: [DecimalPipe, RouterLink, GraficoDiario, GraficoBarras],
  templateUrl: './estatisticas.html',
  styleUrl: './estatisticas.css',
})
export class Estatisticas {
  /** Vem do parâmetro :codigo da rota. */
  readonly codigo = input.required<string>();

  /** Os dias são separados no fuso do navegador (ex.: America/Sao_Paulo). */
  protected readonly fusoHorario = signal(Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC');

  protected readonly dados = httpResource<DadosDeEstatisticas>(() => ({
    url: `/api/links/${encodeURIComponent(this.codigo())}/estatisticas`,
    params: { fusoHorario: this.fusoHorario() },
  }));

  protected readonly dias = computed(() => {
    const dados = this.dados.value();
    return dados ? preencherDias(dados.porDia, hojeNoFuso(dados.fusoHorario)) : [];
  });

  protected readonly paises = computed(() =>
    (this.dados.value()?.porPais ?? []).map((pais) => ({ ...pais, nome: nomeDoPais(pais.nome) })),
  );

  protected readonly urlOriginal = computed(() => urlLegivel(this.dados.value()?.urlOriginal ?? ''));

  protected readonly endereco = computed(() => {
    const dados = this.dados.value();
    return dados ? enderecoDoLinkCurto(dados.urlCurta, dados.codigo) : '';
  });

  protected readonly naoEncontrado = computed(() => {
    const erro = this.dados.error();
    return erro instanceof HttpErrorResponse && erro.status === 404;
  });

  constructor() {
    // Se o banco não reconhecer o fuso do navegador (400), mostra os dias em UTC.
    effect(() => {
      const erro = this.dados.error();
      if (erro instanceof HttpErrorResponse && erro.status === 400 && this.fusoHorario() !== 'UTC') {
        this.fusoHorario.set('UTC');
      }
    });
  }
}
