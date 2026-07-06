import { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import InventoryDashboard from './components/InventoryDashboard';
import CreateHoldForm from './components/CreateHoldForm';
import ActiveHoldsList from './components/ActiveHoldsList';
import type { Hold } from './api/types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1 },
    mutations: { retry: 0 }
  }
});

export default function App() {
  const [holds, setHolds] = useState<Hold[]>([]);

  return (
    <QueryClientProvider client={queryClient}>
      <main style={{ maxWidth: 1000, margin: '0 auto', padding: '1rem', fontFamily: 'sans-serif' }}>
        <h1>Inventory Hold Manager</h1>
        <section>
          <h2>Current Inventory</h2>
          <InventoryDashboard />
        </section>
        <section>
          <h2>Place a Hold</h2>
          <CreateHoldForm onHoldCreated={(hold) => setHolds((prev) => [...prev, hold])} />
        </section>
        <section>
          <h2>Active Holds</h2>
          <ActiveHoldsList holds={holds} onHoldReleased={(id) => setHolds((prev) => prev.filter((h) => h.holdId !== id))} />
        </section>
      </main>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
