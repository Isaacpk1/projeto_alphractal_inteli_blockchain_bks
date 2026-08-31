import { useMock } from './api';
import { HttpFeesTransport } from './transport';
import { MockFeesTransport } from './mock/mockTransport';
import type { ChartWindow, FeesTransport, MetricPeriod } from './transport';
import type {
  ApiEthPriceTick,
  FeesInsights,
  FeesSnapshot,
  HistoryPoint,
  MetricId,
  MetricSeries,
  StreamStatus,
} from '../types/contract';

/**
 * Dono único do stream e da janela de 300 blocos — vive FORA da árvore React.
 *
 * Por quê: um bloco chega a cada ~12 s. Se esse estado morasse num useState do
 * App, a árvore inteira reconciliaria a cada bloco — exatamente o que o RNF-03
 * proíbe ("nenhum render desnecessário acima do card"). Aqui, componentes
 * assinam FATIAS via useFeesSlice (useSyncExternalStore): quem lê a base fee
 * re-renderiza a cada bloco; quem lê só o status fica parado durante horas.
 *
 * O critério de verificação é o React DevTools Profiler, como manda o RNF-03.
 */

const HOT_WINDOW = 300; // RN-10
/** Sem bloco novo por este tempo com a conexão aberta → "Stale data" (RF-26). */
const STALE_AFTER_SECONDS = 45;

export interface FeesState {
  status: StreamStatus;
  snapshot: FeesSnapshot | null;
  /** Cotacao independente do bloco, atualizada pelo ticker da Coinbase. */
  ethPrice: FeesSnapshot['ethUsd'] | null;
  /** A janela quente espelhada no front — alimenta o gráfico LIVE (RF-24). */
  liveHistory: HistoryPoint[];
  /** D-06 (backlog) — null se o endpoint não existir; os painéis degradam sós. */
  insights: FeesInsights | null;
  /** Quando o último bloco chegou (ms) — o "há 4s" do RF-25 tica sobre isto. */
  lastBlockAtMs: number | null;
}

let state: FeesState = {
  status: 'conectando',
  snapshot: null,
  ethPrice: null,
  liveHistory: [],
  insights: null,
  lastBlockAtMs: null,
};

const listeners = new Set<() => void>();

function setState(patch: Partial<FeesState>): void {
  const next = { ...state, ...patch };
  // Guarda contra notificação redundante — o watchdog roda a cada 5 s e quase
  // sempre não muda nada; notificar mesmo assim acordaria todos os assinantes.
  if (
    next.status === state.status &&
    next.snapshot === state.snapshot &&
    next.ethPrice === state.ethPrice &&
    next.liveHistory === state.liveHistory &&
    next.insights === state.insights &&
    next.lastBlockAtMs === state.lastBlockAtMs
  ) {
    return;
  }
  state = next;
  listeners.forEach((fn) => fn());
}

export const feesStore = {
  getState: (): FeesState => state,
  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};

function toHistoryPoint(s: FeesSnapshot): HistoryPoint {
  return {
    blockNumber: s.blockNumber,
    blockTimestampUtc: s.blockTimestampUtc,
    baseFeeGwei: s.baseFeeGwei,
    priorityFeeP50Gwei: s.tiers.standard.priorityFeeGwei,
    gasUsedRatio: s.gasUsedRatio,
  };
}

function acceptSnapshot(s: FeesSnapshot): void {
  const point = toHistoryPoint(s);
  const last = state.liveHistory.at(-1);
  // Mesmo número de bloco = reemissão (reorg/replay) → substitui, não duplica.
  const history =
    last && last.blockNumber >= point.blockNumber
      ? [...state.liveHistory.slice(0, -1), point]
      : [...state.liveHistory, point].slice(-HOT_WINDOW);

  setState({
    snapshot: s,
    ethPrice: s.ethUsd,
    liveHistory: history,
    status: 'ao-vivo',
    lastBlockAtMs: Date.now() - s.dataAgeSeconds * 1000,
  });
}

function acceptPrice(tick: ApiEthPriceTick): void {
  setState({
    ethPrice: {
      price: tick.price,
      change24hPct: state.ethPrice?.change24hPct ?? 0,
    },
  });
}

/** RF-17/RF-18 — abre a tela já populada, sem esperar o primeiro bloco do SSE. */
async function hydrate(transport: FeesTransport): Promise<void> {
  try {
    const [snapshot, history] = await Promise.all([
      transport.fetchSnapshot(),
      transport.fetchHistory('1h'),
    ]);
    setState({
      snapshot,
      ethPrice: snapshot.ethUsd,
      liveHistory: history.slice(-HOT_WINDOW),
      lastBlockAtMs: Date.now() - snapshot.dataAgeSeconds * 1000,
    });
  } catch {
    // Sem snapshot não é fatal: o SSE popula quando o primeiro bloco chegar.
  }
}

function loadInsights(transport: FeesTransport): void {
  transport.fetchInsights().then(
    (insights) => setState({ insights }),
    () => setState({ insights: null }), // D-06 fora do ar → painéis somem sós
  );
}

// Uma instância só, compartilhada entre o stream e as consultas do gráfico.
const transport: FeesTransport = useMock
  ? new MockFeesTransport()
  : new HttpFeesTransport();

/** Janelas frias do gráfico (RF-18/RF-38) — usado pelas views, fora do stream. */
export function fetchHistoryWindow(
  window: Exclude<ChartWindow, 'live'>,
): Promise<HistoryPoint[]> {
  return transport.fetchHistory(window);
}

/** Séries do explorador de métricas (aba Historical Fees) — idem, caminho frio. */
export function fetchMetricSeries(
  metric: MetricId,
  period: MetricPeriod,
  lookbackDays: number,
): Promise<MetricSeries> {
  return transport.fetchMetricSeries(metric, period, lookbackDays);
}

let started = false;

/**
 * Liga o store. Idempotente — App chama via useFeesStream e o StrictMode pode
 * chamar duas vezes; a segunda é no-op. O stream vive até a aba fechar.
 */
export function startFeesStore(): void {
  if (started) return;
  started = true;

  void hydrate(transport);
  loadInsights(transport);

  let wasReconnecting = false;
  transport.connect({
    onSnapshot: acceptSnapshot,
    onPrice: acceptPrice,
    onStatus: (status) => {
      if (status === 'ao-vivo' && wasReconnecting) {
        // Voltou de uma queda: os blocos do intervalo se perderam. Re-hidratar
        // tapa o degrau do gráfico (RF-33 + consistência do RF-24).
        wasReconnecting = false;
        void hydrate(transport);
        loadInsights(transport);
      }
      if (status === 'reconectando' || status === 'erro') wasReconnecting = true;
      // 'atrasado' é decisão do watchdog local, não do transporte.
      if (state.status === 'atrasado' && status === 'ao-vivo' && !blockIsFresh()) {
        return; // continua atrasado até chegar bloco novo de fato
      }
      setState({ status });
    },
  });

  // Watchdog do RF-26: conexão aberta ≠ dado fresco. EventSource não avisa
  // quando o servidor fica mudo — quem avisa é a idade do último bloco.
  setInterval(() => {
    if (state.status === 'ao-vivo' && !blockIsFresh()) {
      setState({ status: 'atrasado' });
    } else if (state.status === 'atrasado' && blockIsFresh()) {
      setState({ status: 'ao-vivo' });
    }
  }, 5_000);
}

function blockIsFresh(): boolean {
  return (
    state.lastBlockAtMs !== null &&
    (Date.now() - state.lastBlockAtMs) / 1000 < STALE_AFTER_SECONDS
  );
}
