import { MockChain } from './generator';
import { MockMetrics } from './metricsGenerator';
import type {
  ChartWindow,
  FeesTransport,
  MetricPeriod,
  StreamHandlers,
} from '../transport';
import { WINDOW_BLOCKS } from '../transport';
import type {
  FeesInsights,
  FeesSnapshot,
  HistoryPoint,
  MetricId,
  MetricSeries,
} from '../../types/contract';

/**
 * Implementação mockada do FeesTransport — o "servidor" enquanto a API .NET
 * não existe. Emite um bloco a cada 12 s, igual à Mainnet, e responde os
 * mesmos endpoints com latência simulada. O store não sabe que isto é mock.
 *
 * Para testar os estados do RF-26/RF-32 sem esperar a rede cair, o console
 * do navegador (só em dev) ganha:
 *   __afMock.outage(15)  → simula queda de conexão por 15 s (Reconnecting)
 *   __afMock.stale(60)   → conexão ok, mas sem bloco novo por 60 s (Stale data)
 */

const BLOCK_MS = 12_000;

// Uma única cadeia para stream, snapshot e histórico concordarem entre si.
const chain = new MockChain();

// As séries históricas terminam ancoradas no estado atual da cadeia, para não
// haver degrau de valor entre a aba ao vivo e a aba histórica.
let metrics: MockMetrics | null = null;
function metricsStore(): MockMetrics {
  if (!metrics) {
    const now = chain.snapshot();
    metrics = new MockMetrics(now.ethUsd.price, now.baseFeeGwei);
  }
  return metrics;
}

const delay = (ms: number) => new Promise((r) => setTimeout(r, ms));

export class MockFeesTransport implements FeesTransport {
  private outageUntil = 0;
  private staleUntil = 0;

  connect(handlers: StreamHandlers): () => void {
    let timer: ReturnType<typeof setInterval> | null = null;
    let disposed = false;

    // Handshake: 'conectando' é o estado inicial do store; abrir leva ~0,5 s.
    const boot = setTimeout(() => {
      if (disposed) return;
      handlers.onStatus('ao-vivo');
      handlers.onSnapshot(chain.snapshot());
      timer = setInterval(() => {
        const now = Date.now();
        if (now < this.outageUntil) {
          handlers.onStatus('reconectando');
          return;
        }
        if (now < this.staleUntil) return; // conectado, porém mudo
        chain.tick();
        handlers.onStatus('ao-vivo');
        handlers.onSnapshot(chain.snapshot());
      }, BLOCK_MS);
    }, 500);

    if (import.meta.env.DEV) {
      (window as unknown as Record<string, unknown>)['__afMock'] = {
        outage: (seconds = 15) => {
          this.outageUntil = Date.now() + seconds * 1000;
        },
        stale: (seconds = 60) => {
          this.staleUntil = Date.now() + seconds * 1000;
        },
      };
    }

    return () => {
      disposed = true;
      clearTimeout(boot);
      if (timer) clearInterval(timer);
    };
  }

  async fetchSnapshot(): Promise<FeesSnapshot> {
    await delay(120);
    return chain.snapshot();
  }

  async fetchHistory(window: Exclude<ChartWindow, 'live'>): Promise<HistoryPoint[]> {
    await delay(300);
    // 1h ≈ a janela quente inteira; acima disso é agregado (caminho frio).
    if (window === '1h') return chain.liveHistory();
    return chain.aggregatedHistory(WINDOW_BLOCKS[window] * 12);
  }

  async fetchInsights(): Promise<FeesInsights> {
    await delay(400);
    return chain.insights();
  }

  async fetchMetricSeries(
    metric: MetricId,
    period: MetricPeriod,
    lookbackDays: number,
  ): Promise<MetricSeries> {
    // Latência maior de propósito: é consulta de agregado no ClickHouse, não
    // leitura de memória. O skeleton de carregamento precisa ser visível.
    await delay(450);
    return metricsStore().series(metric, period, lookbackDays);
  }
}
