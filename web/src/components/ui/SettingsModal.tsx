import { useEffect, useId, useRef } from 'react';
import { usePreferences } from '../../hooks/usePreferences';
import { SegmentedControl } from './SegmentedControl';
import { CloseIcon } from './icons';

/**
 * Preferências do painel — tema (RF-31), unidade (RF-28) e o limiar de alerta
 * de gas (RF-30). Abre pelo Settings da sidebar e pelo sino da topbar.
 */
export function SettingsModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const prefs = usePreferences();
  const titleId = useId();
  const alertId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    dialogRef.current?.querySelector('button')?.focus();
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="modal-overlay" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className="modal" role="dialog" aria-modal="true" aria-labelledby={titleId} ref={dialogRef}>
        <header className="modal__head">
          <h2 id={titleId}>Settings</h2>
          <button type="button" className="icon-btn" aria-label="Close settings" onClick={onClose}>
            <CloseIcon />
          </button>
        </header>

        <div className="modal__row">
          <span className="modal__label">Theme</span>
          <SegmentedControl
            ariaLabel="Theme"
            options={[
              { value: 'dark', label: 'Dark' },
              { value: 'light', label: 'Light' },
            ]}
            value={prefs.theme}
            onChange={(theme) => prefs.update({ theme })}
          />
        </div>

        <div className="modal__row">
          <span className="modal__label">Display unit</span>
          <SegmentedControl
            ariaLabel="Display unit"
            options={[
              { value: 'gwei', label: 'gwei' },
              { value: 'eth', label: 'ETH' },
              { value: 'usd', label: 'USD' },
            ]}
            value={prefs.unit}
            onChange={(unit) => prefs.update({ unit })}
          />
        </div>

        <div className="modal__row">
          <label className="modal__label" htmlFor={alertId}>
            Gas alert — notify when base fee drops below
          </label>
          <div className="modal__alert">
            <input
              id={alertId}
              type="number"
              min={0}
              step={0.5}
              placeholder="e.g. 10"
              value={prefs.alertBelowGwei ?? ''}
              onChange={(e) =>
                prefs.update({
                  alertBelowGwei: e.target.value === '' ? null : Number(e.target.value),
                })
              }
            />
            <span>gwei</span>
            {prefs.alertBelowGwei !== null && (
              <button
                type="button"
                className="btn btn--ghost"
                onClick={() => prefs.update({ alertBelowGwei: null })}
              >
                Clear
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
