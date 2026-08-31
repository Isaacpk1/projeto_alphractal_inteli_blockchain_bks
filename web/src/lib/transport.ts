import { endpoints, getJson, withQuery } from './api';
import type {
  ApiEthPriceTick, ApiFeesSnapshot, EthUsd24hResponse, FeeHistoryPointResponse, FeesInsights,
  FeesSnapshot, HistoryPoint, HistoryResponse, MetricId, MetricSeries,
  QueimaResponse, SpeedLabel, StreamStatus, TierId, TxTypeEstimate,
} from '../types/contract';

export type MetricPeriod = '24h' | '7d' | '30d' | '90d' | 'ytd' | '1y' | 'all';
export const PERIOD_LABEL: Record<MetricPeriod, string> = {
  '24h': '24H', '7d': '7D', '30d': '30D', '90d': '90D',
  ytd: 'YTD', '1y': '1Y', all: 'ALL',
};
export type ChartWindow = 'live' | '1h' | '4h' | '24h' | '7d' | '30d';
export const WINDOW_BLOCKS: Record<Exclude<ChartWindow, 'live'>, number> = {
  '1h': 300, '4h': 1_200, '24h': 7_200, '7d': 50_400, '30d': 216_000,
};

export interface StreamHandlers {
  onSnapshot: (snapshot: FeesSnapshot) => void;
  onPrice: (price: ApiEthPriceTick) => void;
  onStatus: (status: StreamStatus) => void;
}
export interface FeesTransport {
  connect(handlers: StreamHandlers): () => void;
  fetchSnapshot(): Promise<FeesSnapshot>;
  fetchHistory(window: Exclude<ChartWindow, 'live'>): Promise<HistoryPoint[]>;
  fetchInsights(): Promise<FeesInsights>;
  fetchMetricSeries(metric: MetricId, period: MetricPeriod, lookbackDays: number): Promise<MetricSeries>;
}

const TIER_BY_API: Record<SpeedLabel, TierId> = {
  lento: 'slow', padrao: 'standard', rapido: 'fast',
};
const ETA_SECONDS: Record<TierId, number> = { slow: 180, standard: 45, fast: 12 };
const OPERATION_LABEL: Record<string, string> = {
  transfer: 'ETH transfer', erc20_transfer: 'ERC-20 transfer',
  uniswap_v3_swap: 'DEX swap', approve: 'Token approval', nft_mint: 'NFT mint',
};
const OPERATION_ID: Record<string, string> = {
  transfer: 'eth-transfer', erc20_transfer: 'erc20-transfer',
  uniswap_v3_swap: 'dex-swap', approve: 'approval', nft_mint: 'nft-mint',
};
const operationId = (operation: string) => {
  const normalized = operation.trim().toLowerCase();
  return OPERATION_ID[normalized] ?? normalized.replaceAll('_', '-').replaceAll(' ', '-');
};

function adaptSnapshot(dto: ApiFeesSnapshot, change24hPct = 0): FeesSnapshot {
  const priority = (id: TierId) =>
    dto.speeds.find((item) => TIER_BY_API[item.speed] === id)?.priorityFeeGwei ?? 0;
  const ethTransfer = dto.estimates.filter((item) => operationId(item.operation) === 'eth-transfer');
  const makeTier = (id: TierId) => {
    const priorityFeeGwei = priority(id);
    const estimate = ethTransfer.find((item) => TIER_BY_API[item.speed] === id);
    return {
      maxFeeGwei: dto.baseFeeGwei + priorityFeeGwei,
      priorityFeeGwei,
      estEth: estimate?.totalFeeEth ?? 0,
      estUsd: estimate?.totalFeeUsd ?? 0,
      etaSeconds: ETA_SECONDS[id],
    };
  };

  const grouped = new Map<string, ApiFeesSnapshot['estimates']>();
  for (const estimate of dto.estimates) {
    const rows = grouped.get(estimate.operation) ?? [];
    rows.push(estimate);
    grouped.set(estimate.operation, rows);
  }
  const txEstimates: TxTypeEstimate[] = [...grouped].map(([operation, rows]) => {
    const value = (id: TierId) => {
      const row = rows.find((item) => TIER_BY_API[item.speed] === id);
      return { eth: row?.totalFeeEth ?? 0, usd: row?.totalFeeUsd ?? 0 };
    };
    return {
      id: operationId(operation),
      label: OPERATION_LABEL[operation] ?? operation.replaceAll('_', ' '),
      gasLimit: rows[0]?.gasUnits ?? 0,
      tiers: { slow: value('slow'), standard: value('standard'), fast: value('fast') },
    };
  });

  const trend = dto.nextBaseFeeGwei > dto.baseFeeGwei
    ? 'subindo'
    : dto.nextBaseFeeGwei < dto.baseFeeGwei ? 'caindo' : 'estavel';
  return {
    blockNumber: dto.blockNumber, blockHash: dto.blockHash,
    blockTimestampUtc: dto.blockTimestampUtc, baseFeeGwei: dto.baseFeeGwei,
    nextBaseFeeGwei: dto.nextBaseFeeGwei, gasUsed: dto.gasUsed,
    gasLimit: dto.gasLimit, gasUsedRatio: dto.gasUsedRatio, trend,
    congestion: { level: dto.congestion.level, ratio: dto.congestion.ratio },
    tiers: { slow: makeTier('slow'), standard: makeTier('standard'), fast: makeTier('fast') },
    ethUsd: { price: dto.ethUsd?.price ?? 0, change24hPct }, txEstimates,
    dataAgeSeconds: dto.dataAgeSeconds, isStale: dto.isStale,
    deliveryLatencySeconds: dto.deliveryLatencySeconds,
    windowSize: dto.windowSize, source: 'live',
  };
}

