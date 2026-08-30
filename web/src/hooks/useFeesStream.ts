import { useEffect } from 'react';
import { startFeesStore } from '../lib/feesStore';

/**
 * Liga o stream. Só o App chama, uma vez — este hook NÃO devolve estado.
 * Quem lê dado é useFeesSlice, componente a componente: é essa separação que
 * impede o App de re-renderizar a cada bloco (RNF-03).
 *
 * startFeesStore é idempotente, então o double-mount do StrictMode é inócuo.
 */
export function useFeesStream(): void {
  useEffect(() => {
    startFeesStore();
  }, []);
}
