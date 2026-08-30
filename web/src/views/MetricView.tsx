import { useCallback, useEffect, useMemo, useState } from 'react';
import { Navigate, useParams, useSearchParams } from 'react-router-dom';
import { MetricChart } from '../components/metrics/MetricChart';
import { MetricControls } from '../components/metrics/MetricControls';
import type {
  AxisScale,
  ChartStyle,
  ExplorerControls,
} from '../components/metrics/MetricControls';
import { ErrorState } from '../components/ui/ErrorState';
import { Skeleton } from '../components/ui/Skeleton';
import { DownloadIcon, EthIcon } from '../components/ui/icons';
import { fetchMetricSeries } from '../lib/feesStore';
import { fmtDay } from '../lib/format';
import { DEFAULT_METRIC, findMetric, metricPath } from '../lib/metrics';
import type { MetricDef } from '../lib/metrics';
import { ROC_DAYS, ROC_LABEL, downloadCsv, toCsv, transformSeries } from '../lib/series';
import type { RateOfChange, SmoothingKind } from '../lib/series';
import { PERIOD_LABEL } from '../lib/transport';
import type { MetricPeriod } from '../lib/transport';
import type { MetricPoint, MetricSeries } from '../types/contract';

/**
 * Explorador de uma métrica histórica de taxa. Cada métrica é uma ROTA
 * (`/metrics/:metricId`) e aparece na navegação lateral.
 *
 * Inteiramente CAMINHO FRIO: agregados do ClickHouse via API (RF-37/RF-38).
 * Não assina o stream. Se o caminho frio cair, esta página falha sozinha e o
 * painel ao vivo nem fica sabendo (RNF-30) — e se a semana 3 apertar, as rotas
 * inteiras saem sem tocar no resto.
 *
 * A métrica vem da rota; os controles (período, suavização, escalas) vivem na
 * query string, então o botão Share entrega um link que reproduz a visão
 * exata, e trocar de métrica na sidebar preserva a lente escolhida.
 */

const DEFAULTS: ExplorerControls = {
  period: 'all',
  style: 'line',
  roc: 'none',
  smoothing: 'none',
  smoothingWindow: 30,
  metricScale: 'linear',
  priceScale: 'linear',
  showPrice: true,
};


/** Referência estável para o estado vazio — não recriar dentro do memo. */
const NO_POINTS: MetricPoint[] = [];

/** Lê um link compartilhado. Só roda na montagem. */
function readState(p: URLSearchParams): ExplorerControls {
  return {
    period: (p.get('p') as MetricPeriod | null) ?? DEFAULTS.period,
    style: (p.get('s') as ChartStyle | null) ?? DEFAULTS.style,
    roc: (p.get('roc') as RateOfChange | null) ?? DEFAULTS.roc,
    smoothing: (p.get('sm') as SmoothingKind | null) ?? DEFAULTS.smoothing,
    smoothingWindow: Number(p.get('smw')) || DEFAULTS.smoothingWindow,
    metricScale: (p.get('ms') as AxisScale | null) ?? DEFAULTS.metricScale,
    priceScale: (p.get('ps') as AxisScale | null) ?? DEFAULTS.priceScale,
    showPrice: p.get('price') !== '0',
  };
}

/** Serializa para o Share. Valores no padrão são omitidos, para o link ficar curto. */
function writeState(s: ExplorerControls): string {
  const p = new URLSearchParams();
  const set = (key: string, value: string, fallback: string) => {
    if (value !== fallback) p.set(key, value);
  };
  set('p', s.period, DEFAULTS.period);
  set('s', s.style, DEFAULTS.style);
  set('roc', s.roc, DEFAULTS.roc);
  set('sm', s.smoothing, DEFAULTS.smoothing);
  set('smw', String(s.smoothingWindow), String(DEFAULTS.smoothingWindow));
  set('ms', s.metricScale, DEFAULTS.metricScale);
  set('ps', s.priceScale, DEFAULTS.priceScale);
  set('price', s.showPrice ? '1' : '0', '1');
  return p.toString();
}

type Fetch =
  | { key: string; state: 'error' }
  | { key: string; state: 'ready'; series: MetricSeries };

/**
 * Resolve a métrica da rota. Fica separado do explorador para que um id
 * inválido possa redirecionar sem quebrar a ordem dos hooks.
 */
export function MetricView() {
  const { metricId } = useParams();
  const def = findMetric(metricId);
  if (!def) return <Navigate to={metricPath(DEFAULT_METRIC.id)} replace />;
  return <MetricExplorer def={def} />;
}

