import {
  fmtCompactEth,
  fmtCompactNum,
  fmtCompactUsd,
  fmtEth,
  fmtGwei,
  fmtUsd,
} from './format';
import type { MetricId } from '../types/contract';

/**
 * Catálogo das métricas da aba Historical Fees — o que cada uma mede e como se
 * exibe. Rótulo e formatação são decisão do front; o VALOR vem pronto da API.
 *
 * As cinco espelham as métricas de taxa da plataforma do parceiro, traduzidas
 * para Ethereum (ADR-003). A tradução não-óbvia é a última: no Bitcoin a
 * métrica é "fee por byte" porque o blockspace é medido em bytes; no Ethereum
 * o blockspace é medido em **gas**, então o equivalente é fee por unidade de
 * gas — que é o gwei. É o mesmo conceito do sat/vB, na unidade certa.
 */

export interface MetricDef {
  id: MetricId;
  /** Rótulo na navegação lateral. */
  nav: string;
  /** Título da série. */
  label: string;
  description: string;
  /** Cabeçalho da coluna no CSV. */
  csvHeader: string;
  /** Eixo Y e valor em destaque. */
  format: (v: number) => string;
  /** Versão curta para os ticks do eixo. */
  formatAxis: (v: number) => string;
  /** Agregado (soma do intervalo) ou média por transação/gas. */
  kind: 'total' | 'mean';
}

export const METRICS: readonly MetricDef[] = [
  {
    id: 'total-fees-eth',
    nav: 'Total Fees',
    label: 'Total Fees (ETH)',
    description:
      'Estimativa das taxas pagas na rede no intervalo, recomposta da base fee queimada e da priority fee média do rollup.',
    csvHeader: 'total_fees_eth',
    format: fmtCompactEth,
    formatAxis: fmtCompactNum,
    kind: 'total',
  },
  {
    id: 'total-fees-usd',
    nav: 'Total Fees (USD)',
    label: 'Total Fees (USD)',
    description:
      'A mesma estimativa, convertida a dólar. Mistura dois efeitos: mais taxa ou ETH mais caro.',
    csvHeader: 'total_fees_usd',
    format: fmtCompactUsd,
    formatAxis: fmtCompactUsd,
    kind: 'total',
  },
  {
    id: 'mean-tx-fee-eth',
    nav: 'Mean Tx Fee',
    label: 'Mean Tx Fee (ETH)',
    description:
      'Estimativa da taxa média por transação (taxas estimadas ÷ nº de transações).',
    csvHeader: 'mean_tx_fee_eth',
    format: fmtEth,
    formatAxis: fmtCompactNum,
    kind: 'mean',
  },
  {
    id: 'mean-tx-fee-usd',
    nav: 'Mean Tx Fee (USD)',
    label: 'Mean Tx Fee (USD)',
    description:
      'A mesma média, em dólar. É o número que o usuário final sente no bolso.',
    csvHeader: 'mean_tx_fee_usd',
    format: (v) => fmtUsd(v),
    formatAxis: (v) => `$${v.toFixed(2)}`,
    kind: 'mean',
  },
  {
    id: 'mean-fee-per-gas',
    nav: 'Mean Fee per Gas',
    label: 'Mean Fee per Gas (gwei)',
    description:
      'Preço médio de uma unidade de gas. Divide pelo peso da transação, não pela contagem: remove o viés de transações maiores e mostra o preço puro do blockspace. É o equivalente Ethereum do sat/vB do Bitcoin.',
    csvHeader: 'mean_fee_per_gas_gwei',
    format: (v) => `${fmtGwei(v)} gwei`,
    formatAxis: fmtCompactNum,
    kind: 'mean',
  },
];

/** Métrica exibida por padrão — a primeira da navegação. */
export const DEFAULT_METRIC: MetricDef = METRICS[0]!;

/** Busca tolerante: a rota vem da URL, então o id pode não existir. */
export function findMetric(id: string | undefined): MetricDef | undefined {
  return METRICS.find((m) => m.id === id);
}

/** Cada métrica é uma rota própria, e o id é legível na URL. */
export function metricPath(id: MetricId): string {
  return `/metrics/${id}`;
}
