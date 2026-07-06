import type { Hold } from '../api/types';
import HoldRow from './HoldRow';

interface Props {
  holds: Hold[];
  onHoldReleased: (holdId: string) => void;
}

export default function ActiveHoldsList({ holds, onHoldReleased }: Props) {
  if (holds.length === 0) return <p>No active holds.</p>;

  return (
    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
      <thead>
        <tr>
          <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Hold ID</th>
          <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Product</th>
          <th style={{ textAlign: 'right', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Qty</th>
          <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Status</th>
          <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Time Remaining</th>
          <th style={{ textAlign: 'left', padding: '0.5rem', borderBottom: '1px solid #ccc' }}>Action</th>
        </tr>
      </thead>
      <tbody>
        {holds.map((hold) => (
          <HoldRow key={hold.holdId} hold={hold} onReleased={onHoldReleased} />
        ))}
      </tbody>
    </table>
  );
}
