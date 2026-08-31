/** Tipos usados pela interface. O transporte converte os DTOs reais da API. */

export type CongestionLevel = 'baixo' | 'normal' | 'alto' | 'extremo';
export type TrendDirection = 'subindo' | 'caindo' | 'estavel';
export type TierId = 'slow' | 'standard' | 'fast';
export type SpeedLabel = 'lento' | 'padrao' | 'rapido';

export interface FeeTier {
  maxFeeGwei: number;
  priorityFeeGwei: number;
  estEth: number;
  estUsd: number;
  etaSeconds: number;
}

export interface TxTypeEstimate {
  id: string;
  label: string;
  gasLimit: number;
  tiers: Record<TierId, { eth: number; usd: number }>;
}

export interface FeesSnapshot {
  blockNumber: number;
  blockHash: string;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  nextBaseFeeGwei: number;
  gasUsed: number;
  gasLimit: number;
  gasUsedRatio: number;
  trend: TrendDirection;
  congestion: { level: CongestionLevel; ratio: number };
  tiers: Record<TierId, FeeTier>;
  ethUsd: { price: number; change24hPct: number };
  txEstimates: TxTypeEstimate[];
  dataAgeSeconds: number;
  isStale: boolean;
  deliveryLatencySeconds: number;
  windowSize: number;
  source: 'live';
}

/** DTO exato de GET /snapshot e de cada mensagem SSE. */
export interface ApiFeesSnapshot {
  blockNumber: number;
  blockHash: string;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  nextBaseFeeGwei: number;
  gasUsed: number;
  gasLimit: number;
  gasUsedRatio: number;
  congestion: {
    level: CongestionLevel;
    ratio: number;
    movingAverageGwei: number;
    sampleSize: number;
  };
  speeds: Array<{ speed: SpeedLabel; priorityFeeGwei: number }>;
  estimates: Array<{
    operation: string;
    speed: SpeedLabel;
    gasUnits: number;
    totalFeeGwei: number;
    totalFeeEth: number;
    totalFeeUsd?: number | null;
  }>;
  ethUsd?: {
    price: number;
    observedAtUtc: string;
    isStale: boolean;
    source: string;
  } | null;
  dataAgeSeconds: number;
  isStale: boolean;
  deliveryLatencySeconds: number;
  windowSize: number;
  source: 'live';
}

export interface ApiEthPriceTick {
  price: number;
  observedAtUtc: string;
  source: string;
}

export interface HistoryPoint {
  blockNumber: number;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  priorityFeeP50Gwei: number;
  gasUsedRatio: number;
}

export interface FeesInsights {
  burnRateEthPerMin: number;
  burned24hEth: number;
}

export type MetricId =
  | 'total-fees-eth'
  | 'total-fees-usd'
  | 'mean-tx-fee-eth'
  | 'mean-tx-fee-usd'
  | 'mean-fee-per-gas';

export interface MetricPoint { t: string; value: number; ethUsd: number }
export interface MetricSeries {
  metric: MetricId;
  resolution: '1h' | '1d';
  from: string;
  points: MetricPoint[];
}

export interface FeeHistoryPointResponse {
  bucketUtc: string;
  blocks: number;
  baseFeeGweiAvg: number;
  baseFeeGweiMin: number;
  baseFeeGweiMax: number;
  baseFeeGweiP50: number;
  baseFeeGweiP90: number;
  baseFeeGweiP95: number;
  priorityFeeGweiAvg: number;
  gasUsedRatioAvg: number;
  txCount: number;
  burnedEth: number;
  ethUsdAvg: number;
}

export interface HistoryResponse<T> {
  granularity: 'hour' | 'day';
  fromUtc: string;
  toUtc: string;
  count: number;
  items: T[];
}

export interface EthUsd24hResponse {
  precoAtual: number;
  observadoEmUtc: string;
  preco24h?: number | null;
  variacaoPercentual?: number | null;
}

export interface QueimaResponse {
  ethPorMinuto: number;
  ethNoUltimoBloco: number;
  ethNaJanela: number;
  blocosNaJanela: number;
  minutosDaJanela: number;
  usdPorMinuto?: number | null;
}

export type StreamStatus = 'conectando' | 'ao-vivo' | 'reconectando' | 'atrasado' | 'erro';
export interface HealthResponse {
  status: 'ok' | 'degraded'; rpcConnected: boolean; lastBlockNumber: number | null; uptimeSeconds: number;
}

export interface LatestBlockResponse {
  blockNumber: number; blockTimestampUtc: string; baseFeeGwei: number;
  nextBaseFeeGwei: number; priorityFeeGwei: number; gasUsed: number;
  gasLimit: number; gasUsedRatio: number; txCount: number; burnedEth: number;
  ethUsd: number; dataAgeSeconds: number; source: 'cold';
}
export interface MempoolNowResponse {
  sampledAtUtc: string; blockNumber: number; pendingBlockTxCount: number;
  baseFeeGwei: number; prioritySlowGwei: number; priorityStandardGwei: number;
  priorityFastGwei: number; ethUsd: number;
}
export interface FeeEstimateResponse {
  operation: string; speed: string; gasUnits: number; totalFeeGwei: number;
  totalFeeUsd: number; lastSampledAtUtc: string;
}
export interface FeeEstimateDailyResponse {
  bucket: string; operation: string; speed: string; samples: number;
  usdAvg: number; usdMin: number; usdMax: number; usdP50: number; usdP90: number;
}
export interface ComponentStatusResponse {
  component: string; status: string; lagMs: number; lastBlock: number;
  detail: string; lastSeenAtUtc: string; secondsSinceLastSeen: number;
}
export interface StatusResponse { coldPath: 'up' | 'down'; components: ComponentStatusResponse[] }
