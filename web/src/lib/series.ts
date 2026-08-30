import type { MetricPoint, MetricSeries } from '../types/contract';

/**
 * Transformações de SÉRIE para a aba Historical Fees: suavização e taxa de
 * variação.
 *
 * ► Isto não viola a regra "nenhum cálculo de taxa no front". A proibição é
 *   sobre derivar valores de taxa a partir de wei — o que continua sendo
 *   exclusividade do Service da API (RN-09). Aqui a série já chega calculada
 *   e o que se faz é reapresentá-la (média móvel, variação percentual), do
 *   mesmo tipo de operação que arredondar casas decimais.
 *
 * ► Em produção estas transformações podem migrar para a API: o ClickHouse tem
 *   janelas móveis nativas, e mover para lá evita trafegar a série crua. Ficam
 *   no cliente enquanto a API não existe. A troca não muda a interface: os
 *   controles continuam iguais, só param de transformar localmente.
 */

/**
 * Suavização — janelas em DIAS. As janelas longas são úteis nas visualizações
 * de 1Y e ALL e equivalem às lentes disponíveis na plataforma de referência.
 */
export type SmoothingKind = 'none' | 'sma' | 'ema';
export const SMOOTHING_WINDOWS = [7, 14, 30, 90, 180, 365] as const;

/**
 * Taxa de variação — comparação com o mesmo ponto N dias atrás.
 */
export type RateOfChange = 'none' | 'wow' | 'mom' | 'qoq' | 'yoy';

export const ROC_DAYS: Record<Exclude<RateOfChange, 'none'>, number> = {
  wow: 7,
  mom: 30,
  qoq: 90,
  yoy: 365,
};

export const ROC_LABEL: Record<RateOfChange, string> = {
  none: 'None',
  wow: 'WoW (7d)',
  mom: 'MoM (30d)',
  qoq: 'QoQ (90d)',
  yoy: 'YoY (365d)',
};

/** Quantos buckets equivalem a N dias, na resolução da série. */
function bucketsPerDay(resolution: MetricSeries['resolution']): number {
  return resolution === '1h' ? 24 : 1;
}

/** Média móvel simples. Descarta o começo até a janela encher. */
export function sma(points: readonly MetricPoint[], window: number): MetricPoint[] {
  if (window <= 1) return [...points];
  const out: MetricPoint[] = [];
  let sum = 0;
  for (let i = 0; i < points.length; i++) {
    const p = points[i];
    if (!p) continue;
    sum += p.value;
    if (i >= window) {
      const dropped = points[i - window];
      if (dropped) sum -= dropped.value;
    }
    if (i >= window - 1) out.push({ ...p, value: sum / window });
  }
  return out;
}

/** Média móvel exponencial. Não descarta pontos — arranca no primeiro valor. */
export function ema(points: readonly MetricPoint[], window: number): MetricPoint[] {
  if (window <= 1) return [...points];
  const k = 2 / (window + 1);
  const out: MetricPoint[] = [];
  let prev: number | null = null;
  for (const p of points) {
    prev = prev === null ? p.value : p.value * k + prev * (1 - k);
    out.push({ ...p, value: prev });
  }
  return out;
}

/** Variação percentual contra o ponto de `lag` buckets atrás. */
export function rateOfChange(points: readonly MetricPoint[], lag: number): MetricPoint[] {
  if (lag <= 0) return [...points];
  const out: MetricPoint[] = [];
  for (let i = lag; i < points.length; i++) {
    const current = points[i];
    const past = points[i - lag];
    if (!current || !past || past.value === 0) continue;
    out.push({
      ...current,
      value: ((current.value - past.value) / Math.abs(past.value)) * 100,
    });
  }
  return out;
}

export interface SeriesTransform {
  smoothing: SmoothingKind;
  smoothingWindow: number;
  roc: RateOfChange;
}

/**
 * Aplica suavização e DEPOIS taxa de variação. A ordem importa: suavizar antes
 * mede a variação da tendência; inverter mediria a tendência do ruído.
 */
export function transformSeries(
  series: MetricSeries,
  { smoothing, smoothingWindow, roc }: SeriesTransform,
): MetricPoint[] {
  const perDay = bucketsPerDay(series.resolution);
  let points: MetricPoint[] = [...series.points];

  if (smoothing !== 'none') {
    const window = smoothingWindow * perDay;
    points = smoothing === 'sma' ? sma(points, window) : ema(points, window);
  }
  if (roc !== 'none') {
    points = rateOfChange(points, ROC_DAYS[roc] * perDay);
  }
  return points;
}

/** Exportação CSV (a plataforma do parceiro oferece CSV/Excel nas 5 métricas). */
export function toCsv(points: readonly MetricPoint[], valueHeader: string): string {
  const rows = [`timestamp_utc,${valueHeader},eth_usd`];
  for (const p of points) rows.push(`${p.t},${p.value},${p.ethUsd}`);
  return rows.join('\n');
}

/** Dispara o download no navegador. Só funciona em app servido, não em sandbox. */
export function downloadCsv(filename: string, csv: string): void {
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
