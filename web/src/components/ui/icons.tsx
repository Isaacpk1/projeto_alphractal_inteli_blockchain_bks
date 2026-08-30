import type { SVGProps } from 'react';

/**
 * Ícones inline (stroke 1.7, herdam currentColor) — sem biblioteca externa,
 * então o tema claro/escuro os pinta de graça via CSS.
 */

type IconProps = SVGProps<SVGSVGElement> & { size?: number };

function Base({ size = 18, children, ...rest }: IconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.7}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...rest}
    >
      {children}
    </svg>
  );
}

export const BoltIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M13 2 4.5 13.5H11L10 22l8.5-11.5H13L13 2Z" />
  </Base>
);

export const HistoryIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M3 12a9 9 0 1 0 2.6-6.3L3 8" />
    <path d="M3 3v5h5" />
    <path d="M12 7v5l3.5 2" />
  </Base>
);

export const GearIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="12" cy="12" r="3.2" />
    <path d="M19.4 15a1.7 1.7 0 0 0 .34 1.87l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.7 1.7 0 0 0-1.87-.34 1.7 1.7 0 0 0-1.03 1.56V21a2 2 0 1 1-4 0v-.09A1.7 1.7 0 0 0 8.9 19.4a1.7 1.7 0 0 0-1.87.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.7 1.7 0 0 0 .34-1.87 1.7 1.7 0 0 0-1.55-1.03H3a2 2 0 1 1 0-4h.09A1.7 1.7 0 0 0 4.6 8.9a1.7 1.7 0 0 0-.34-1.87l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.7 1.7 0 0 0 1.87.34H9a1.7 1.7 0 0 0 1.03-1.55V3a2 2 0 1 1 4 0v.09c0 .68.4 1.3 1.03 1.56a1.7 1.7 0 0 0 1.87-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.7 1.7 0 0 0-.34 1.87v.09c.26.62.88 1.03 1.56 1.03H21a2 2 0 1 1 0 4h-.09c-.68 0-1.3.4-1.51 1.03Z" />
  </Base>
);

export const BellIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
    <path d="M13.7 21a2 2 0 0 1-3.4 0" />
  </Base>
);

export const SunIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
  </Base>
);

export const MoonIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z" />
  </Base>
);

export const EthIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 2v20M12 2l6.5 10.5L12 16 5.5 12.5 12 2ZM5.5 12.5 12 22l6.5-9.5" />
  </Base>
);

export const BikeIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="5.5" cy="17" r="3.2" />
    <circle cx="18.5" cy="17" r="3.2" />
    <path d="M9 17 12 10h4M12 10l3.5 7M12 10H8.5M14 7h2.5l2 10M8.5 10 5.5 17" />
  </Base>
);

export const TramIcon = (p: IconProps) => (
  <Base {...p}>
    <rect x="5" y="4" width="14" height="13" rx="2.5" />
    <path d="M5 11h14M9 4V2.5M15 4V2.5M8.5 20l-1.5 2M15.5 20l1.5 2M9 14.5h.01M15 14.5h.01" />
  </Base>
);

export const RocketIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 15c-1.5-1.5-2-4.5-.5-7C13.5 4.5 17 3 21 3c0 4-1.5 7.5-5 9.5-2.5 1.5-5.5 1-7 .5Z" />
    <path d="M9 12H5.5L8 8.5M12 15v3.5L15.5 16M6 18c-1.5 1.5-2 4-2 4s2.5-.5 4-2" />
    <circle cx="15.5" cy="8.5" r="1" fill="currentColor" stroke="none" />
  </Base>
);

export const ArrowUpIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 19V5M5.5 11.5 12 5l6.5 6.5" />
  </Base>
);

export const ArrowDownIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 5v14M5.5 12.5 12 19l6.5-6.5" />
  </Base>
);

export const FlameIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 22a7 7 0 0 0 7-7c0-4-3-6.5-4.5-9.5C13 8 10.5 8.5 10.5 5.5c0 0-5.5 3.5-5.5 9.5a7 7 0 0 0 7 7Z" />
    <path d="M12 22a3.5 3.5 0 0 0 3.5-3.5c0-2-1.5-3-2.5-5-1.5 1.5-4.5 2.5-4.5 5A3.5 3.5 0 0 0 12 22Z" />
  </Base>
);

/** Somatório — as métricas agregadas (Total Fees). */
export const SumIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M18 5H6.5l5.5 7-5.5 7H18" />
  </Base>
);

