import {
  Area,
  Bar,
  Brush,
  CartesianGrid,
  ComposedChart,
  Line,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { fmtCompactUsd, fmtDay, fmtSeriesAxis, fmtUsd } from '../../lib/format';
import { useChartZoom } from '../../hooks/useChartZoom';
import type { MetricDef } from '../../lib/metrics';
import type { MetricPoint, MetricSeries } from '../../types/contract';
import type { AxisScale, ChartStyle } from './MetricControls';

/**
 * Gráfico do explorador: a métrica no eixo esquerdo, o preço do ETH sobreposto
 * no direito. Dois eixos com escalas independentes — daí o "Mixed" quando uma
 * está em log e a outra em linear.
 */

interface Props {
  series: MetricSeries;
  points: MetricPoint[];
  def: MetricDef;
  style: ChartStyle;
  metricScale: AxisScale;
  priceScale: AxisScale;
  showPrice: boolean;
  /** Rótulo do eixo esquerdo quando a taxa de variação está ligada. */
  isPercent: boolean;
}

interface TooltipShape {
  active?: boolean;
  payload?: ReadonlyArray<{ payload?: unknown }>;
}

function makeTooltip(def: MetricDef, isPercent: boolean, showPrice: boolean) {
  return function ChartTooltip({ active, payload }: TooltipShape) {
    const point = payload?.[0]?.payload as MetricPoint | undefined;
    if (!active || !point) return null;
    return (
      <div className="chart-tooltip">
        <div className="chart-tooltip__date">{fmtDay(point.t)}</div>
        <div className="chart-tooltip__row">
          <span className="dot dot--base" />{' '}
          {isPercent
            ? `${point.value >= 0 ? '+' : ''}${point.value.toFixed(2)}%`
            : def.format(point.value)}
        </div>
        {showPrice && (
          <div className="chart-tooltip__row">
            <span className="dot dot--price" /> ETH: {fmtUsd(point.ethUsd)}
          </div>
        )}
      </div>
    );
  };
}

export function MetricChart({
  series,
  points,
  def,
  style,
  metricScale,
  priceScale,
  showPrice,
  isPercent,
}: Props) {
  const zoom = useChartZoom(points.length);
  // Log exige domínio estritamente positivo — sem isto o recharts renderiza vazio.
  const metricDomain: [number | string, number | string] =
    metricScale === 'log' ? ['auto', 'auto'] : isPercent ? ['auto', 'auto'] : [0, 'auto'];

  const formatMetricTick = (v: number) =>
    isPercent ? `${v.toFixed(0)}%` : def.formatAxis(v);

  const firstTimestamp = Date.parse(points[0]?.t ?? '');
  const lastTimestamp = Date.parse(points.at(-1)?.t ?? '');
  const spansMultipleYears = lastTimestamp - firstTimestamp > 400 * 86_400_000;

  const values = points.map((point) => point.value);
  const minValue = Math.min(...values);
  const maxValue = Math.max(...values);
  const zeroOffset =
    maxValue === minValue
      ? 0.5
      : Math.min(1, Math.max(0, maxValue / (maxValue - minValue)));

  let metricMark;
  switch (style) {
    case 'bar':
      metricMark = (
      <Bar
        yAxisId="metric"
        dataKey="value"
        fill="var(--accent)"
        maxBarSize={18}
        isAnimationActive={false}
      />
      );
      break;
    case 'area':
    case 'gradient':
      metricMark = (
        <Area
          yAxisId="metric"
          type={style === 'gradient' ? 'monotone' : 'linear'}
          dataKey="value"
          stroke="var(--accent)"
          strokeWidth={2}
          fill={style === 'gradient' ? 'url(#metricFill)' : 'var(--accent)'}
          fillOpacity={style === 'gradient' ? 1 : 0.13}
          isAnimationActive={false}
          dot={false}
        />
      );
      break;
    case 'baseline':
      metricMark = (
        <Area
          yAxisId="metric"
          type="monotone"
          dataKey="value"
          stroke="var(--accent)"
          strokeWidth={2}
          fill="url(#baselineFill)"
          isAnimationActive={false}
          dot={false}
        />
      );
      break;
    case 'dots':
      metricMark = (
        <Line
          yAxisId="metric"
          type="linear"
          dataKey="value"
          stroke="transparent"
          isAnimationActive={false}
          dot={{ r: 2.2, fill: 'var(--accent)', strokeWidth: 0 }}
          activeDot={{ r: 4, fill: 'var(--accent)', stroke: 'var(--surface)', strokeWidth: 2 }}
        />
      );
      break;
    case 'step':
    case 'smooth':
    case 'line':
      metricMark = (
        <Line
          yAxisId="metric"
          type={style === 'step' ? 'stepAfter' : style === 'smooth' ? 'monotone' : 'linear'}
          dataKey="value"
          stroke="var(--accent)"
          strokeWidth={2}
          isAnimationActive={false}
          dot={false}
        />
      );
      break;
  }

  return (
    <div className="chart-area chart-area--zoomable" {...zoom.handlers}>
      <div className="chart-zoom-hint" aria-hidden="true">
        Pinça ou Ctrl+roda para ampliar · duplo clique para restaurar
      </div>
      {zoom.isZoomed && (
        <button type="button" className="chart-zoom-reset" onClick={zoom.reset}>
          Restaurar zoom
        </button>
      )}
      <div className="chart-legend" aria-hidden="true">
        <span><i className="dot dot--base" />{def.nav}</span>
        {showPrice && <span><i className="dot dot--price" />Price</span>}
      </div>
      <div className="chart-watermark" aria-hidden="true">Alphractal</div>
      <ResponsiveContainer width="100%" height={520}>
        <ComposedChart data={points} margin={{ top: 12, right: 8, bottom: 0, left: 0 }}>
          <defs>
            <linearGradient id="metricFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="var(--accent)" stopOpacity={0.24} />
              <stop offset="100%" stopColor="var(--accent)" stopOpacity={0.02} />
            </linearGradient>
            <linearGradient id="baselineFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="var(--green)" stopOpacity={0.3} />
              <stop offset={`${zeroOffset * 100}%`} stopColor="var(--green)" stopOpacity={0.08} />
              <stop offset={`${zeroOffset * 100}%`} stopColor="var(--red)" stopOpacity={0.08} />
              <stop offset="100%" stopColor="var(--red)" stopOpacity={0.3} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke="var(--chart-grid)" strokeDasharray="4 6" vertical={false} />
          <XAxis
            dataKey="t"
            tickFormatter={(t: string) =>
              spansMultipleYears
                ? String(new Date(t).getUTCFullYear())
                : fmtSeriesAxis(t, series.resolution)
            }
            tick={{ fill: 'var(--text-faint)', fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            minTickGap={56}
          />
          {points.length > 2 && (
            <Brush
              dataKey="t"
              height={28}
              travellerWidth={10}
              startIndex={zoom.range.startIndex}
              endIndex={zoom.range.endIndex}
              stroke="var(--border-strong)"
              fill="var(--surface-2)"
              tickFormatter={(t: string) => fmtSeriesAxis(t, series.resolution)}
              onChange={(next: { startIndex?: number; endIndex?: number }) => {
                if (next.startIndex === undefined || next.endIndex === undefined) return;
                zoom.setRange({ startIndex: next.startIndex, endIndex: next.endIndex });
              }}
            />
          )}
          <YAxis
            yAxisId="metric"
            scale={metricScale}
            domain={metricDomain}
            allowDataOverflow={metricScale === 'log'}
            tickFormatter={formatMetricTick}
            tick={{ fill: 'var(--text-faint)', fontSize: 12 }}
            axisLine={false}
            tickLine={false}
            width={70}
          />
          {showPrice && (
            <YAxis
              yAxisId="price"
              orientation="right"
              scale={priceScale}
              domain={priceScale === 'log' ? ['auto', 'auto'] : [0, 'auto']}
              allowDataOverflow={priceScale === 'log'}
              tickFormatter={fmtCompactUsd}
              tick={{ fill: 'var(--text-faint)', fontSize: 12 }}
              axisLine={false}
              tickLine={false}
              width={64}
            />
          )}
          <Tooltip
            content={makeTooltip(def, isPercent, showPrice)}
            cursor={{ stroke: 'var(--chart-cursor)', strokeWidth: 1 }}
          />
          {isPercent && (
            <ReferenceLine yAxisId="metric" y={0} stroke="var(--chart-cursor)" />
          )}
          {showPrice && (
            <Line
              yAxisId="price"
              type="monotone"
              dataKey="ethUsd"
              stroke="var(--text-faint)"
              strokeWidth={1.4}
              strokeDasharray="3 3"
              isAnimationActive={false}
              dot={false}
            />
          )}
          {metricMark}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
