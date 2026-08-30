import { useFeesSlice } from '../hooks/useFeesSlice';
import { usePreferences } from '../hooks/usePreferences';
import { AlertBanner } from '../components/fees/AlertBanner';
import { FeeTierCard } from '../components/fees/FeeTierCard';
import { TxCostEstimator } from '../components/fees/TxCostEstimator';
import { BurnRate } from '../components/insights/BurnRate';
import { LiveFeeChartCard } from '../components/live/BaseFeeChart';
import { ConnectionBar } from '../components/live/ConnectionBar';
import { ErrorState } from '../components/ui/ErrorState';
import { SegmentedControl } from '../components/ui/SegmentedControl';

/**
 * A tela principal — o painel da aba "Fees" (RF-22 a RF-33), na composição do
 * design. A view só COMPÕE: quem assina o stream são as folhas, cada uma com a
 * sua fatia, para a árvore acima delas ficar parada entre blocos (RNF-03).
 */
export function RealTimeGasView() {
  const { unit, update } = usePreferences();
  // Primitivo: só vira true/false quando a conexão morre de vez sem nunca ter
  // recebido dado (RF-32) — não acorda a view a cada bloco.
  const deadOnArrival = useFeesSlice((s) => s.status === 'erro' && s.snapshot === null);

  if (deadOnArrival) {
    return (
      <div className="view">
        <ConnectionBar />
        <ErrorState title="Could not reach the fees API" onRetry={() => location.reload()}>
          The stream is offline and no snapshot was received. Check that the API
          is running, then try again.
        </ErrorState>
      </div>
    );
  }

  return (
    <div className="view">
      <ConnectionBar />
      <AlertBanner />

      <div className="tiers__toolbar">
        <SegmentedControl
          ariaLabel="Display unit"
          options={[
            { value: 'gwei', label: 'gwei' },
            { value: 'eth', label: 'ETH' },
            { value: 'usd', label: 'USD' },
          ]}
          value={unit}
          onChange={(u) => update({ unit: u })}
        />
      </div>

      <div className="tiers">
        <FeeTierCard id="slow" />
        <FeeTierCard id="standard" />
        <FeeTierCard id="fast" />
      </div>

      <LiveFeeChartCard />

      <TxCostEstimator />

      <BurnRate />
    </div>
  );
}
