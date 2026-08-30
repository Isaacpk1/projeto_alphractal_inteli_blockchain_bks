import { useId, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { METRICS, metricPath } from '../../lib/metrics';
import type { MetricId } from '../../types/contract';
import {
  BoltIcon,
  BookIcon,
  ChevronDownIcon,
  ChevronLeftIcon,
  FlameIcon,
  GaugeIcon,
  GlobeIcon,
  ListIcon,
  MeanIcon,
  NetworkIcon,
  SearchIcon,
  SumIcon,
} from '../ui/icons';

type Icon = typeof BoltIcon;

const METRIC_ICON: Record<MetricId, Icon> = {
  'total-fees-eth': SumIcon,
  'total-fees-usd': SumIcon,
  'mean-tx-fee-eth': MeanIcon,
  'mean-tx-fee-usd': MeanIcon,
  'mean-fee-per-gas': GaugeIcon,
};

const FEATURED = ['SOPR Trend Signal', 'Liquidation Levels', 'Whale vs Retail Delta', 'Open Interest'];

const METRIC_GROUPS: ReadonlyArray<{ label: string; icon: Icon }> = [
  { label: 'Summary', icon: ListIcon },
  { label: 'Alpha Metrics', icon: NetworkIcon },
  { label: 'Lifespan', icon: GaugeIcon },
  { label: 'Valuation Models', icon: ListIcon },
  { label: 'Market', icon: GlobeIcon },
  { label: 'Derivatives', icon: MeanIcon },
  { label: 'Cycle', icon: GaugeIcon },
  { label: 'Social', icon: MeanIcon },
  { label: 'Addresses', icon: BookIcon },
  { label: 'Transactions', icon: MeanIcon },
  { label: 'Mining', icon: BoltIcon },
  { label: 'Supply', icon: GaugeIcon },
  { label: 'Exchanges', icon: ListIcon },
  { label: 'Network', icon: NetworkIcon },
];

const linkClass = ({ isActive }: { isActive: boolean }) =>
  isActive ? 'metrics-nav__fee-link is-active' : 'metrics-nav__fee-link';

function BlockedMetricItem({
  label,
  icon: IconComponent,
  onUnavailable,
}: {
  label: string;
  icon: Icon;
  onUnavailable: () => void;
}) {
  return (
    <button type="button" className="metrics-nav__item" onClick={onUnavailable}>
      <IconComponent size={17} />
      <span>{label}</span>
    </button>
  );
}

/** Navegação interna de Metrics. Somente as quatro métricas de Fees são rotas. */
export function MetricsSidebar({ onUnavailable }: { onUnavailable: () => void }) {
  const { pathname, search } = useLocation();
  const featuredId = useId();
  const [featuredOpen, setFeaturedOpen] = useState(true);
  const carriedSearch = pathname.startsWith('/metrics/') ? search : '';
  const feeMetrics = METRICS.slice(0, 4);

  return (
    <aside className="metrics-sidebar">
      <div className="metrics-sidebar__head">
        <button type="button" className="metrics-sidebar__back" onClick={onUnavailable}>
          <ChevronLeftIcon size={17} />
          <span>Metrics</span>
        </button>
        <button type="button" className="metrics-sidebar__tool" aria-label="Buscar métricas" onClick={onUnavailable}>
          <SearchIcon size={17} />
        </button>
      </div>

      <nav className="metrics-nav" aria-label="Navegação de métricas">
        <button
          type="button"
          className="metrics-nav__section"
          aria-expanded={featuredOpen}
          aria-controls={featuredId}
          onClick={() => setFeaturedOpen((open) => !open)}
        >
          <FlameIcon size={17} />
          <strong>Featured Metrics</strong>
          <ChevronDownIcon
            className={
              featuredOpen
                ? 'metrics-nav__chevron'
                : 'metrics-nav__chevron is-collapsed'
            }
            size={15}
          />
        </button>
        {featuredOpen && (
          <div className="metrics-nav__featured" id={featuredId}>
            {FEATURED.map((label, index) => (
              <button type="button" key={label} onClick={onUnavailable}>
                <span>{label}</span>
                {index < 3 && <i aria-hidden="true" />}
              </button>
            ))}
          </div>
        )}

        <div className="metrics-nav__groups">
          {METRIC_GROUPS.map((item) => (
            <BlockedMetricItem key={item.label} {...item} onUnavailable={onUnavailable} />
          ))}
        </div>

        <div className="metrics-nav__fees">
          <div className="metrics-nav__fees-title">
            <span className="metrics-nav__dollar">$</span>
            <strong>Fees</strong>
            <ChevronDownIcon size={15} />
          </div>
          <div className="metrics-nav__fee-links">
            {feeMetrics.map((metric) => {
              const IconComponent = METRIC_ICON[metric.id];
              return (
                <NavLink
                  key={metric.id}
                  to={{ pathname: metricPath(metric.id), search: carriedSearch }}
                  className={linkClass}
                >
                  <IconComponent size={15} />
                  <span>{metric.nav}</span>
                </NavLink>
              );
            })}
          </div>
        </div>

        <BlockedMetricItem label="Technical" icon={MeanIcon} onUnavailable={onUnavailable} />
      </nav>
    </aside>
  );
}
