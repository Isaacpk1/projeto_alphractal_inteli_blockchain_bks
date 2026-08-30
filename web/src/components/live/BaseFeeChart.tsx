import { useCallback, useEffect, useState } from 'react';
import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { useFeesSlice } from '../../hooks/useFeesSlice';
import { fetchHistoryWindow } from '../../lib/feesStore';
import type { FeesState } from '../../lib/feesStore';
import type { ChartWindow } from '../../lib/transport';
import { fmtAxisTime, fmtGwei, fmtTooltipTime } from '../../lib/format';
import type { HistoryPoint } from '../../types/contract';
import { ErrorState } from '../ui/ErrorState';
import { SegmentedControl } from '../ui/SegmentedControl';
import { Skeleton } from '../ui/Skeleton';

/**
 * O gráfico "Base Fee & Priority Fee" do design (RF-24): linha/área azul para a
 * base fee, barras laranjas para a priority p50, tooltip com as duas.
 */

interface TooltipShape {
  active?: boolean;
  payload?: ReadonlyArray<{ payload?: unknown }>;
}

function ChartTooltip({ active, payload }: TooltipShape) {
  const point = payload?.[0]?.payload as HistoryPoint | undefined;
  if (!active || !point) return null;
  return (
    <div className="chart-tooltip">
      <div className="chart-tooltip__date">{fmtTooltipTime(point.blockTimestampUtc)}</div>
      <div className="chart-tooltip__row">
        <span className="dot dot--base" /> Base: {fmtGwei(point.baseFeeGwei)} gwei
      </div>
      <div className="chart-tooltip__row">
        <span className="dot dot--priority" /> Priority: {fmtGwei(point.priorityFeeP50Gwei)}{' '}
        gwei
      </div>
    </div>
  );
}

/** Núcleo apresentacional — recebe os pontos prontos, não busca nada. */
export function FeeChartCore({ data }: { data: HistoryPoint[] }) {
  return (
    <div className="chart-area">
      <ResponsiveContainer width="100%" height={320}>
        <ComposedChart data={data} margin={{ top: 12, right: 8, bottom: 0, left: 0 }}>
          <defs>
            <linearGradient id="baseFeeFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="var(--accent)" stopOpacity={0.22} />
              <stop offset="100%" stopColor="var(--accent)" stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke="var(--chart-grid)" strokeDasharray="4 6" vertical={false} />
          <XAxis
            dataKey="blockTimestampUtc"
            tickFormatter={fmtAxisTime}
            tick={{ fill: 'var(--text-faint)', fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            minTickGap={48}
          />
          <YAxis
            tickFormatter={(v: number) => `${Math.round(v)} gwei`}
            tick={{ fill: 'var(--text-faint)', fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={64}
            domain={[0, 'auto']}
          />
          <Tooltip
            content={<ChartTooltip />}
            cursor={{ stroke: 'var(--chart-cursor)', strokeWidth: 1 }}
          />
          {/* Animação desligada: com um ponto novo a cada 12 s, animar é jank. */}
          <Bar
            dataKey="priorityFeeP50Gwei"
            fill="var(--amber)"
            barSize={2}
            isAnimationActive={false}
          />
          <Area
            type="monotone"
            dataKey="baseFeeGwei"
            stroke="var(--accent)"
            strokeWidth={2}
            fill="url(#baseFeeFill)"
            isAnimationActive={false}
            dot={false}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}

const WINDOW_OPTIONS: ReadonlyArray<{ value: ChartWindow; label: string }> = [
  { value: 'live', label: 'LIVE' },
  { value: '1h', label: '1H' },
  { value: '4h', label: '4H' },
  { value: '24h', label: '24H' },
  { value: '7d', label: '7D' },
  { value: '30d', label: '30D' },
];

/** Resultado carimbado com a requisição que o gerou — "carregando" é derivado
 *  (carimbo ≠ pedido atual), sem setState síncrono dentro do effect. */
type ColdFetch =
  | { window: ChartWindow; tick: number; state: 'error' }
  | { window: ChartWindow; tick: number; state: 'ready'; data: HistoryPoint[] };

/**
 * O card completo da visão Real-Time. Janela LIVE lê a memória quente do store
 * (re-renderiza a cada bloco — este card, e só ele); janelas maiores buscam o
 * endpoint de histórico uma vez (caminho frio, RF-38) e ficam estáticas.
 */
export function LiveFeeChartCard() {
  const [window_, setWindow] = useState<ChartWindow>('live');
  const [coldResult, setColdResult] = useState<ColdFetch | null>(null);
  const [retryTick, setRetryTick] = useState(0);

  // Seletor condicional: em janela fria devolve null (constante) — o card
  // fica surdo ao stream e não re-renderiza a cada bloco (RNF-03).
  const liveSelector = useCallback(
    (s: FeesState) => (window_ === 'live' ? s.liveHistory : null),
    [window_],
  );
  const liveData = useFeesSlice(liveSelector);

  useEffect(() => {
    if (window_ === 'live') return;
    let cancelled = false;
    fetchHistoryWindow(window_).then(
      (data) =>
        !cancelled &&
        setColdResult({ window: window_, tick: retryTick, state: 'ready', data }),
      () =>
        !cancelled &&
        setColdResult({ window: window_, tick: retryTick, state: 'error' }),
    );
    return () => {
      cancelled = true;
    };
  }, [window_, retryTick]);

  const cold: ColdFetch | { state: 'loading' } =
    coldResult && coldResult.window === window_ && coldResult.tick === retryTick
      ? coldResult
      : { state: 'loading' };

  const isCold = window_ !== 'live' && window_ !== '1h';

  return (
    <section className="card chart-card">
      <header className="chart-card__head">
        <div className="chart-card__title">
          <h2>Base Fee &amp; Priority Fee</h2>
          {window_ === 'live' ? (
            <span className="badge badge--live">Live</span>
          ) : (
            isCold && <span className="badge badge--cold" title="Served from ClickHouse aggregates">cold path</span>
          )}
        </div>
        <div className="chart-card__controls">
          <SegmentedControl
            ariaLabel="Chart window"
            options={WINDOW_OPTIONS}
            value={window_}
            onChange={setWindow}
          />
        </div>
      </header>

      {window_ === 'live' ? (
        liveData && liveData.length > 0 ? (
          <FeeChartCore data={liveData} />
        ) : (
          <div className="chart-area chart-area--empty">
            <Skeleton width="100%" height={320} />
          </div>
        )
      ) : cold.state === 'loading' ? (
        <div className="chart-area chart-area--empty">
          <Skeleton width="100%" height={320} />
        </div>
      ) : cold.state === 'error' ? (
        <ErrorState title="History unavailable" onRetry={() => setRetryTick((t) => t + 1)}>
          The cold path (ClickHouse) did not answer. The live panel keeps working
          without it (RNF-30).
        </ErrorState>
      ) : cold.data.length === 0 ? (
        <div className="chart-area chart-area--empty">
          <p className="muted">No data for this window yet.</p>
        </div>
      ) : (
        <FeeChartCore data={cold.data} />
      )}
    </section>
  );
}
