import { useFeesSlice } from '../../hooks/useFeesSlice';
import { fmtGwei, fmtInt, fmtPct, fmtUsd } from '../../lib/format';
import { ArrowDownIcon, ArrowUpIcon, EthIcon } from '../ui/icons';
import { Skeleton } from '../ui/Skeleton';

/**
 * Faixa de métricas do design. O preço vive num assinante separado: ticks da
 * Coinbase não reconciliam base fee e bloco desnecessariamente.
 */
export function MetricsHeader() {
  const snapshot = useFeesSlice((s) => s.snapshot);

  return (
    <div className="metrics">
      <div className="metrics__asset">
        <span className="metrics__asset-icon">
          <EthIcon size={20} />
        </span>
        <h1>
          Ethereum <span>(ETH)</span>
        </h1>
      </div>

      <dl className="metrics__list">
        <CurrentPriceMetric />

        <div className="metric">
          <dt>Base fee</dt>
          <dd>
            {snapshot ? (
              <>
                {fmtGwei(snapshot.baseFeeGwei)} <small>gwei</small>
                {/* RF-29 — direção em relação ao bloco anterior */}
                {snapshot.trend === 'subindo' && (
                  <span className="trend trend--up" title="Rising vs previous block">
                    <ArrowUpIcon size={13} />
                  </span>
                )}
                {snapshot.trend === 'caindo' && (
                  <span className="trend trend--down" title="Falling vs previous block">
                    <ArrowDownIcon size={13} />
                  </span>
                )}
              </>
            ) : (
              <Skeleton width={80} height={20} />
            )}
          </dd>
        </div>

        <div className="metric">
          <dt>Block</dt>
          <dd>
            {snapshot ? fmtInt(snapshot.blockNumber) : <Skeleton width={95} height={20} />}
          </dd>
        </div>
      </dl>
    </div>
  );
}

function CurrentPriceMetric() {
  const price = useFeesSlice((state) => state.ethPrice);

  return (
    <div className="metric">
      <dt>Current price</dt>
      <dd>
        {price ? (
          <>
            {fmtUsd(price.price)}
            <span className={price.change24hPct >= 0 ? 'badge badge--up' : 'badge badge--down'}>
              {fmtPct(price.change24hPct)}
            </span>
          </>
        ) : (
          <Skeleton width={110} height={20} />
        )}
      </dd>
    </div>
  );
}
