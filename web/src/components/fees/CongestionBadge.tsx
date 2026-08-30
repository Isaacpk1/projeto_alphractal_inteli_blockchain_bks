import { useFeesSlice } from '../../hooks/useFeesSlice';
import type { CongestionLevel } from '../../types/contract';

/** RF-23 — os quatro níveis, calculados na API (RN-04). O front só pinta. */
const LEVELS: Record<CongestionLevel, { label: string; className: string }> = {
  baixo: { label: 'Low', className: 'congestion--low' },
  normal: { label: 'Normal', className: 'congestion--normal' },
  alto: { label: 'High', className: 'congestion--high' },
  extremo: { label: 'Extreme', className: 'congestion--extreme' },
};

export function CongestionBadge() {
  const level = useFeesSlice((s) => s.snapshot?.congestion.level ?? null);
  if (!level) return null;

  const { label, className } = LEVELS[level];
  return (
    <span className={`congestion ${className}`} title="Base fee vs. 100-block moving average">
      Network congestion: <strong>{label}</strong>
    </span>
  );
}
