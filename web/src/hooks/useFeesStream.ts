import { useEffect } from 'react';
import { startFeesStore } from '../lib/feesStore';

/** Liga, uma única vez, o store externo que mantém snapshot e SSE. */
export function useFeesStream(): void {
  useEffect(() => { startFeesStore() }, []);
}
