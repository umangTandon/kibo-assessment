import { useState } from 'react';
import type { CreateHoldRequest, Hold } from '../api/types';
import { useCreateHold } from '../hooks/useHolds';
import { useInventory } from '../hooks/useInventory';

interface Props {
  onHoldCreated: (hold: Hold) => void;
}

export default function CreateHoldForm({ onHoldCreated }: Props) {
  const { data: inventory, isLoading } = useInventory();
  const [productId, setProductId] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [customerId, setCustomerId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const mutation = useCreateHold();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    try {
      const hold = await mutation.mutateAsync({ productId, quantity, customerId: customerId || undefined });
      onHoldCreated(hold);
      setProductId('');
      setQuantity(1);
      setCustomerId('');
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div style={{ marginBottom: '0.75rem' }}>
        <label>
          Product{' '}
          <select value={productId} onChange={(e) => setProductId(e.target.value)} disabled={isLoading || mutation.isPending} style={{ minWidth: 240 }}>
            <option value="">Select a product</option>
            {inventory?.map((item) => (
              <option key={item.productId} value={item.productId}>
                {item.productName} ({item.availableStock} available)
              </option>
            ))}
          </select>
        </label>
      </div>
      <div style={{ marginBottom: '0.75rem' }}>
        <label>
          Quantity{' '}
          <input type="number" min="1" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} disabled={mutation.isPending} />
        </label>
      </div>
      <div style={{ marginBottom: '0.75rem' }}>
        <label>
          Customer ID{' '}
          <input type="text" value={customerId} onChange={(e) => setCustomerId(e.target.value)} disabled={mutation.isPending} />
        </label>
      </div>
      <button type="submit" disabled={mutation.isPending || !productId || quantity <= 0}>
        {mutation.isPending ? 'Placing hold…' : 'Place Hold'}
      </button>
      {error && <p style={{ color: 'red' }}>{error}</p>}
    </form>
  );
}
