import { useFeesStream } from './hooks/useFeesStream';

export function App() {
  const { snapshot, status } = useFeesStream();

  return (
    <main>
      <h1>Ethereum · custo de taxa em tempo real</h1>
      <p>Conexão: {status}</p>
      {snapshot ? (
        <p>
          Bloco {snapshot.blockNumber} · base fee {snapshot.baseFeeGwei} gwei
        </p>
      ) : (
        <p>Aguardando o primeiro bloco…</p>
      )}
    </main>
  );
}
