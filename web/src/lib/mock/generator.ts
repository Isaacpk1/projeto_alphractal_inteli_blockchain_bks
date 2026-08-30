import type {
  CongestionLevel,
  FeesInsights,
  FeesSnapshot,
  HistoryPoint,
  TierId,
  TrendDirection,
  TxTypeEstimate,
} from '../../types/contract';

/**
 * Simulação da Mainnet para o transporte mockado. Este arquivo faz o papel da
 * API .NET — por isso (e SÓ por isso) existe matemática de taxa aqui: é a
 * matemática que o Service do back fará (RN-01 a RN-05), não lógica do front.
 * Quando a API existir, este arquivo inteiro deixa de ser usado.
 *
 * A dinâmica segue o EIP-1559 de verdade: a base fee varia até ±12,5% por bloco
 * conforme gasUsed/gasLimit, então o gráfico mockado se comporta como o real.
 */

const BLOCK_SECONDS = 12;
const GAS_LIMIT = 30_000_000;
const WINDOW = 300; // RN-10 — janela quente
const CONG_WINDOW = 100; // RN-04 — média móvel do congestionamento

/** RF-11 — gas limits de referência, os mesmos que a API terá configurados. */
const TX_TYPES = [
  { id: 'eth-transfer', label: 'ETH transfer', gasLimit: 21_000 },
  { id: 'erc20-transfer', label: 'ERC-20 transfer', gasLimit: 65_000 },
  { id: 'dex-swap', label: 'DEX swap', gasLimit: 200_000 },
  { id: 'approval', label: 'Token approval', gasLimit: 46_000 },
  { id: 'nft-mint', label: 'NFT mint', gasLimit: 150_000 },
] as const;

/** Regimes de tráfego — a rede alterna entre eles para o painel ter vida. */
interface Regime {
  target: number;
  tip: number;
  weight: number;
}

const NORMAL: Regime = { target: 0.5, tip: 1.0, weight: 5 };
const REGIMES: readonly Regime[] = [
  { target: 0.38, tip: 0.7, weight: 3 }, // madrugada
  NORMAL,
  { target: 0.68, tip: 1.8, weight: 3 }, // movimentado
  { target: 0.93, tip: 4.5, weight: 1 }, // pico (mint, listagem…)
];

interface Block {
  number: number;
  timestampMs: number;
  baseFeeGwei: number;
  gasUsedRatio: number;
  priorityP25: number;
  priorityP50: number;
  priorityP75: number;
}

function gauss(): number {
  // Box-Muller — ruído com cara de rede, não de dado de RPG.
  const u = Math.random() || 1e-9;
  const v = Math.random() || 1e-9;
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}

const clamp = (n: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, n));

export class MockChain {
  private blocks: Block[] = [];
  private regime: Regime = NORMAL;
  private regimeLeft = 40;
  private ethUsd = 3_412.8;
  private change24h = 1.24;

  constructor() {
    let base = 14 + Math.random() * 10;
    let number = 21_984_332 - WINDOW;
    let ts = Date.now() - WINDOW * BLOCK_SECONDS * 1000;
    for (let i = 0; i < WINDOW; i++) {
      const block = this.makeBlock(number++, ts, base);
      this.blocks.push(block);
      base = this.nextBaseFee(block);
      ts += BLOCK_SECONDS * 1000;
    }
  }

  private makeBlock(number: number, timestampMs: number, baseFeeGwei: number): Block {
    if (--this.regimeLeft <= 0) {
      const pool = REGIMES.flatMap((r) => Array<Regime>(r.weight).fill(r));
      this.regime = pool[Math.floor(Math.random() * pool.length)] ?? NORMAL;
      this.regimeLeft = 20 + Math.floor(Math.random() * 60);
    }
    // Elasticidade de demanda: taxa cara desincentiva uso, então o alvo de
    // ocupação cai conforme a base fee sobe — sem isto, um regime de pico
    // composto a +12,5%/bloco dispara a taxa até o teto e nunca volta.
    const elasticity = clamp(1 - 0.38 * Math.log10(baseFeeGwei / 18), 0.25, 1.15);
    const ratio = clamp(this.regime.target * elasticity + gauss() * 0.13, 0.02, 1);
    const tip = this.regime.tip * (0.8 + Math.random() * 0.4);
    return {
      number,
      timestampMs,
      baseFeeGwei: baseFeeGwei,
      gasUsedRatio: ratio,
      priorityP25: tip * 0.5,
      priorityP50: tip,
      priorityP75: tip * 2.2,
    };
  }

  /** EIP-1559: base' = base × (1 + 0,125 × (ratio − 0,5) / 0,5). */
  private nextBaseFee(block: Block): number {
    const delta = 0.125 * ((block.gasUsedRatio - 0.5) / 0.5);
    return clamp(block.baseFeeGwei * (1 + delta), 0.05, 500);
  }

  private get head(): Block {
    const head = this.blocks.at(-1);
    if (!head) throw new Error('mock sem blocos — construtor não rodou?');
    return head;
  }

  /** Avança um bloco (~12 s de rede). */
  tick(): void {
    const head = this.head;
    const block = this.makeBlock(
      head.number + 1,
      Date.now(),
      this.nextBaseFee(head),
    );
    this.blocks.push(block);
    if (this.blocks.length > WINDOW) this.blocks.shift();

    this.ethUsd = clamp(this.ethUsd * (1 + gauss() * 0.0006), 2_500, 4_800);
    this.change24h = clamp(this.change24h + gauss() * 0.03, -9, 9);
  }

