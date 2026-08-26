import { useEffect, useRef, useState } from 'react';
import { endpoints } from '../lib/api';
import type { FeesSnapshot, StreamStatus } from '../types/contract';

/**
 * Assina o SSE da API. É este hook que faz o painel ser "ao vivo" — e é por causa
 * dele que a View não pode ser Razor: o servidor não empurra HTML atualizado.
 *
 * EventSource já reconecta sozinho. O que ele NÃO faz é avisar que o dado ficou
 * velho: quem mostra "dado atrasado há 40 s" é o campo dataAgeSeconds.
 */
export function useFeesStream() {
  const [snapshot, setSnapshot] = useState<FeesSnapshot | null>(null);
  const [status, setStatus] = useState<StreamStatus>('conectando');
  const sourceRef = useRef<EventSource | null>(null);

  useEffect(() => {
    const source = new EventSource(endpoints.stream);
    sourceRef.current = source;

    source.onopen = () => setStatus('ao-vivo');
    source.onmessage = (event) => {
      setSnapshot(JSON.parse(event.data) as FeesSnapshot);
      setStatus('ao-vivo');
    };
    source.onerror = () => {
      setStatus(source.readyState === EventSource.CLOSED ? 'erro' : 'reconectando');
    };

    return () => source.close();
  }, []);

  return { snapshot, status };
}
