const base = import.meta.env.VITE_API_BASE_URL ?? '';

export const endpoints = {
  health: `${base}/api/v1/health`,
  stream: `${base}/api/v1/fees/stream`,
  snapshot: `${base}/api/v1/fees/snapshot`,
  history: `${base}/api/v1/fees/history`,
  /** D-06 (backlog) — ainda não existe na API. Hoje só o mock responde. */
  insights: `${base}/api/v1/fees/insights`,
  /** Métricas agregadas da aba Historical Fees (caminho frio). */
  metrics: `${base}/api/v1/fees/metrics`,
} as const;

/**
 * A API ainda não existe — o padrão é o transporte mockado (lib/mock/).
 * Quando a API .NET subir, defina VITE_USE_MOCK=false no .env para o front
 * passar a falar com os endpoints acima. Nada mais muda.
 */
export const useMock = import.meta.env.VITE_USE_MOCK !== 'false';
