import type { ReactNode } from 'react';

/** RF-32 — estado de erro explícito, com ação de retry quando fizer sentido. */
export function ErrorState({
  title,
  children,
  onRetry,
}: {
  title: string;
  children?: ReactNode;
  onRetry?: () => void;
}) {
  return (
    <div className="error-state" role="alert">
      <strong>{title}</strong>
      {children && <p>{children}</p>}
      {onRetry && (
        <button type="button" className="btn" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}
