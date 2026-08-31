const base = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

export const endpoints = {
  health: `${base}/api/v1/health`,
  status: `${base}/api/v1/status`,
  stream: `${base}/api/v1/fees/stream`,
  priceStream: `${base}/api/v1/fees/price-stream`,
  snapshot: `${base}/api/v1/fees/snapshot`,
  coldLatest: `${base}/api/v1/fees/latest`,
  coldMempool: `${base}/api/v1/fees/mempool`,
  coldEstimates: `${base}/api/v1/fees/estimates`,
  history: `${base}/api/v1/fees/history`,
  estimatesHistory: `${base}/api/v1/fees/estimates/history`,
  ethUsd24h: `${base}/api/v1/fees/eth-usd`,
  burn: `${base}/api/v1/fees/queima`,
} as const;

export class ApiError extends Error {
  constructor(readonly status: number, readonly detail: string) {
    super(detail);
    this.name = 'ApiError';
  }
  get isColdPathDown(): boolean { return this.status === 503 }
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const detail = problem && typeof problem === 'object' && 'detail' in problem
      ? String((problem as { detail: unknown }).detail)
      : response.statusText;
    throw new ApiError(response.status, detail);
  }
  return (await response.json()) as T;
}

export function withQuery(url: string, params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) search.set(key, String(value));
  }
  const query = search.toString();
  return query ? `${url}?${query}` : url;
}

/** API real por padrão. O mock é opt-in para demos offline. */
export const useMock = import.meta.env.VITE_USE_MOCK === 'true';
