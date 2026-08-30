import { useEffect, useState } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { MetricsSidebar } from './components/layout/MetricsSidebar';
import { Sidebar } from './components/layout/Sidebar';
import { TopBar } from './components/layout/TopBar';
import { MetricsHeader } from './components/live/MetricsHeader';
import { SettingsModal } from './components/ui/SettingsModal';
import { UnavailableModal } from './components/ui/UnavailableModal';
import { useFeesStream } from './hooks/useFeesStream';
import { DEFAULT_METRIC, metricPath } from './lib/metrics';
import { MetricView } from './views/MetricView';
import { RealTimeGasView } from './views/RealTimeGasView';

const SIDEBAR_STORAGE_KEY = 'af-primary-sidebar-collapsed';

function loadSidebarState(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true';
  } catch {
    return false;
  }
}

/**
 * O shell: sidebar + topbar + rota ativa. Liga o stream (useFeesStream) mas NÃO
 * lê nada dele — nenhum bloco novo re-renderiza esta árvore. Quem lê são as
 * folhas, via useFeesSlice. É o contrato do RNF-03; se um dia alguém puxar o
 * snapshot para cá, o Profiler acusa a árvore inteira piscando a cada 12 s.
 */
export function App() {
  useFeesStream();
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [unavailableOpen, setUnavailableOpen] = useState(false);
  const [primaryCollapsed, setPrimaryCollapsed] = useState(loadSidebarState);

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_STORAGE_KEY, String(primaryCollapsed));
    } catch {
      // O colapso continua funcionando durante a sessão se o storage estiver bloqueado.
    }
  }, [primaryCollapsed]);

  return (
    <div className={primaryCollapsed ? 'shell is-primary-collapsed' : 'shell'}>
      <Sidebar
        collapsed={primaryCollapsed}
        onToggleCollapsed={() => setPrimaryCollapsed((collapsed) => !collapsed)}
        onUnavailable={() => setUnavailableOpen(true)}
      />
      <div className="shell__main">
        <TopBar
          onOpenSettings={() => setSettingsOpen(true)}
          onUnavailable={() => setUnavailableOpen(true)}
        />
        <div className="shell__assetbar">
          <MetricsHeader />
        </div>
        <div className="shell__workspace">
          <MetricsSidebar onUnavailable={() => setUnavailableOpen(true)} />
          <main className="shell__content">
            <Routes>
              <Route
                path="/"
                element={<Navigate to={metricPath(DEFAULT_METRIC.id)} replace />}
              />
              {/* A antiga tela ao vivo continua preservada para acesso direto,
                  mas o recorte navegável replica as quatro métricas de Fees. */}
              <Route path="/real-time-gas" element={<RealTimeGasView />} />
              <Route path="/metrics/:metricId" element={<MetricView />} />
              <Route
                path="/metrics"
                element={<Navigate to={metricPath(DEFAULT_METRIC.id)} replace />}
              />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
      </div>
      <SettingsModal open={settingsOpen} onClose={() => setSettingsOpen(false)} />
      <UnavailableModal
        open={unavailableOpen}
        onClose={() => setUnavailableOpen(false)}
      />
    </div>
  );
}
