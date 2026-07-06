import { useEffect, useState } from 'react';
import type { Hold } from '../api/types';
import { useReleaseHold } from '../hooks/useHolds';

interface Props {
  hold: Hold;
  onReleased: (holdId: string) => void;
}

export default function HoldRow({ hold, onReleased }: Props) {
  const [now, setNow] = useState(Date.now());
  const { mutateAsync, isPending, isError, error } = useReleaseHold();

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const msLeft = new Date(hold.expiresAt).getTime() - now;
  const countdown = msLeft > 0 ? `${Math.max(0, Math.floor(msLeft / 60000))}m ${Math.max(0, Math.floor((msLeft % 60000) / 1000))}s` : 'Expired';

  const handleRelease = async () => {
    if (!window.confirm(`Release hold ${hold.holdId}?`)) return;
    await mutateAsync(hold.holdId);
    onReleased(hold.holdId);
  };

  return (
    <tr>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>{hold.holdId.slice(0, 8)}</td>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>{hold.productId}</td>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee', textAlign: 'right' }}>{hold.quantity}</td>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>{hold.status}</td>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>{countdown}</td>
      <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>
        <button onClick={handleRelease} disabled={isPending || hold.status !== 'Active'}>
          Release
        </button>
        {isError && <div style={{ color: 'red' }}>{(error as Error).message}</div>}
      </td>
    </tr>
  );
}
