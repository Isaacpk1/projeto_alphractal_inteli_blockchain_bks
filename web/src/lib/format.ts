/**
 * Formatação de exibição — a única "matemática" permitida no front é a de
 * apresentação (casas decimais, separador, unidade relativa de tempo).
 * Os valores em si já chegam prontos da API (RN-09).
 */

const intFmt = new Intl.NumberFormat('en-US');
const usdFmt = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

export function fmtInt(n: number): string {
  return intFmt.format(Math.round(n));
}

export function fmtUsd(n: number): string {
  return usdFmt.format(n);
}

/** 18.4 · 1.52 · 0.87 — o painel nunca precisa de mais que uma casa acima de 10. */
export function fmtGwei(n: number): string {
  if (n >= 100) return n.toFixed(0);
  if (n >= 10) return n.toFixed(1);
  return n.toFixed(2);
}

export function fmtEth(n: number): string {
  if (n >= 1) return `${n.toFixed(2)} ETH`;
  if (n >= 0.001) return `${n.toFixed(4)} ETH`;
  return `${n.toFixed(6)} ETH`;
}

export function fmtEthAmount(n: number): string {
  return `${new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 }).format(n)} ETH`;
}

export function fmtPct(n: number): string {
  return `${n >= 0 ? '+' : ''}${n.toFixed(2)}%`;
}

/** "2s ago" · "1m 12s ago" — o selo do RF-25. */
export function fmtAge(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  if (s < 60) return `${s}s ago`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m ${s % 60}s ago`;
  return `${Math.floor(m / 60)}h ${m % 60}m ago`;
}

/** "~12s" · "~45s" · "~3 mins" — o selo de ETA dos cards, como no design. */
export function fmtEta(seconds: number): string {
  if (seconds < 60) return `~${Math.round(seconds)}s`;
  const mins = Math.round(seconds / 60);
  return `~${mins} min${mins > 1 ? 's' : ''}`;
}

/** "10:45" para o eixo X do gráfico. */
export function fmtAxisTime(iso: string): string {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

/** "Nov 14, 10:45 AM" para o tooltip, como no design. */
export function fmtTooltipTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

/** "$240.49K" · "$1.23M" — os valores agregados da aba Historical Fees. */
export function fmtCompactUsd(n: number): string {
  const abs = Math.abs(n);
  if (abs >= 1e9) return `$${(n / 1e9).toFixed(2)}B`;
  if (abs >= 1e6) return `$${(n / 1e6).toFixed(2)}M`;
  if (abs >= 1e3) return `$${(n / 1e3).toFixed(2)}K`;
  return usdFmt.format(n);
}

/** "3,190 ETH" · "1.24M ETH" — idem, em unidade nativa. */
export function fmtCompactEth(n: number): string {
  const abs = Math.abs(n);
  if (abs >= 1e6) return `${(n / 1e6).toFixed(2)}M ETH`;
  if (abs >= 1e3) return `${(n / 1e3).toFixed(2)}K ETH`;
  if (abs >= 1) return `${n.toFixed(2)} ETH`;
  return `${n.toFixed(6)} ETH`;
}

/** Número compacto para ticks de eixo: "0" · "580" · "1.2K" · "3.40M". */
export function fmtCompactNum(n: number): string {
  const abs = Math.abs(n);
  if (abs === 0) return '0';
  if (abs >= 1e9) return `${(n / 1e9).toFixed(2)}B`;
  if (abs >= 1e6) return `${(n / 1e6).toFixed(2)}M`;
  if (abs >= 1e3) return `${(n / 1e3).toFixed(1)}K`;
  if (abs >= 1) return n.toFixed(0);
  // Abaixo de 1 o que importa são os algarismos significativos, não as casas.
  return Number(n.toPrecision(2)).toString();
}

/** "14 Nov 2026" — eixo e tooltip das séries diárias. */
export function fmtDay(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

/** Eixo X compacto: "14 Nov" para séries diárias, "10:00" para horárias. */
export function fmtSeriesAxis(iso: string, resolution: '1h' | '1d'): string {
  const d = new Date(iso);
  return resolution === '1h'
    ? `${String(d.getHours()).padStart(2, '0')}:00`
    : d.toLocaleDateString('en-US', { day: '2-digit', month: 'short' });
}

