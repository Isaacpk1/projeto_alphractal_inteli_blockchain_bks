import { useEffect, useId, useRef } from 'react';
import { CloseIcon } from './icons';

interface Props {
  open: boolean;
  onClose: () => void;
}

export function UnavailableModal({ open, onClose }: Props) {
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKeyDown);
    dialogRef.current?.querySelector<HTMLButtonElement>('.btn')?.focus();
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      className="modal-overlay"
      onMouseDown={(event) => event.target === event.currentTarget && onClose()}
    >
      <div
        className="modal unavailable-modal"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        ref={dialogRef}
      >
        <header className="modal__head">
          <div>
            <span className="unavailable-modal__eyebrow">Recorte de Fees</span>
            <h2 id={titleId}>Menu indisponível</h2>
          </div>
          <button type="button" className="icon-btn" aria-label="Fechar" onClick={onClose}>
            <CloseIcon />
          </button>
        </header>
        <p>Não é possível entrar nesse menu</p>
        <button type="button" className="btn unavailable-modal__action" onClick={onClose}>
          Entendi
        </button>
      </div>
    </div>
  );
}
