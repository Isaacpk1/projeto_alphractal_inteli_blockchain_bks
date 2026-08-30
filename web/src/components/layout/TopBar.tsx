import { useCallback } from 'react';
import { usePreferences } from '../../hooks/usePreferences';
import { useFeesSlice } from '../../hooks/useFeesSlice';
import type { FeesState } from '../../lib/feesStore';
import {
  BellIcon,
  EthIcon,
  HeadphonesIcon,
  MailIcon,
  MegaphoneIcon,
  MoonIcon,
  PlusIcon,
  SunIcon,
} from '../ui/icons';

/**
 * Barra superior do design: abas de rede à esquerda; tema, sino, suporte e
 * avatar à direita. Só o sino toca o store — e por um seletor booleano, então
 * a barra fica parada até o alerta (RF-30) mudar de estado.
 */
export function TopBar({
  onOpenSettings,
  onUnavailable,
}: {
  onOpenSettings: () => void;
  onUnavailable: () => void;
}) {
  const prefs = usePreferences();
  const threshold = prefs.alertBelowGwei;

  const alertSelector = useCallback(
    (s: FeesState) =>
      threshold !== null && s.snapshot !== null && s.snapshot.baseFeeGwei <= threshold,
    [threshold],
  );
  const alertActive = useFeesSlice(alertSelector);

  const nextTheme = prefs.theme === 'dark' ? 'light' : 'dark';

  return (
    <header className="topbar">
      {/* Não é seletor: o módulo é Ethereum, e só. A identificação fica estática
          para a tela dizer qual rede está olhando sem sugerir que há escolha. */}
      <div className="topbar__network-group">
        <button type="button" className="topbar__network" onClick={onUnavailable}>
          <EthIcon size={15} />
          <span>ETH</span>
        </button>
        <button type="button" className="topbar__add" aria-label="Adicionar ativo" onClick={onUnavailable}>
          <PlusIcon size={19} />
        </button>
      </div>

      <div className="topbar__actions">
        <button type="button" className="icon-btn topbar__announcement" aria-label="Novidades" onClick={onUnavailable}>
          <MegaphoneIcon />
          <span className="icon-btn__dot" aria-hidden="true" />
        </button>
        <button type="button" className="icon-btn" aria-label="Suporte" onClick={onUnavailable}>
          <HeadphonesIcon />
        </button>
        <button type="button" className="icon-btn" aria-label="Mensagens" onClick={onUnavailable}>
          <MailIcon />
        </button>
        <button
          type="button"
          className="icon-btn"
          aria-label={`Switch to ${nextTheme} mode`}
          onClick={() => prefs.update({ theme: nextTheme })}
        >
          {prefs.theme === 'dark' ? <MoonIcon /> : <SunIcon />}
        </button>
        <button
          type="button"
          className={alertActive ? 'icon-btn is-alerting' : 'icon-btn'}
          aria-label="Gas alert settings"
          onClick={onOpenSettings}
        >
          <BellIcon />
          {alertActive && <span className="icon-btn__dot" aria-hidden="true" />}
        </button>
        <button type="button" className="topbar__avatar" aria-label="Conta" onClick={onUnavailable}>
          V
        </button>
      </div>
    </header>
  );
}
