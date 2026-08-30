import { useFeesSlice } from '../../hooks/useFeesSlice';
import { useTicker } from '../../hooks/useTicker';
import { fmtAge } from '../../lib/format';

/**
 * O "2s ago" do RF-25. Este é o ÚNICO componente que re-renderiza a cada
 * segundo — o timer mora aqui, na folha, de propósito: se ticasse no store,
 * todos os assinantes acordariam junto e o RNF-03 iria embora.
 */
export function BlockAge() {
  const lastBlockAtMs = useFeesSlice((s) => s.lastBlockAtMs);
  const now = useTicker(1_000);

  if (lastBlockAtMs === null) return null;
  return <span className="connection__age">{fmtAge((now - lastBlockAtMs) / 1000)}</span>;
}
