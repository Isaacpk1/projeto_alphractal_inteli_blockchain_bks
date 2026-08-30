import type { MetricId, MetricPoint, MetricSeries } from '../../types/contract';
import type { MetricPeriod } from '../transport';

/**
 * Séries históricas mockadas para a aba Historical Fees — faz o papel dos
 * agregados que o ClickHouse servirá (RF-37/RF-38). Quando a API existir, este
 * arquivo deixa de ser usado.
 *
 * Constrói UMA espinha dorsal diária de 10 anos e deriva as cinco métricas
 * dela, para que sejam mutuamente consistentes: Total Fees (USD) é exatamente
 * Total Fees (ETH) × preço, e Mean Tx Fee é exatamente o total ÷ nº de
 * transações. Séries incoerentes entre si seriam percebidas na hora por quem
 * conhece o dado.
 */

const DAY_MS = 86_400_000;
/** Dez anos de agregados diários para exercitar 1Y e ALL como na referência. */
const BACKBONE_DAYS = 3_650;
/** Gas médio por transação na Mainnet — transferências, swaps e mints misturados. */
const AVG_GAS_PER_TX = 88_000;

interface DailyRecord {
  ms: number;
  ethUsd: number;
  txCount: number;
  gasPriceGwei: number;
  totalFeesEth: number;
}

function gauss(): number {
  const u = Math.random() || 1e-9;
  const v = Math.random() || 1e-9;
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}

const clamp = (n: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, n));

function startOfUtcDay(ms: number): number {
  return Math.floor(ms / DAY_MS) * DAY_MS;
}

export class MockMetrics {
  private readonly daily: DailyRecord[] = [];

  /**
   * @param anchorEthUsd preço atual, para a série terminar onde o painel ao vivo está
   * @param anchorGasGwei base fee atual, idem
   */
  constructor(anchorEthUsd: number, anchorGasGwei: number) {
    const today = startOfUtcDay(Date.now());
    const days = BACKBONE_DAYS;

    // Preço: passeio geométrico com deriva suave.
    let price = anchorEthUsd;
    const prices: number[] = [];
    for (let i = 0; i < days; i++) {
      const cycle = Math.sin((i / 45) * 2 * Math.PI) * 0.0012;
      price = clamp(price * (1 + 0.0004 + cycle + gauss() * 0.022), 400, 20_000);
      prices.push(price);
    }
    // Reescala para terminar exatamente no preço do painel — sem degrau entre abas.
    const priceScale = anchorEthUsd / (prices.at(-1) ?? anchorEthUsd);

    // Gas: log-normal com regimes de congestionamento (Ordinals/Runes, mints…).
    let logGas = Math.log(18);
    let spikeLeft = 0;
    const gas: number[] = [];
    for (let i = 0; i < days; i++) {
      if (spikeLeft > 0) {
        spikeLeft--;
        logGas += 0.06 * gauss() + 0.02;
      } else {
        if (Math.random() < 0.006) spikeLeft = 5 + Math.floor(Math.random() * 25);
        // Reversão à média: gas caro não se sustenta (elasticidade de demanda).
        logGas += (Math.log(18) - logGas) * 0.03 + gauss() * 0.09;
      }
      gas.push(clamp(Math.exp(logGas), 0.4, 900));
    }
    const gasScale = anchorGasGwei / (gas.at(-1) ?? anchorGasGwei);

    for (let i = 0; i < days; i++) {
      const ms = today - (days - 1 - i) * DAY_MS;
      const gasPriceGwei = clamp((gas[i] ?? 18) * gasScale, 0.2, 1_200);
      // Atividade cai um pouco quando o gas dispara (elasticidade de demanda).
      const elasticity = clamp(1 - 0.16 * Math.log10(gasPriceGwei / 18), 0.5, 1.3);
      const txCount = Math.round(
        clamp(1_150_000 * elasticity * (1 + gauss() * 0.07), 120_000, 2_200_000),
      );
      const totalFeesEth = txCount * AVG_GAS_PER_TX * gasPriceGwei * 1e-9;
      this.daily.push({
        ms,
        ethUsd: clamp((prices[i] ?? anchorEthUsd) * priceScale, 3, 25_000),
        txCount,
        gasPriceGwei,
        totalFeesEth,
      });
    }
  }

