import { useCallback } from 'react';
import { useFeesSlice } from '../../hooks/useFeesSlice';
import { usePreferences } from '../../hooks/usePreferences';
import { fmtGwei } from '../../lib/format';
import type { FeesState } from '../../lib/feesStore';
import { BellIcon } from '../ui/icons';

/**
 * RF-30 — o alerta visual do limiar de gas. O usuário define "me avise quando a
 * base fee ficar abaixo de X gwei" no Settings; cruzou, esta faixa aparece.
 * Seletor devolve primitivo (número ou null) — só re-renderiza no cruzamento.
 */
export function AlertBanner() {
  const { alertBelowGwei } = usePreferences();

  const selector = useCallback(
    (s: FeesState) =>
      alertBelowGwei !== null &&
      s.snapshot !== null &&
      s.snapshot.baseFeeGwei <= alertBelowGwei
        ? s.snapshot.baseFeeGwei
        : null,
    [alertBelowGwei],
  );
  const triggeredAt = useFeesSlice(selector);

  if (triggeredAt === null || alertBelowGwei === null) return null;

  return (
    <div className="alert-banner" role="status">
      <BellIcon size={15} />
      <span>
        Base fee is at <strong>{fmtGwei(triggeredAt)} gwei</strong> — below your{' '}
        {fmtGwei(alertBelowGwei)} gwei alert. Good moment to transact.
      </span>
    </div>
  );
}
