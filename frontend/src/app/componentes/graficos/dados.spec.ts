import { formatarPercentual, hojeNoFuso, marcasDoEixo, nomeDoPais, prepararBarras, preencherDias } from './dados';

describe('marcasDoEixo', () => {
  it.each([
    [19, [0, 5, 10, 15, 20]],
    [140, [0, 50, 100, 150]],
    [7, [0, 2, 4, 6, 8]],
    [3, [0, 1, 2, 3]],
    [1, [0, 1]],
    [0, [0, 1]],
  ])('para o máximo %d usa %j', (maximo, esperado) => {
    expect(marcasDoEixo(maximo)).toEqual(esperado);
  });
});

describe('hojeNoFuso', () => {
  it('usa a data do fuso pedido, não a de UTC', () => {
    // 01:30 UTC de 25/11 ainda é 22:30 de 24/11 em São Paulo.
    const instante = new Date('2026-11-25T01:30:00Z');

    expect(hojeNoFuso('UTC', instante)).toBe('2026-11-25');
    expect(hojeNoFuso('America/Sao_Paulo', instante)).toBe('2026-11-24');
  });
});

describe('preencherDias', () => {
  it('completa com zero os dias sem cliques até hoje', () => {
    const dias = preencherDias(
      [
        { dia: '2026-11-10', cliques: 4 },
        { dia: '2026-11-12', cliques: 1 },
      ],
      '2026-11-17',
    );

    expect(dias.map((d) => d.dia)).toEqual([
      '2026-11-10', '2026-11-11', '2026-11-12', '2026-11-13', '2026-11-14', '2026-11-15', '2026-11-16', '2026-11-17',
    ]);
    expect(dias.map((d) => d.cliques)).toEqual([4, 0, 1, 0, 0, 0, 0, 0]);
  });

  it('mostra pelo menos 7 dias, mesmo com cliques só hoje', () => {
    const dias = preencherDias([{ dia: '2026-11-25', cliques: 3 }], '2026-11-25');

    expect(dias).toHaveLength(7);
    expect(dias[0].dia).toBe('2026-11-19');
    expect(dias.at(-1)).toEqual({ dia: '2026-11-25', cliques: 3 });
  });

  it('atravessa a virada do mês', () => {
    const dias = preencherDias([{ dia: '2026-10-30', cliques: 1 }], '2026-11-02');

    expect(dias.map((d) => d.dia)).toContain('2026-10-31');
    expect(dias.map((d) => d.dia)).toContain('2026-11-01');
  });
});

describe('prepararBarras', () => {
  it('ordena da maior para a menor e calcula o comprimento relativo à maior', () => {
    const barras = prepararBarras([{ nome: 'Safari', cliques: 25 }, { nome: 'Chrome', cliques: 50 }], 75);

    expect(barras.map((b) => [b.nome, b.largura, b.percentual])).toEqual([
      ['Chrome', 100, '67%'],
      ['Safari', 50, '33%'],
    ]);
  });

  it('com mais de 7 categorias, mantém as 6 maiores e soma o resto em "Outros", por último', () => {
    const categorias = Array.from({ length: 9 }, (_, i) => ({ nome: `site${i}`, cliques: 90 - i * 10 }));

    const barras = prepararBarras(categorias, 450);

    expect(barras).toHaveLength(7);
    expect(barras.at(-1)).toMatchObject({ nome: 'Outros', cliques: 30 + 20 + 10, ehOutros: true });
  });
});

describe('formatarPercentual', () => {
  it.each([
    [1, 3, '33%'],
    [1, 200, '<1%'],
    [0, 10, '0%'],
    [5, 0, '0%'],
  ])('%d de %d vira %s', (parte, total, esperado) => {
    expect(formatarPercentual(parte, total)).toBe(esperado);
  });
});

describe('nomeDoPais', () => {
  it('traduz códigos ISO para o nome em português e mantém outros textos', () => {
    expect(nomeDoPais('BR')).toBe('Brasil');
    expect(nomeDoPais('US')).toBe('Estados Unidos');
    expect(nomeDoPais('Desconhecido')).toBe('Desconhecido');
  });
});
