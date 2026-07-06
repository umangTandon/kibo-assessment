import { useInventory } from '../hooks/useInventory';

export default function InventoryDashboard() {
  const { data, isLoading, isError, error } = useInventory();

  if (isLoading) return <p>Loading inventory...</p>;
  if (isError) return <p style={{ color: 'red' }}>Failed to load inventory: {(error as Error).message}</p>;

  return (
    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
      <thead>
        <tr>
          <th style={{ textAlign: 'left', borderBottom: '1px solid #ccc', padding: '0.5rem' }}>Product Name</th>
          <th style={{ textAlign: 'right', borderBottom: '1px solid #ccc', padding: '0.5rem' }}>Available</th>
          <th style={{ textAlign: 'right', borderBottom: '1px solid #ccc', padding: '0.5rem' }}>Reserved</th>
          <th style={{ textAlign: 'right', borderBottom: '1px solid #ccc', padding: '0.5rem' }}>Total</th>
        </tr>
      </thead>
      <tbody>
        {data?.map((item) => (
          <tr key={item.productId}>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee' }}>{item.productName}</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee', textAlign: 'right' }}>{item.availableStock}</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee', textAlign: 'right' }}>{item.reservedStock}</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #eee', textAlign: 'right' }}>{item.totalStock}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
