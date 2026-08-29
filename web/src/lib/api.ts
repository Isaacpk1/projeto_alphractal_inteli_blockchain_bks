const base = import.meta.env.VITE_API_BASE_URL ?? '';

export const endpoints = {
  health: `${base}/api/v1/health`,
  /** Saúde da ingestão, lida do caminho frio (v_ingestion_status). */
  status: `${base}/api/v1/status`,

  // ── Caminho quente — servido da memória da API (RN-14). Ainda não implementado.
  stream: `${base}/api/v1/fees/stream`,
  snapshot: `${base}/api/v1/fees/snapshot`,

  // ── Caminho frio — ClickHouse via views v_*.
  /** Último bloco segundo o banco. Diagnóstico/fallback, NÃO é o dado ao vivo. */
  coldLatest: `${base}/api/v1/fees/latest`,
  coldMempool: `${base}/api/v1/fees/mempool`,
  coldEstimates: `${base}/api/v1/fees/estimates`,
  /** `?granularity=hour|day&from=&to=&limit=` */
  history: `${base}/api/v1/fees/history`,
  /** `?from=&to=&limit=` — custo diário por operação e velocidade (D-04). */
  estimatesHistory: `${base}/api/v1/fees/estimates/history`,
} as const;

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly detail: string,
  ) {
    super(detail);
    this.name = 'ApiError';
  }

  /** 503 = caminho frio fora do ar. O painel ao vivo não depende dele. */
  get isColdPathDown(): boolean {
    return this.status === 503;
  }
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal, headers: { Accept: 'application/json' } });

  if (!response.ok) {
    // A API responde ProblemDetails (RFC 7807) em todo erro.
    const problem = await response.json().catch(() => null);
    const detail =
      problem && typeof problem === 'object' && 'detail' in problem
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
