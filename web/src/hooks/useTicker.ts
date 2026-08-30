import { useEffect, useState } from 'react';

/**
 * Re-renderiza o componente a cada `intervalMs`. Uso: relógios ("2s ago").
 *
 * O tique fica no componente FOLHA que exibe o relógio — nunca no store nem
 * numa view. Se este timer morasse no store, todo assinante acordaria a cada
 * segundo, e o RNF-03 morreria 12 vezes por bloco.
 */
export function useTicker(intervalMs = 1_000): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), intervalMs);
    return () => clearInterval(id);
  }, [intervalMs]);
  return now;
}
