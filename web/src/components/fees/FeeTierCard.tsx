import { useFeesSlice } from '../../hooks/useFeesSlice';
import { usePreferences } from '../../hooks/usePreferences';
import { fmtEta, fmtEth, fmtGwei, fmtUsd } from '../../lib/format';
import type { TierId } from '../../types/contract';
import { Card } from '../ui/Card';
import { Skeleton } from '../ui/Skeleton';
import { BikeIcon, RocketIcon, TramIcon } from '../ui/icons';

/**
 * Um card de faixa do design — SLOW / STANDARD / FAST (RF-22), com o FAST
 * destacado em âmbar. A unidade principal segue a preferência (RF-28); as
 * outras duas viram a linha "Est.". Nenhum cálculo aqui: a API já mandou
 * gwei, ETH e USD prontos (RN-09).
 */

const META: Record<TierId, { label: string; Icon: typeof BikeIcon }> = {
  slow: { label: 'Slow', Icon: BikeIcon },
  standard: { label: 'Standard', Icon: TramIcon },
  fast: { label: 'Fast', Icon: RocketIcon },
};

export function FeeTierCard({ id }: { id: TierId }) {
  const tier = useFeesSlice((s) => s.snapshot?.tiers[id] ?? null);
  const { unit } = usePreferences();
  const { label, Icon } = META[id];

  const isFast = id === 'fast';

  if (!tier) {
    return (
      <Card className={isFast ? 'tier tier--fast' : 'tier'}>
        <header className="tier__head">
          <span className="tier__name">
            <Icon size={16} /> {label}
          </span>
        </header>
        <Skeleton width={120} height={30} />
        <Skeleton width={90} />
      </Card>
    );
  }

  const main =
    unit === 'gwei' ? (
      <>
        {fmtGwei(tier.maxFeeGwei)} <small>gwei</small>
      </>
    ) : unit === 'usd' ? (
      fmtUsd(tier.estUsd)
    ) : (
      fmtEth(tier.estEth)
    );

  const sub =
    unit === 'gwei'
      ? `Est. ${fmtUsd(tier.estUsd)} USD`
      : unit === 'usd'
        ? `${fmtGwei(tier.maxFeeGwei)} gwei`
        : `Est. ${fmtUsd(tier.estUsd)} USD`;

  return (
    <Card className={isFast ? 'tier tier--fast' : 'tier'}>
      <header className="tier__head">
        <span className="tier__name">
          <Icon size={16} /> {label}
        </span>
        <span className="tier__eta">{fmtEta(tier.etaSeconds)}</span>
      </header>
      <p className="tier__value">{main}</p>
      <p className="tier__sub">{sub}</p>
    </Card>
  );
}
