/**
 * Espelho em TypeScript de api/src/Alphractal.Fees.Api/Models/Responses/.
 *
 * Este arquivo é a metade do contrato que vive no front. Não existe compilador
 * que verifique se ele bate com o C# — se um campo mudar lá e não mudar aqui,
 * a tela quebra em runtime, sem aviso. Mudou o DTO na API? Mude aqui no MESMO PR.
 *
 * Unidades: a API já entrega convertido. Nunca chega wei aqui.
 * Todo cálculo (RN-01 a RN-05) vive na API — o front só exibe (RN-09).
 */

/** RF-12 / RF-23 — nível de congestionamento derivado da média móvel de 100 blocos. */
export type CongestionLevel = 'baixo' | 'normal' | 'alto' | 'extremo';

/** RF-29 — direção da base fee em relação ao bloco anterior. Calculada na API. */
export type TrendDirection = 'subindo' | 'caindo' | 'estavel';

/** RF-08 — as três faixas. As chaves são estáveis; o rótulo de exibição é do front. */
export type TierId = 'slow' | 'standard' | 'fast';

/** Uma faixa de taxa (RF-08, RF-09). `est*` é o custo de uma transferência simples. */
export interface FeeTier {
  /** baseFee + percentil de priority fee, pronto para exibir. */
  maxFeeGwei: number;
  priorityFeeGwei: number;
  estEth: number;
  estUsd: number;
  /** Tempo esperado de inclusão, para o selo "~12s" do card. */
  etaSeconds: number;
}

/** RF-11 / RF-27 — custo estimado por tipo de transação, já calculado na API. */
export interface TxTypeEstimate {
  /** Ex.: 'eth-transfer', 'erc20-transfer', 'dex-swap', 'approval', 'nft-mint'. */
  id: string;
  label: string;
  gasLimit: number;
  tiers: Record<TierId, { eth: number; usd: number }>;
}

/** Evento do SSE (RF-16) e resposta do snapshot (RF-17). Um por bloco. */
export interface FeesSnapshot {
  blockNumber: number;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  /** RF-13 — projeção EIP-1559 do próximo bloco. */
  nextBaseFeeGwei: number;
  gasUsedRatio: number;
  trend: TrendDirection;
  congestion: {
    level: CongestionLevel;
    /** baseFee atual ÷ média móvel dos últimos 100 blocos (RN-04). */
    ratio: number;
  };
  tiers: Record<TierId, FeeTier>;
  /** RF-09 / RF-10 — cotação com cache na API. */
  ethUsd: { price: number; change24hPct: number };
  txEstimates: TxTypeEstimate[];
  /*
   * Sem métricas de mempool. O BRD as exclui duas vezes — nos não-objetivos
   * ("foi exatamente o custo que inviabilizou a Blocknative") e na tabela de
   * fora de escopo — e o orçamento de RPC mostra que a subscription queima a
   * cota mensal em ~7 dias. RF-07 permanece [C], a decidir no kick-off.
   */
  /** Idade do dado em segundos — alimenta o aviso de "dado atrasado". */
  dataAgeSeconds: number;
}

/** Um ponto do histórico (RF-18) — alimenta o gráfico (RF-24). */
export interface HistoryPoint {
  blockNumber: number;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  /** Percentil 50 da priority fee — as barras laranjas do gráfico. */
  priorityFeeP50Gwei: number;
  gasUsedRatio: number;
}

/**
 * Queima de ETH pela base fee (EIP-1559) — BRD §4.1 e RF-06 (Must) do BRD:
 * "Calcular fee burn acumulado e emissão líquida de ETH".
 *
 * ⚠️ Ausente da lista RF-01..RF-40 dos requisitos técnicos: os dois documentos
 * de escopo divergem. Mantido porque o BRD o classifica como Must e nenhum
 * documento o exclui — a divergência é pergunta de kick-off.
 *
 * ► Só existe MOCKADO. Nenhum endpoint real serve isto ainda.
 */
export interface FeesInsights {
  burnRateEthPerMin: number;
  burned24hEth: number;
}

/**
 * Métricas históricas agregadas — a aba Historical Fees. CAMINHO FRIO:
 * agregados servidos pelo ClickHouse (RF-37/RF-38), nunca pelo stream.
 *
 * Espelham as métricas de taxa da plataforma do parceiro, traduzidas para
 * Ethereum. A tradução de "Mean Tx Fee per Byte" (que é conceito de Bitcoin)
 * está justificada na ADR-003.
 */
export type MetricId =
  | 'total-fees-eth'
  | 'total-fees-usd'
  | 'mean-tx-fee-eth'
  | 'mean-tx-fee-usd'
  | 'mean-fee-per-gas';

export interface MetricPoint {
  /** Início do bucket, ISO-8601 UTC. */
  t: string;
  value: number;
  /** Preço ETH/USD no mesmo bucket — alimenta o overlay de preço. */
  ethUsd: number;
}

export interface MetricSeries {
  metric: MetricId;
  /** Granularidade escolhida pela API conforme o período pedido. */
  resolution: '1h' | '1d';
  /**
   * Início do período PEDIDO, ISO-8601. Os pontos podem começar antes disto:
   * é o lead-in que a suavização e a taxa de variação consomem. O cliente
   * recorta a exibição a partir daqui.
   */
  from: string;
  points: MetricPoint[];
}

/** RF-20 — GET /health. */
export interface HealthResponse {
  status: 'ok' | 'degraded';
  rpcConnected: boolean;
  lastBlockNumber: number | null;
  uptimeSeconds: number;
}

/**
 * RF-26 — estados exibidos: Ao vivo / Reconectando / Offline / Dados desatualizados.
 * 'atrasado' = conectado, mas sem bloco novo há tempo demais (watchdog do store).
 */
export type StreamStatus =
  | 'conectando'
  | 'ao-vivo'
  | 'reconectando'
  | 'atrasado'
  | 'erro';
