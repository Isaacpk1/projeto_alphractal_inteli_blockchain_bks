import { useEffect, useRef, useState } from 'react';
import { endpoints } from '../lib/api';
import type { FeesSnapshot, StreamStatus } from '../types/contract';

/**
 * Assina o SSE da API. É este hook que faz o painel ser "ao vivo" — e é por causa
 * dele que a View não pode ser Razor: o servidor não empurra HTML atualizado.
 *
 * EventSource já reconecta sozinho. O que ele NÃO faz é avisar que o dado ficou
 * velho: um socket aberto sem bloco novo continua "conectado". Por isso existe o
 * relógio local abaixo — sem ele, o painel exibiria um número de 10 minutos atrás
 * como se fosse atual, que é exatamente o problema que o projeto veio resolver.
 */
export function useFeesStream() {
  const [snapshot, setSnapshot] = useState<FeesSnapshot | null>(null);
  const [status, setStatus] = useState<StreamStatus>('conectando');
  const [ageSeconds, setAgeSeconds] = useState(0);
  const receivedAt = useRef<number | null>(null);

  useEffect(() => {
    const source = new EventSource(endpoints.stream);

    source.onopen = () => setStatus('ao-vivo');
    source.onmessage = (event) => {
      setSnapshot(JSON.parse(event.data) as FeesSnapshot);
      receivedAt.current = Date.now();
      setStatus('ao-vivo');
    };
    source.onerror = () => {
      setStatus(source.readyState === EventSource.CLOSED ? 'erro' : 'reconectando');
    };

    return () => source.close();
  }, []);

  // Idade calculada no cliente: o dataAgeSeconds do payload congela no instante
  // em que o bloco chegou e envelheceria junto com a tela sem ninguém perceber.
  useEffect(() => {
    const timer = window.setInterval(() => {
      if (snapshot === null) return;
      const base = snapshot.dataAgeSeconds;
      const since = receivedAt.current === null ? 0 : (Date.now() - receivedAt.current) / 1000;
      setAgeSeconds(base + since);
    }, 1000);

    return () => window.clearInterval(timer);
  }, [snapshot]);

  /** RN-07: 60 s ≈ 5 blocos sem novidade. */
  const isStale = ageSeconds > 60;

  return {
    snapshot,
    status,
    ageSeconds,
    isStale,
    /** O que o badge do painel deve mostrar. */
    liveLabel: status === 'ao-vivo' && !isStale ? 'Ao vivo' : 'Dados desatualizados',
  };
}
