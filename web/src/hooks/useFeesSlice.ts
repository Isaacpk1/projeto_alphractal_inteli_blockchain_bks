import { useSyncExternalStore } from 'react';
import { feesStore } from '../lib/feesStore';
import type { FeesState } from '../lib/feesStore';

/**
 * Assina uma FATIA do feesStore. É o mecanismo que cumpre o RNF-03: o React só
 * re-renderiza o componente se o VALOR retornado pelo seletor mudar.
 *
 * ► Armadilha clássica: o seletor roda a cada notificação, então ele precisa
 *   devolver referência estável — um primitivo (`s => s.status`) ou um objeto
 *   que o store só recria quando muda (`s => s.snapshot`, `s => s.liveHistory`).
 *   NUNCA montar objeto novo no seletor (`s => ({a, b})`): isso re-renderiza
 *   sempre e, no limite, entra em loop.
 */
export function useFeesSlice<T>(selector: (state: FeesState) => T): T {
  return useSyncExternalStore(feesStore.subscribe, () =>
    selector(feesStore.getState()),
  );
}
