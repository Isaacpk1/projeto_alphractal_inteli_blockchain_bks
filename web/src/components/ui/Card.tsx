import type { ReactNode } from 'react';

/** Superfície padrão do painel — borda, raio e fundo vêm dos tokens do tema. */
export function Card({
  className,
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <section className={`card${className ? ` ${className}` : ''}`}>{children}</section>;
}
