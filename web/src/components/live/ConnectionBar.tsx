import { useFeesSlice } from '../../hooks/useFeesSlice';
import { CongestionBadge } from '../fees/CongestionBadge';
import { BlockAge } from './BlockAge';
import type { StreamStatus } from '../../types/contract';

/**
 * A faixa "LIVE — connected via SSE · 2s ago" do design (RF-26).
 * O design original dizia "WebSocket", mas o transporte do painel é SSE
 * (RF-16 / dúvida 12) — exibir o nome errado seria pergunta perdida em banca.
 */
const STATUS: Record<StreamStatus, { label: string; className: string }> = {
  conectando: { label: 'CONNECTING…', className: 'connection--connecting' },
  'ao-vivo': { label: 'LIVE — connected via SSE', className: 'connection--live' },
  reconectando: { label: 'RECONNECTING…', className: 'connection--reconnecting' },
  atrasado: { label: 'STALE DATA — no new block', className: 'connection--stale' },
  erro: { label: 'OFFLINE', className: 'connection--offline' },
};

export function ConnectionBar() {
  const status = useFeesSlice((s) => s.status);
  const { label, className } = STATUS[status];

  return (
    <div className={`connection ${className}`}>
      <span className="connection__dot" aria-hidden="true" />
      <span className="connection__label" role="status">
        {label}
      </span>
      {(status === 'ao-vivo' || status === 'atrasado') && <BlockAge />}
      <span className="connection__spacer" />
      <CongestionBadge />
    </div>
  );
}