  private valueOf(record: DailyRecord, metric: MetricId): number {
    switch (metric) {
      case 'total-fees-eth':
        return record.totalFeesEth;
      case 'total-fees-usd':
        return record.totalFeesEth * record.ethUsd;
      case 'mean-tx-fee-eth':
        return record.totalFeesEth / record.txCount;
      case 'mean-tx-fee-usd':
        return (record.totalFeesEth / record.txCount) * record.ethUsd;
      case 'mean-fee-per-gas':
        return record.gasPriceGwei;
    }
  }

  /** Início do período pedido, em ms. */
  private startMs(period: MetricPeriod): number {
    switch (period) {
      case '24h':
        return Date.now() - DAY_MS;
      case '7d':
        return Date.now() - 7 * DAY_MS;
      case '30d':
        return Date.now() - 30 * DAY_MS;
      case '90d':
        return Date.now() - 90 * DAY_MS;
      case 'ytd':
        return Date.UTC(new Date().getUTCFullYear(), 0, 1);
      case '1y':
        return Date.now() - 365 * DAY_MS;
      case 'all':
        return this.daily[0]?.ms ?? Date.now() - (BACKBONE_DAYS - 1) * DAY_MS;
    }
  }

  /**
   * Horária nas janelas curtas; diária em 30 dias — que é a granularidade do
   * agregado que a RN-15 guarda indefinidamente (`fee_stats_daily`), e o
   * "nível diário" que o parceiro pediu.
   */
  private resolutionFor(period: MetricPeriod): '1h' | '1d' {
    return period === '24h' || period === '7d' ? '1h' : '1d';
  }

  series(metric: MetricId, period: MetricPeriod, lookbackDays = 0): MetricSeries {
    const resolution = this.resolutionFor(period);
    const from = this.startMs(period);
    // Busca o lead-in ANTES do período pedido: é o que a suavização e a taxa
    // de variação consomem. `from` continua marcando o início do que se exibe.
    const fetchFrom = from - lookbackDays * DAY_MS;
    const window = this.daily.filter((d) => d.ms >= fetchFrom - DAY_MS);
    const fromIso = new Date(from).toISOString();

    if (resolution === '1d') {
      return {
        metric,
        resolution,
        from: fromIso,
        points: window
          .filter((d) => d.ms >= fetchFrom)
          .map((d) => ({
            t: new Date(d.ms).toISOString(),
            value: this.valueOf(d, metric),
            ethUsd: d.ethUsd,
          })),
      };
    }

    // Expande cada dia em 24 buckets com ciclo intradiário (pico ~14–18 UTC).
    const points: MetricPoint[] = [];
    for (const day of window) {
      for (let hour = 0; hour < 24; hour++) {
        const ms = day.ms + hour * 3_600_000;
        if (ms < fetchFrom || ms > Date.now()) continue;
        const cycle = 1 + 0.28 * Math.sin(((hour - 8) / 24) * 2 * Math.PI);
        // Contagem e preço do gas oscilam juntos; o total é DERIVADO dos dois,
        // como no dia — assim Mean Tx Fee continua sendo total ÷ txs também aqui.
        const txCount = Math.max(
          1,
          Math.round((day.txCount / 24) * cycle * (1 + gauss() * 0.07)),
        );
        const gasPriceGwei = day.gasPriceGwei * cycle * (1 + gauss() * 0.07);
        const hourly: DailyRecord = {
          ...day,
          txCount,
          gasPriceGwei,
          totalFeesEth: txCount * AVG_GAS_PER_TX * gasPriceGwei * 1e-9,
        };
        points.push({
          t: new Date(ms).toISOString(),
          value: this.valueOf(hourly, metric),
          ethUsd: day.ethUsd,
        });
      }
    }
    return { metric, resolution, from: fromIso, points };
  }
}
