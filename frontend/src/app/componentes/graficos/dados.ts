import { CliquesPorCategoria, CliquesPorDia } from '../../nucleo/api';

const UM_DIA_EM_MS = 24 * 60 * 60 * 1000;

/** Marcas do eixo Y em números redondos (passos de 1, 2 ou 5 × 10ⁿ), de zero até cobrir o máximo. */
export function marcasDoEixo(maximo: number, quantidadeDesejada = 4): number[] {
  if (maximo <= 0) {
    return [0, 1];
  }

  const passoBruto = maximo / quantidadeDesejada;
  const potencia = 10 ** Math.floor(Math.log10(passoBruto));
  const passoRedondo = [1, 2, 5, 10].map((m) => m * potencia).find((p) => p >= passoBruto) ?? passoBruto;
  const passo = Math.max(1, passoRedondo); // cliques são inteiros: nada de 0,5 no eixo
  const topo = Math.ceil(maximo / passo) * passo;

  return Array.from({ length: Math.round(topo / passo) + 1 }, (_, i) => i * passo);
}

/** Dia de hoje (aaaa-mm-dd) no fuso informado. */
export function hojeNoFuso(fuso: string, agora = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: fuso, year: 'numeric', month: '2-digit', day: '2-digit' }).format(agora);
}

function somarDias(dia: string, quantidade: number): string {
  return new Date(Date.parse(`${dia}T00:00:00Z`) + quantidade * UM_DIA_EM_MS).toISOString().slice(0, 10);
}

/**
 * A API só devolve os dias que tiveram cliques; o gráfico precisa de todos os dias do período,
 * com zero nos dias sem cliques. O período vai do primeiro clique até hoje, com no mínimo 7 dias.
 */
export function preencherDias(porDia: CliquesPorDia[], hoje: string, minimoDeDias = 7): CliquesPorDia[] {
  const cliquesPorData = new Map(porDia.map((d) => [d.dia, d.cliques]));
  const datas = [...cliquesPorData.keys(), hoje, somarDias(hoje, -(minimoDeDias - 1))].sort();
  const ultimo = datas[datas.length - 1];

  const dias: CliquesPorDia[] = [];
  for (let dia = datas[0]; dia <= ultimo; dia = somarDias(dia, 1)) {
    dias.push({ dia, cliques: cliquesPorData.get(dia) ?? 0 });
  }

  return dias;
}

export interface ItemDeBarra {
  nome: string;
  cliques: number;
  percentual: string;
  /** Comprimento da barra em %, relativo à maior barra. */
  largura: number;
  ehOutros: boolean;
}

/**
 * Barras em ordem decrescente. Até 7 categorias aparecem como estão; acima disso, as 6 maiores
 * ficam e o restante é somado em "Outros", sempre por último.
 */
export function prepararBarras(categorias: CliquesPorCategoria[], total: number, maximoDeBarras = 7): ItemDeBarra[] {
  const ordenadas = [...categorias].sort((a, b) => b.cliques - a.cliques || a.nome.localeCompare(b.nome, 'pt-BR'));
  const visiveis = ordenadas.length > maximoDeBarras ? ordenadas.slice(0, maximoDeBarras - 1) : ordenadas;
  const resto = ordenadas.slice(visiveis.length);

  const linhas = visiveis.map((c) => ({ nome: c.nome, cliques: c.cliques, ehOutros: false }));
  if (resto.length > 0) {
    linhas.push({ nome: 'Outros', cliques: resto.reduce((soma, c) => soma + c.cliques, 0), ehOutros: true });
  }

  const maior = Math.max(1, ...linhas.map((l) => l.cliques));
  return linhas.map((l) => ({ ...l, largura: (l.cliques / maior) * 100, percentual: formatarPercentual(l.cliques, total) }));
}

export function formatarPercentual(parte: number, total: number): string {
  if (total <= 0 || parte <= 0) {
    return '0%';
  }

  const percentual = (parte / total) * 100;
  return percentual < 1 ? '<1%' : `${Math.round(percentual)}%`;
}

const nomesDePaises = new Intl.DisplayNames(['pt-BR'], { type: 'region' });

/** Nome do país em português a partir do código ISO ("BR" vira "Brasil"); outros textos ficam como estão. */
export function nomeDoPais(codigo: string): string {
  if (!/^[A-Z]{2}$/.test(codigo)) {
    return codigo;
  }

  try {
    return nomesDePaises.of(codigo) ?? codigo;
  } catch {
    return codigo;
  }
}
