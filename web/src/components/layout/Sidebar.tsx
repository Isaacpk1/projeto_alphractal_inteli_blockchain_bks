import { usePreferences } from '../../hooks/usePreferences';
import {
  BellIcon,
  BookIcon,
  ChatIcon,
  ChevronLeftIcon,
  EthIcon,
  GiftIcon,
  GlobeIcon,
  GraduationIcon,
  HomeIcon,
  ListIcon,
  LogoMark,
  MoonIcon,
  NetworkIcon,
  SearchIcon,
  SunIcon,
  TerminalIcon,
} from '../ui/icons';

type Icon = typeof HomeIcon;

const PRIMARY_ITEMS: ReadonlyArray<{ label: string; icon: Icon; context?: boolean }> = [
  { label: 'Home', icon: HomeIcon },
  { label: 'Cryptos', icon: EthIcon, context: true },
  { label: 'Sentiment', icon: ChatIcon },
  { label: 'Macroeconomy', icon: GlobeIcon },
  { label: 'Screeners', icon: ListIcon },
  { label: 'Research', icon: BookIcon },
  { label: 'Alerts', icon: BellIcon },
  { label: 'Alpha AI', icon: NetworkIcon },
];

const COMMUNITY_ITEMS: ReadonlyArray<{ label: string; icon: Icon }> = [
  { label: 'Invite Friends', icon: GiftIcon },
  { label: 'Academy', icon: GraduationIcon },
  { label: 'API and MCP', icon: TerminalIcon },
];

function BlockedItem({
  label,
  icon: IconComponent,
  onUnavailable,
  context = false,
}: {
  label: string;
  icon: Icon;
  onUnavailable: () => void;
  context?: boolean;
}) {
  return (
    <button
      type="button"
      className={context ? 'primary-nav__item is-context' : 'primary-nav__item'}
      onClick={onUnavailable}
    >
      <IconComponent size={18} />
      <span>{label}</span>
    </button>
  );
}

/** Navegação global da Alphractal. Neste recorte, ela é apenas contextual. */
export function Sidebar({
  collapsed,
  onToggleCollapsed,
  onUnavailable,
}: {
  collapsed: boolean;
  onToggleCollapsed: () => void;
  onUnavailable: () => void;
}) {
  const prefs = usePreferences();
  const nextTheme = prefs.theme === 'dark' ? 'light' : 'dark';

  return (
    <aside className="sidebar primary-sidebar">
      <div className="sidebar__brand">
        <LogoMark />
        <span>Alphractal</span>
      </div>

      <button type="button" className="primary-search" onClick={onUnavailable}>
        <SearchIcon size={17} />
        <span>Search</span>
        <kbd>⌘ K</kbd>
      </button>

      <nav className="primary-nav" aria-label="Navegação global">
        {PRIMARY_ITEMS.map((item) => (
          <BlockedItem key={item.label} {...item} onUnavailable={onUnavailable} />
        ))}

        <div className="primary-nav__divider" />

        {COMMUNITY_ITEMS.map((item) => (
          <BlockedItem key={item.label} {...item} onUnavailable={onUnavailable} />
        ))}
      </nav>

      <div className="primary-sidebar__footer">
        <button
          type="button"
          className="primary-nav__item"
          aria-label={`Ativar tema ${nextTheme === 'light' ? 'claro' : 'escuro'}`}
          title={`Ativar tema ${nextTheme === 'light' ? 'claro' : 'escuro'}`}
          onClick={() => prefs.update({ theme: nextTheme })}
        >
          {prefs.theme === 'dark' ? <MoonIcon size={18} /> : <SunIcon size={18} />}
          <span>Theme Mode</span>
        </button>
        <button
          type="button"
          className="primary-nav__item primary-nav__collapse"
          aria-expanded={!collapsed}
          aria-label={collapsed ? 'Expandir primeira barra lateral' : 'Colapsar primeira barra lateral'}
          title={collapsed ? 'Expandir menu' : 'Colapsar menu'}
          onClick={onToggleCollapsed}
        >
          <ChevronLeftIcon size={18} />
          <span>{collapsed ? 'Expand Menu' : 'Collapse Menu'}</span>
        </button>
      </div>
    </aside>
  );
}
