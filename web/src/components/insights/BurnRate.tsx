import { useFeesSlice } from '../../hooks/useFeesSlice';
import { fmtEthAmount } from '../../lib/format';
import { Card } from '../ui/Card';
import { FlameIcon } from '../ui/icons';

/**
 * Painel "EIP-1559 BURN RATE" do design — ETH queimado por minuto (a base fee
 * é destruída, não paga ao validador). ► D-06 (backlog): mock-only, some sozinho
 * se o endpoint não existir.
 */
export function BurnRate() {
  const insights = useFeesSlice((s) => s.insights);
  if (!insights) return null;

  return (
    <Card className="insight">
      <header className="card__head">
        <h2>EIP-1559 burn rate</h2>
        <span className="badge badge--burn">
          <FlameIcon size={12} /> burning
        </span>
      </header>
      <p className="burn__value">
        {insights.burnRateEthPerMin.toFixed(2)} <small>ETH/min</small>
      </p>
      <p className="burn__sub">{fmtEthAmount(insights.burned24hEth)} burned in the last 24h</p>
    </Card>
  );
}
