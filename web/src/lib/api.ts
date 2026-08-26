const base = import.meta.env.VITE_API_BASE_URL ?? '';

export const endpoints = {
  health: `${base}/api/v1/health`,
  stream: `${base}/api/v1/fees/stream`,
  snapshot: `${base}/api/v1/fees/snapshot`,
  history: `${base}/api/v1/fees/history`,
} as const;
