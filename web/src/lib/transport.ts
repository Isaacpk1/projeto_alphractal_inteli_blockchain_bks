import { endpoints } from './api';
import type {
  FeesInsights,
  FeesSnapshot,
  HistoryPoint,
  MetricId,
  MetricSeries,
  StreamStatus,
} from '../types/contract';

/**
 * Períodos do explorador de métricas.
 *
 * As três primeiras cobrem a RF-38. As janelas longas reproduzem a experiência
 * da plataforma de referência e usam os agregados diários, que podem ser
 * mantidos além da retenção de 30 dias dos blocos brutos.
 */
export type MetricPeriod = '24h' | '7d' | '30d' | '90d' | 'ytd' | '1y' | 'all';

export const PERIOD_LABEL: Record<MetricPeriod, string> = {
  '24h': '24H',
  '7d': '7D',
  '30d': '30D',
  '90d': '90D',
  ytd: 'YTD',
  '1y': '1Y',
  all: 'ALL',
};

/**
 * Janelas do gráfico. 'live' vem da memória do .NET (300 blocos, RF-24);
 * as demais vêm do endpoint de histórico (RF-18 / RF-38 — caminho frio).
 */
export type ChartWindow = 'live' | '1h' | '4h' | '24h' | '7d' | '30d';

/** ~1 bloco a cada 12 s → blocos por janela, o parâmetro do RF-18. */
export const WINDOW_BLOCKS: Record<Exclude<ChartWindow, 'live'>, number> = {
  '1h': 300,
  '4h': 1_200,
  '24h': 7_200,
  '7d': 50_400,
  '30d': 216_000,
};

export interface StreamHandlers {
  onSnapshot: (snapshot: FeesSnapshot) => void;
  onStatus: (status: StreamStatus) => void;
}

/**
 * O que o feesStore precisa do mundo externo. Duas implementações:
 * HttpFeesTransport (a API .NET de verdade) e MockFeesTransport (lib/mock/,
 * enquanto a API não existe). O store não sabe qual das duas está usando —
 * é o que permite trocar mock por API real mudando uma variável de ambiente.
 */
export interface FeesTransport {
  /** Abre o stream (RF-16). Devolve a função que o encerra. */
  connect(handlers: StreamHandlers): () => void;
  /** Hidratação do load (RF-17). */
  fetchSnapshot(): Promise<FeesSnapshot>;
  /** Histórico para o gráfico (RF-18). */
  fetchHistory(window: Exclude<ChartWindow, 'live'>): Promise<HistoryPoint[]>;
  /** D-06 (backlog) — pode rejeitar; os painéis que dependem disso degradam sós. */
  fetchInsights(): Promise<FeesInsights>;
  /**
   * Métricas agregadas da aba Historical Fees (caminho frio, RF-37/RF-38).
   * @param lookbackDays dias ANTES do período, para alimentar suavização e
   *   taxa de variação. Sem isto, "YoY sobre 1 ano" não teria com o que
   *   comparar e o gráfico sairia vazio.
   */
  fetchMetricSeries(
    metric: MetricId,
    period: MetricPeriod,
    lookbackDays: number,
  ): Promise<MetricSeries>;
}

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { headers: { Accept: 'application/json' } });
  if (!res.ok) throw new Error(`${url} → HTTP ${res.status}`);
  return (await res.json()) as T;
}

/** Fala com a API .NET. EventSource já reconecta sozinho (RF-33). */
export class HttpFeesTransport implements FeesTransport {
  connect(handlers: StreamHandlers): () => void {
    const source = new EventSource(endpoints.stream);

    source.onopen = () => handlers.onStatus('ao-vivo');
    source.onmessage = (event) => {
      handlers.onSnapshot(JSON.parse(event.data) as FeesSnapshot);
    };
    source.onerror = () => {
      handlers.onStatus(
        source.readyState === EventSource.CLOSED ? 'erro' : 'reconectando',
      );
    };

    return () => source.close();
  }

  fetchSnapshot(): Promise<FeesSnapshot> {
    return getJson<FeesSnapshot>(endpoints.snapshot);
  }

  fetchHistory(window: Exclude<ChartWindow, 'live'>): Promise<HistoryPoint[]> {
    return getJson<HistoryPoint[]>(
      `${endpoints.history}?blocks=${WINDOW_BLOCKS[window]}`,
    );
  }

  fetchInsights(): Promise<FeesInsights> {
    return getJson<FeesInsights>(endpoints.insights);
  }

  fetchMetricSeries(
    metric: MetricId,
    period: MetricPeriod,
    lookbackDays: number,
  ): Promise<MetricSeries> {
    return getJson<MetricSeries>(
      `${endpoints.metrics}?metric=${metric}&period=${period}&lookbackDays=${lookbackDays}`,
    );
  }
}
