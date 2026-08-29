/**
 * Espelho em TypeScript de api/src/Alphractal.Fees.Api/Models/Responses/.
 *
 * Este arquivo é a metade do contrato que vive no front. Não existe compilador
 * que verifique se ele bate com o C# — se um campo mudar lá e não mudar aqui,
 * a tela quebra em runtime, sem aviso. Mudou o DTO na API? Mude aqui no MESMO PR.
 *
 * Unidades: a API já entrega convertido. Nunca chega wei aqui.
 */

// ─── Caminho quente (memória da API, SSE) ────────────────────────────────────

export type SpeedLabel = 'lento' | 'padrao' | 'rapido';
export type CongestionLabel = 'baixo' | 'normal' | 'alto' | 'extremo';

export interface CongestionResponse {
  level: CongestionLabel;
  /** Base fee atual ÷ média móvel de N_cong blocos. */
  ratio: number;
  movingAverageGwei: number;
  /** Menor que N_cong enquanto a janela enche — o painel pode indicar "aquecendo". */
  sampleSize: number;
}

export interface SpeedTierResponse {
  speed: SpeedLabel;
  priorityFeeGwei: number;
}

export interface OperationCostResponse {
  operation: string;
  speed: SpeedLabel;
  gasUnits: number;
  totalFeeGwei: number;
  totalFeeEth: number;
  /** Ausente quando não há cotação. NUNCA exiba 0 — exiba "—". */
  totalFeeUsd?: number;
}

export interface EthPriceResponse {
  price: number;
  observedAtUtc: string;
  /** Defasada há mais de 5 min: exibir como desatualizada (RN-03). */
  isStale: boolean;
  source: string;
}

/** Payload de cada evento SSE e de /api/v1/fees/snapshot. */
export interface FeesSnapshot {
  blockNumber: number;
  blockHash: string;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  nextBaseFeeGwei: number;
  gasUsed: number;
  gasLimit: number;
  gasUsedRatio: number;
  congestion: CongestionResponse;
  speeds: SpeedTierResponse[];
  estimates: OperationCostResponse[];
  /** Ausente quando não há cotação disponível. */
  ethUsd?: EthPriceResponse;
  dataAgeSeconds: number;
  /** Sem bloco novo há mais de 60 s — sair de "Ao vivo" (RN-07). */
  isStale: boolean;
  /** Segundos entre o bloco e a chegada na API. É o RNF-01 medido. */
  deliveryLatencySeconds: number;
  windowSize: number;
  source: 'live';
}

export type StreamStatus = 'conectando' | 'ao-vivo' | 'reconectando' | 'erro';

// ─── Caminho frio (ClickHouse, views v_*) ────────────────────────────────────

/** Resposta de /api/v1/fees/latest. `source` é sempre "cold" — não é o dado ao vivo. */
export interface LatestBlockResponse {
  blockNumber: number;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  nextBaseFeeGwei: number;
  priorityFeeGwei: number;
  gasUsed: number;
  gasLimit: number;
  gasUsedRatio: number;
  txCount: number;
  burnedEth: number;
  ethUsd: number;
  dataAgeSeconds: number;
  source: 'cold';
}

export interface MempoolNowResponse {
  sampledAtUtc: string;
  blockNumber: number;
  pendingTxCount: number;
  baseFeeGwei: number;
  prioritySlowGwei: number;
  priorityStandardGwei: number;
  priorityFastGwei: number;
  ethUsd: number;
}

export interface FeeEstimateResponse {
  operation: string;
  speed: string;
  gasUnits: number;
  totalFeeGwei: number;
  totalFeeUsd: number;
  lastSampledAtUtc: string;
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

export interface FeeEstimateDailyResponse {
  /** Data ISO (YYYY-MM-DD) — serializado de DateOnly. */
  bucket: string;
  operation: string;
  speed: string;
  samples: number;
  usdAvg: number;
  usdMin: number;
  usdMax: number;
  usdP50: number;
  usdP90: number;
}

export type HistoryGranularity = 'hour' | 'day';

export interface HistoryResponse<T> {
  granularity: HistoryGranularity;
  fromUtc: string;
  toUtc: string;
  count: number;
  items: T[];
}

// ─── Status da ingestão ──────────────────────────────────────────────────────

export interface ComponentStatusResponse {
  component: string;
  status: string;
  lagMs: number;
  lastBlock: number;
  detail: string;
  lastSeenAtUtc: string;
  secondsSinceLastSeen: number;
}

export interface StatusResponse {
  coldPath: 'up' | 'down';
  components: ComponentStatusResponse[];
}