function historyRange(window: Exclude<ChartWindow, 'live'>) {
  const hours = { '1h': 1, '4h': 4, '24h': 24, '7d': 168, '30d': 720 }[window];
  const to = new Date();
  return {
    from: new Date(to.getTime() - hours * 3_600_000), to,
    granularity: hours <= 24 ? 'hour' : 'day',
  } as const;
}
function toHistoryPoint(row: FeeHistoryPointResponse, index: number): HistoryPoint {
  return {
    blockNumber: Date.parse(row.bucketUtc) + index,
    blockTimestampUtc: row.bucketUtc,
    baseFeeGwei: row.baseFeeGweiAvg,
    priorityFeeP50Gwei: row.priorityFeeGweiAvg,
    gasUsedRatio: row.gasUsedRatioAvg,
  };
}
function periodDays(period: MetricPeriod): number {
  if (period === '24h') return 1;
  if (period === '7d') return 7;
  if (period === '30d' || period === 'all') return 30;
  if (period === '90d') return 90;
  if (period === '1y') return 365;
  const now = new Date();
  return Math.max(1, Math.ceil((now.getTime() - Date.UTC(now.getUTCFullYear(), 0, 1)) / 86_400_000));
}
function metricValue(metric: MetricId, row: FeeHistoryPointResponse): number {
  // O rollup persiste a base fee queimada e a priority fee média. Recompomos
  // uma estimativa do total porque o agregado não guarda tips transação a transação.
  const feeRatio = row.baseFeeGweiAvg > 0
    ? (row.baseFeeGweiAvg + row.priorityFeeGweiAvg) / row.baseFeeGweiAvg
    : 1;
  const totalEth = row.burnedEth * feeRatio;
  if (metric === 'total-fees-eth') return totalEth;
  if (metric === 'total-fees-usd') return totalEth * row.ethUsdAvg;
  if (metric === 'mean-tx-fee-eth') return row.txCount > 0 ? totalEth / row.txCount : 0;
  if (metric === 'mean-tx-fee-usd') return row.txCount > 0 ? totalEth * row.ethUsdAvg / row.txCount : 0;
  return row.baseFeeGweiAvg + row.priorityFeeGweiAvg;
}

export class HttpFeesTransport implements FeesTransport {
  private change24hPct = 0;

  connect(handlers: StreamHandlers): () => void {
    const source = new EventSource(endpoints.stream);
    const priceSource = new EventSource(endpoints.priceStream);
    source.onopen = () => handlers.onStatus('ao-vivo');
    source.onmessage = (event) => {
      try {
        handlers.onSnapshot(adaptSnapshot(JSON.parse(event.data) as ApiFeesSnapshot, this.change24hPct));
      } catch { handlers.onStatus('erro') }
    };
    source.onerror = () => handlers.onStatus(
      source.readyState === EventSource.CLOSED ? 'erro' : 'reconectando',
    );
    priceSource.onmessage = (event) => {
      try { handlers.onPrice(JSON.parse(event.data) as ApiEthPriceTick) } catch { /* ignora tick invalido */ }
    };
    return () => {
      source.close();
      priceSource.close();
    };
  }

  async fetchSnapshot(): Promise<FeesSnapshot> {
    const [snapshot, price] = await Promise.all([
      getJson<ApiFeesSnapshot>(endpoints.snapshot),
      getJson<EthUsd24hResponse>(endpoints.ethUsd24h).catch(() => null),
    ]);
    this.change24hPct = price?.variacaoPercentual ?? 0;
    return adaptSnapshot(snapshot, this.change24hPct);
  }

  async fetchHistory(window: Exclude<ChartWindow, 'live'>): Promise<HistoryPoint[]> {
    const range = historyRange(window);
    const response = await getJson<HistoryResponse<FeeHistoryPointResponse>>(withQuery(endpoints.history, {
      granularity: range.granularity, from: range.from.toISOString(),
      to: range.to.toISOString(), limit: 10_000,
    }));
    return response.items.map(toHistoryPoint);
  }

  async fetchInsights(): Promise<FeesInsights> {
    const burn = await getJson<QueimaResponse>(endpoints.burn);
    return { burnRateEthPerMin: burn.ethPorMinuto, burned24hEth: burn.ethPorMinuto * 1_440 };
  }

  async fetchMetricSeries(metric: MetricId, period: MetricPeriod, lookbackDays: number): Promise<MetricSeries> {
    const days = periodDays(period);
    const resolution = days <= 7 ? '1h' : '1d';
    const to = new Date();
    const from = new Date(to.getTime() - days * 86_400_000);
    const queryFrom = new Date(from.getTime() - lookbackDays * 86_400_000);
    const response = await getJson<HistoryResponse<FeeHistoryPointResponse>>(withQuery(endpoints.history, {
      granularity: resolution === '1h' ? 'hour' : 'day',
      from: queryFrom.toISOString(), to: to.toISOString(), limit: 10_000,
    }));
    return {
      metric, resolution, from: from.toISOString(),
      points: response.items.map((row) => ({
        t: row.bucketUtc, value: metricValue(metric, row), ethUsd: row.ethUsdAvg,
      })),
    };
  }
}
