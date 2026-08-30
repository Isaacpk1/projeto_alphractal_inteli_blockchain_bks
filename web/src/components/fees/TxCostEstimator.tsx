import { useId } from 'react';
import { useFeesSlice } from '../../hooks/useFeesSlice';
import { usePreferences } from '../../hooks/usePreferences';
import { fmtEth, fmtInt, fmtUsd } from '../../lib/format';
import type { TierId } from '../../types/contract';
import { Card } from '../ui/Card';
import { Skeleton } from '../ui/Skeleton';

/**
 * RF-27 — o usuário escolhe o tipo de transação e vê o custo nas três faixas.
 * Os custos já vêm calculados pela API em snapshot.txEstimates (RF-11);
 * aqui é só seleção e formatação.
 */

const TIER_LABEL: Record<TierId, string> = {
  slow: 'Slow',
  standard: 'Standard',
  fast: 'Fast',
};

export function TxCostEstimator() {
  const estimates = useFeesSlice((s) => s.snapshot?.txEstimates ?? null);
  const { txTypeId, update } = usePreferences();
  const selectId = useId();

  if (!estimates) {
    return (
      <Card className="estimator">
        <header className="card__head">
          <h2>Cost by transaction type</h2>
        </header>
        <Skeleton width="100%" height={72} />
      </Card>
    );
  }

  const selected = estimates.find((e) => e.id === txTypeId) ?? estimates[0];
  if (!selected) return null;

  return (
    <Card className="estimator">
      <header className="card__head">
        <h2>Cost by transaction type</h2>
        <div className="estimator__picker">
          <label htmlFor={selectId} className="sr-only">
            Transaction type
          </label>
          <select
            id={selectId}
            value={selected.id}
            onChange={(e) => update({ txTypeId: e.target.value })}
          >
            {estimates.map((e) => (
              <option key={e.id} value={e.id}>
                {e.label}
              </option>
            ))}
          </select>
          <span className="estimator__gas">{fmtInt(selected.gasLimit)} gas</span>
        </div>
      </header>

      <div className="estimator__grid">
        {(Object.keys(TIER_LABEL) as TierId[]).map((tier) => (
          <div key={tier} className={`estimator__cell${tier === 'fast' ? ' is-fast' : ''}`}>
            <span className="estimator__tier">{TIER_LABEL[tier]}</span>
            <strong>{fmtUsd(selected.tiers[tier].usd)}</strong>
            <span className="estimator__eth">{fmtEth(selected.tiers[tier].eth)}</span>
          </div>
        ))}
      </div>
    </Card>
  );
}