function MetricExplorer({ def }: { def: MetricDef }) {
  const [params, setParams] = useSearchParams();
  const [retryTick, setRetryTick] = useState(0);
  const [result, setResult] = useState<Fetch | null>(null);
  const metric = def.id;

  // ── estado do explorador ────────────────────────────────────────────────
  // Fonte da verdade é o estado local; a URL é ESPELHO (o link continua
  // reproduzindo a visão) e entrada só na montagem. Tentar usar a URL como
  // fonte da verdade cria
  // corrida: `setParams` propaga de forma assíncrona, então duas mudanças de
  // controle em sequência rápida leem a mesma URL antiga e uma apaga a outra.
  // Como todas as escritas usam `replace`, não há histórico intra-página para
  // reconciliar — voltar/avançar sai da tela, e não há o que ressincronizar.
  const [controls, setControls] = useState<ExplorerControls>(() => readState(params));

  const update = useCallback(
    (patch: Partial<ExplorerControls>) => setControls((prev) => ({ ...prev, ...patch })),
    [],
  );

  const search = writeState(controls);
  useEffect(() => {
    if (search !== window.location.search.replace(/^\?/, '')) {
      setParams(search, { replace: true });
    }
  }, [search, setParams]);

  // ── busca ───────────────────────────────────────────────────────────────
  // Quanto de série ANTES do período a transformação precisa mastigar. Sem
  // isto, "YoY sobre 1 ano" não teria com o que comparar e o gráfico sairia
  // vazio — o pedido cresce com a suavização e com a taxa de variação.
  const lookbackDays =
    (controls.smoothing !== 'none' ? controls.smoothingWindow : 0) +
    (controls.roc !== 'none' ? ROC_DAYS[controls.roc] : 0);

  const key = `${metric}|${controls.period}|${lookbackDays}|${retryTick}`;

  useEffect(() => {
    let cancelled = false;
    fetchMetricSeries(metric, controls.period, lookbackDays).then(
      (series) => !cancelled && setResult({ key, state: 'ready', series }),
      () => !cancelled && setResult({ key, state: 'error' }),
    );
    return () => {
      cancelled = true;
    };
  }, [metric, controls.period, lookbackDays, key]);

  const fetchState: Fetch | { state: 'loading' } =
    result && result.key === key ? result : { state: 'loading' };

  // ── derivações ──────────────────────────────────────────────────────────
  const isPercent = controls.roc !== 'none';

  // Deriva de `result`, não de `fetchState`: este último é um literal novo a
  // cada render quando está carregando, e invalidaria o memo sempre.
  const points = useMemo(() => {
    if (!result || result.key !== key || result.state !== 'ready') return NO_POINTS;
    const transformed = transformSeries(result.series, {
      smoothing: controls.smoothing,
      smoothingWindow: controls.smoothingWindow,
      roc: controls.roc,
    });
    // Descarta o lead-in: ele existiu só para alimentar a transformação, e
    // exibi-lo mostraria mais período do que o usuário pediu.
    const from = result.series.from;
    return transformed.filter((p) => p.t >= from);
  }, [result, key, controls.smoothing, controls.smoothingWindow, controls.roc]);

  const latest = points.at(-1);
  const first = points[0];
  const changePct =
    first && latest && first.value !== 0 && !isPercent
      ? ((latest.value - first.value) / Math.abs(first.value)) * 100
      : null;

  const exportCsv = () => {
    if (fetchState.state !== 'ready') return;
    downloadCsv(`${metric}-${controls.period}.csv`, toCsv(points, def.csvHeader));
  };

  return (
    <div className="view">
      <section className="card chart-card">
        <header className="explorer__head">
          <div className="explorer__title">
            <h1>
              <span className="explorer__asset-icon">
                <EthIcon size={18} />
              </span>
              Ethereum: {def.nav}
              <span
                className="badge badge--cold"
                title="Agregados do caminho frio (ETL Python → ClickHouse). O painel ao vivo não depende desta página."
              >
                cold path
              </span>
            </h1>
            <p className="explorer__desc">{def.description}</p>
          </div>
          {/* Só exportação: é o RF-14 do BRD. Favoritar, compartilhar e tela
              cheia eram cromo da plataforma de referência, sem requisito. */}
          <div className="explorer__actions">
            <button
              type="button"
              className="icon-btn"
              aria-label="Export CSV"
              disabled={fetchState.state !== 'ready'}
              onClick={exportCsv}
            >
              <DownloadIcon size={15} />
            </button>
          </div>
        </header>

        <div className="explorer__value">
          {fetchState.state === 'ready' && latest ? (
            <>
              <strong>
                {isPercent
                  ? `${latest.value >= 0 ? '+' : ''}${latest.value.toFixed(2)}%`
                  : def.format(latest.value)}
              </strong>
              {changePct !== null && (
                <span className={changePct >= 0 ? 'badge badge--up' : 'badge badge--down'}>
                  {changePct >= 0 ? '+' : ''}
                  {changePct.toFixed(1)}% no período
                </span>
              )}
              <span className="explorer__meta">
                {PERIOD_LABEL[controls.period]} ·{' '}
                {fetchState.series.resolution === '1h' ? 'resolução 1h' : 'resolução 1d'}
                {controls.roc !== 'none' && ` · ${ROC_LABEL[controls.roc]}`}
                {controls.smoothing !== 'none' &&
                  ` · ${controls.smoothing.toUpperCase()} ${controls.smoothingWindow}d`}
                {latest && ` · até ${fmtDay(latest.t)}`}
              </span>
            </>
          ) : (
            <Skeleton width={220} height={30} />
          )}
        </div>

        <MetricControls value={controls} onChange={update} />

        {fetchState.state === 'loading' ? (
          <div className="chart-area chart-area--empty">
            <Skeleton width="100%" height={520} />
          </div>
        ) : fetchState.state === 'error' ? (
          <ErrorState title="Métrica indisponível" onRetry={() => setRetryTick((t) => t + 1)}>
            O ClickHouse não respondeu. O painel Real-Time Gas continua
            funcionando — esta é a única página que depende do caminho frio (RNF-30).
          </ErrorState>
        ) : points.length === 0 ? (
          <div className="chart-area chart-area--empty">
            <p className="muted">
              Sem dados suficientes para esta combinação — a janela de suavização ou
              a taxa de variação é maior que o período selecionado.
            </p>
          </div>
        ) : (
          <MetricChart
            series={fetchState.series}
            points={points}
            def={def}
            style={controls.style}
            metricScale={controls.metricScale}
            priceScale={controls.priceScale}
            showPrice={controls.showPrice}
            isPercent={isPercent}
          />
        )}
      </section>
    </div>
  );
}
