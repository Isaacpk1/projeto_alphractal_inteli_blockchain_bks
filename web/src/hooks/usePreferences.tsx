import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

/**
 * Estado de PREFERÊNCIA — muda quando o usuário clica, não quando chega bloco.
 * Por isso pode viver em Context sem ferir o RNF-03: re-renderizar os
 * consumidores numa ação de clique é irrelevante. O estado de STREAM (12 s)
 * nunca entra aqui — mora no feesStore.
 *
 * Persistência em localStorage com try/catch: aba anônima ou storage bloqueado
 * não podem quebrar a tela.
 */

export type Theme = 'dark' | 'light';
/** RF-28 — unidade exibida nos cards. */
export type Unit = 'gwei' | 'usd' | 'eth';

export interface Preferences {
  theme: Theme;
  unit: Unit;
  /** RF-27 — tipo de transação selecionado no estimador. */
  txTypeId: string;
  /** RF-30 — alerta quando a base fee ficar ABAIXO deste valor. null = sem alerta. */
  alertBelowGwei: number | null;
}

interface PreferencesApi extends Preferences {
  update: (patch: Partial<Preferences>) => void;
}

const STORAGE_KEY = 'af-prefs';

const DEFAULTS: Preferences = {
  theme: 'dark', // RF-31 — a identidade da Alphractal é o tema escuro
  unit: 'gwei',
  txTypeId: 'eth-transfer',
  alertBelowGwei: null,
};

function load(): Preferences {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? { ...DEFAULTS, ...(JSON.parse(raw) as Partial<Preferences>) } : DEFAULTS;
  } catch {
    return DEFAULTS;
  }
}

const PreferencesContext = createContext<PreferencesApi | null>(null);

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [prefs, setPrefs] = useState<Preferences>(load);

  useEffect(() => {
    document.documentElement.dataset['theme'] = prefs.theme;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    } catch {
      // storage indisponível — preferências valem só para a sessão
    }
  }, [prefs]);

  const api = useMemo<PreferencesApi>(
    () => ({ ...prefs, update: (patch) => setPrefs((p) => ({ ...p, ...patch })) }),
    [prefs],
  );

  return (
    <PreferencesContext.Provider value={api}>{children}</PreferencesContext.Provider>
  );
}

// Provider e hook juntos de propósito: são uma unidade. O custo é o fast
// refresh recarregar o arquivo inteiro ao editá-lo — aceitável.
// eslint-disable-next-line react-refresh/only-export-components
export function usePreferences(): PreferencesApi {
  const ctx = useContext(PreferencesContext);
  if (!ctx) throw new Error('usePreferences fora de <PreferencesProvider>');
  return ctx;
}
