/**
 * Espelho em TypeScript de api/src/Alphractal.Fees.Api/Models/Responses/.
 *
 * Este arquivo é a metade do contrato que vive no front. Não existe compilador
 * que verifique se ele bate com o C# — se um campo mudar lá e não mudar aqui,
 * a tela quebra em runtime, sem aviso. Mudou o DTO na API? Mude aqui no MESMO PR.
 *
 * Unidades: a API já entrega convertido. Nunca chega wei aqui.
 */

export interface FeesSnapshot {
  blockNumber: number;
  blockTimestampUtc: string;
  baseFeeGwei: number;
  gasUsedRatio: number;
  /** Idade do dado em segundos — alimenta o aviso de "dado atrasado". */
  dataAgeSeconds: number;
}

export type StreamStatus = 'conectando' | 'ao-vivo' | 'reconectando' | 'erro';
