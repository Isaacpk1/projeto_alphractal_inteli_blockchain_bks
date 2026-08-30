import { useId } from 'react';
import { ROC_LABEL, SMOOTHING_WINDOWS } from '../../lib/series';
import type { RateOfChange, SmoothingKind } from '../../lib/series';
import { PERIOD_LABEL } from '../../lib/transport';
import type { MetricPeriod } from '../../lib/transport';
import { SegmentedControl } from '../ui/SegmentedControl';

/**
 * Barra de controles do explorador — as mesmas dimensões que a plataforma do
 * parceiro oferece nas métricas de taxa: rate of change, style, scale,
 * smoothing e período.
 */

export type ChartStyle =
  | 'line'
  | 'smooth'
  | 'step'
  | 'area'
  | 'gradient'
  | 'baseline'
  | 'bar'
  | 'dots';
export type AxisScale = 'linear' | 'log';

export interface ExplorerControls {
  period: MetricPeriod;
  style: ChartStyle;
  roc: RateOfChange;
  smoothing: SmoothingKind;
  smoothingWindow: number;
  metricScale: AxisScale;
  priceScale: AxisScale;
  showPrice: boolean;
}

const PERIODS: readonly MetricPeriod[] = ['24h', '7d', '30d', '90d', 'ytd', '1y', 'all'];

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="control">
      <span className="control__label">{label}</span>
      {children}
    </label>
  );
}

export function MetricControls({
  value,
  onChange,
}: {
  value: ExplorerControls;
  onChange: (patch: Partial<ExplorerControls>) => void;
}) {
  const priceId = useId();
  // Escala log exige valores positivos; taxa de variação é negativa metade do
  // tempo. Em vez de gerar um gráfico vazio, o controle desabilita e explica.
  const logBlocked = value.roc !== 'none';

  return (
    <div className="controls">
      <Field label="Rate of change">
        <select
          value={value.roc}
          onChange={(e) => {
            const roc = e.target.value as RateOfChange;
            onChange(
              roc === 'none'
                ? { roc }
                : { roc, metricScale: 'linear' }, // log não sobrevive a valores negativos
            );
          }}
        >
          {(Object.keys(ROC_LABEL) as RateOfChange[]).map((k) => (
            <option key={k} value={k}>
              {ROC_LABEL[k]}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Style">
        <select
          value={value.style}
          onChange={(e) => onChange({ style: e.target.value as ChartStyle })}
        >
          <option value="line">Line</option>
          <option value="smooth">Smooth line</option>
          <option value="step">Step line</option>
          <option value="area">Area</option>
          <option value="gradient">Gradient area</option>
          <option value="baseline">Baseline</option>
          <option value="bar">Bars</option>
          <option value="dots">Dots</option>
        </select>
      </Field>

      <Field label="Smoothing">
        <select
          value={value.smoothing === 'none' ? 'none' : `${value.smoothing}-${value.smoothingWindow}`}
          onChange={(e) => {
            const raw = e.target.value;
            if (raw === 'none') return onChange({ smoothing: 'none' });
            const [kind, days] = raw.split('-');
            onChange({
              smoothing: kind as SmoothingKind,
              smoothingWindow: Number(days),
            });
          }}
        >
          <option value="none">None</option>
          <optgroup label="SMA">
            {SMOOTHING_WINDOWS.map((d) => (
              <option key={`sma-${d}`} value={`sma-${d}`}>{`SMA ${d}d`}</option>
            ))}
          </optgroup>
          <optgroup label="EMA">
            {SMOOTHING_WINDOWS.map((d) => (
              <option key={`ema-${d}`} value={`ema-${d}`}>{`EMA ${d}d`}</option>
            ))}
          </optgroup>
        </select>
      </Field>

      <Field label="Metric scale">
        <select
          value={value.metricScale}
          disabled={logBlocked}
          title={logBlocked ? 'Log não se aplica a variação percentual (valores negativos)' : undefined}
          onChange={(e) => onChange({ metricScale: e.target.value as AxisScale })}
        >
          <option value="linear">Linear</option>
          <option value="log">Log</option>
        </select>
      </Field>

      <Field label="Price scale">
        <select
          value={value.priceScale}
          disabled={!value.showPrice}
          onChange={(e) => onChange({ priceScale: e.target.value as AxisScale })}
        >
          <option value="linear">Linear</option>
          <option value="log">Log</option>
        </select>
      </Field>

      <label className="control control--check" htmlFor={priceId}>
        <input
          id={priceId}
          type="checkbox"
          checked={value.showPrice}
          onChange={(e) => onChange({ showPrice: e.target.checked })}
        />
        <span>ETH price overlay</span>
      </label>

      <div className="controls__period">
        <SegmentedControl
          ariaLabel="Period"
          options={PERIODS.map((p) => ({ value: p, label: PERIOD_LABEL[p] }))}
          value={value.period}
          onChange={(period) => onChange({ period })}
        />
      </div>
    </div>
  );
}