  snapshot(): FeesSnapshot {
    const head = this.head;
    const prev = this.blocks.at(-2);

    const congRatio =
      head.baseFeeGwei /
      Math.max(
        0.05,
        this.blocks.slice(-CONG_WINDOW).reduce((s, b) => s + b.baseFeeGwei, 0) /
          Math.min(this.blocks.length, CONG_WINDOW),
      );
    const level: CongestionLevel =
      congRatio < 0.9
        ? 'baixo'
        : congRatio < 1.15
          ? 'normal'
          : congRatio < 1.5
            ? 'alto'
            : 'extremo';

    const deltaPct = prev
      ? (head.baseFeeGwei - prev.baseFeeGwei) / prev.baseFeeGwei
      : 0;
    const trend: TrendDirection =
      Math.abs(deltaPct) < 0.005 ? 'estavel' : deltaPct > 0 ? 'subindo' : 'caindo';

    const tier = (priority: number, etaSeconds: number) => {
      const maxFeeGwei = head.baseFeeGwei + priority;
      const estEth = maxFeeGwei * 21_000 * 1e-9;
      return {
        maxFeeGwei,
        priorityFeeGwei: priority,
        estEth,
        estUsd: estEth * this.ethUsd,
        etaSeconds,
      };
    };
    const tiers = {
      slow: tier(head.priorityP25, 180),
      standard: tier(head.priorityP50, 45),
      fast: tier(head.priorityP75, 12),
    };

    const txEstimates: TxTypeEstimate[] = TX_TYPES.map((t) => ({
      id: t.id,
      label: t.label,
      gasLimit: t.gasLimit,
      tiers: Object.fromEntries(
        (Object.keys(tiers) as TierId[]).map((id) => {
          const eth = tiers[id].maxFeeGwei * t.gasLimit * 1e-9;
          return [id, { eth, usd: eth * this.ethUsd }];
        }),
      ) as TxTypeEstimate['tiers'],
    }));

    return {
      blockNumber: head.number,
      blockHash: `mock-${head.number}`,
      blockTimestampUtc: new Date(head.timestampMs).toISOString(),
      baseFeeGwei: head.baseFeeGwei,
      nextBaseFeeGwei: this.nextBaseFee(head),
      gasUsed: Math.round(head.gasUsedRatio * GAS_LIMIT),
      gasLimit: GAS_LIMIT,
      gasUsedRatio: head.gasUsedRatio,
      trend,
      congestion: { level, ratio: congRatio },
      tiers,
      ethUsd: { price: this.ethUsd, change24hPct: this.change24h },
      txEstimates,
      dataAgeSeconds: Math.max(0, (Date.now() - head.timestampMs) / 1000),
      isStale: false,
      deliveryLatencySeconds: 0,
      windowSize: this.blocks.length,
      source: 'live',
    };
  }

  /** A janela quente — alimenta a visão "LIVE" do gráfico (RF-24). */
  liveHistory(): HistoryPoint[] {
    return this.blocks.map((b) => ({
      blockNumber: b.number,
      blockTimestampUtc: new Date(b.timestampMs).toISOString(),
      baseFeeGwei: b.baseFeeGwei,
      priorityFeeP50Gwei: b.priorityP50,
      gasUsedRatio: b.gasUsedRatio,
    }));
  }

  /**
   * Janelas maiores (RF-38 — caminho frio). A API real lerá agregados do
   * ClickHouse; aqui sintetizamos uma série ancorada no valor atual, com ciclo
   * diário, para o gráfico histórico ter forma plausível.
   */
  aggregatedHistory(spanSeconds: number, points = 180): HistoryPoint[] {
    const head = this.head;
    const stepSec = spanSeconds / points;
    const out: HistoryPoint[] = [];
    let value = head.baseFeeGwei;
    for (let i = 0; i < points; i++) {
      const ts = head.timestampMs - (points - 1 - i) * stepSec * 1000;
      const hour = new Date(ts).getUTCHours();
      const daily = 1 + 0.22 * Math.sin(((hour - 14) / 24) * 2 * Math.PI);
      const noisy = clamp(value * daily * (1 + gauss() * 0.06), 0.5, 300);
      out.push({
        blockNumber: head.number - Math.round(((points - 1 - i) * stepSec) / BLOCK_SECONDS),
        blockTimestampUtc: new Date(ts).toISOString(),
        baseFeeGwei: noisy,
        priorityFeeP50Gwei: clamp(noisy * 0.06 * (1 + Math.random()), 0.1, 30),
        gasUsedRatio: clamp(0.5 + gauss() * 0.15, 0.05, 1),
      });
      value = clamp(value * (1 + gauss() * 0.015), 1, 300);
    }
    // Âncora: o último ponto é o presente, para o gráfico "chegar" no valor atual.
    const last = out.at(-1);
    if (last) {
      last.baseFeeGwei = head.baseFeeGwei;
      last.priorityFeeP50Gwei = head.priorityP50;
    }
    return out;
  }

  /** Queima da base fee (EIP-1559) — BRD RF-06. */
  insights(): FeesInsights {
    const head = this.head;
    const burnPerBlock = head.baseFeeGwei * head.gasUsedRatio * GAS_LIMIT * 1e-9;
    return {
      burnRateEthPerMin: burnPerBlock * (60 / BLOCK_SECONDS),
      burned24hEth: burnPerBlock * (86_400 / BLOCK_SECONDS),
    };
  }
}