/** Média — a série e a linha do valor médio atravessando (Mean Tx Fee). */
export const MeanIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M3.5 17c2.5-8 5-1 7.5-6s5 5 9.5-2" />
    <path d="M3 12h18" strokeDasharray="3 3" />
  </Base>
);

/** Preço por unidade — mostrador (Mean Fee per Gas). */
export const GaugeIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M4 17a8 8 0 1 1 16 0" />
    <path d="M12 17l4-4.5" />
    <circle cx="12" cy="17" r="1.2" fill="currentColor" stroke="none" />
  </Base>
);

export const DownloadIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 3v12M7.5 10.5 12 15l4.5-4.5M4 19h16" />
  </Base>
);

export const CloseIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M6 6l12 12M18 6 6 18" />
  </Base>
);

export const SearchIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="11" cy="11" r="6.5" />
    <path d="m16 16 4.5 4.5" />
  </Base>
);

export const PlusIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M12 5v14M5 12h14" />
  </Base>
);

export const HomeIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="m3.5 11 8.5-7 8.5 7" />
    <path d="M5.5 9.5V21h13V9.5M9.5 21v-6h5v6" />
  </Base>
);

export const GlobeIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="12" cy="12" r="9" />
    <path d="M3 12h18M12 3c2.4 2.5 3.5 5.5 3.5 9S14.4 18.5 12 21M12 3C9.6 5.5 8.5 8.5 8.5 12S9.6 18.5 12 21" />
  </Base>
);

export const ListIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M9 6h11M9 12h11M9 18h11" />
    <path d="M4 6h.01M4 12h.01M4 18h.01" strokeWidth={2.8} />
  </Base>
);

export const BookIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M4 4.5h5.5A2.5 2.5 0 0 1 12 7v13a3 3 0 0 0-3-3H4V4.5ZM20 4.5h-5.5A2.5 2.5 0 0 0 12 7v13a3 3 0 0 1 3-3h5V4.5Z" />
  </Base>
);

export const ChatIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M4 5.5h16v11H9l-5 4v-15Z" />
    <path d="M8 10h8M8 13h5" />
  </Base>
);

export const NetworkIcon = (p: IconProps) => (
  <Base {...p}>
    <circle cx="12" cy="5" r="2" />
    <circle cx="5" cy="19" r="2" />
    <circle cx="19" cy="19" r="2" />
    <path d="M12 7v5M5 17v-3h14v3" />
  </Base>
);

export const GiftIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M3.5 9h17v12h-17zM2.5 5.5h19V9h-19zM12 5.5V21" />
    <path d="M12 5.5C10 5.5 7 5 7 3.2 7 1.6 10.5 2 12 5.5ZM12 5.5c2 0 5-.5 5-2.3 0-1.6-3.5-1.2-5 2.3Z" />
  </Base>
);

export const GraduationIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="m3 9 9-5 9 5-9 5-9-5Z" />
    <path d="M7 12v5c2.7 2 7.3 2 10 0v-5M21 9v6" />
  </Base>
);

export const TerminalIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="m5 7 4 5-4 5M11 18h8" />
  </Base>
);

export const MailIcon = (p: IconProps) => (
  <Base {...p}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="m4 7 8 6 8-6" />
  </Base>
);

export const HeadphonesIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M4 14v-2a8 8 0 0 1 16 0v2M4 14h3v6H5a1 1 0 0 1-1-1v-5ZM20 14h-3v6h2a1 1 0 0 0 1-1v-5Z" />
  </Base>
);

export const MegaphoneIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="M4 11v3h3l9 4V7l-9 4H4ZM16 10c2 1 2 4 0 5M8 15l1.5 5h3L11 16" />
  </Base>
);

export const ChevronDownIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="m6 9 6 6 6-6" />
  </Base>
);

export const ChevronLeftIcon = (p: IconProps) => (
  <Base {...p}>
    <path d="m15 5-7 7 7 7" />
  </Base>
);

/** Marca geométrica compacta para o recorte visual da plataforma. */
export const LogoMark = ({ size = 34 }: { size?: number }) => (
  <svg width={size} height={size} viewBox="0 0 36 36" aria-hidden="true">
    <path d="M5 30 15.2 6h5.6L31 30h-6.2l-2.2-5.8h-9.4L11 30H5Z" fill="currentColor" opacity=".92" />
    <path d="m10.5 20 18-9-3 7-15 7v-5Z" fill="var(--bg-sidebar)" opacity=".9" />
  </svg>
);
