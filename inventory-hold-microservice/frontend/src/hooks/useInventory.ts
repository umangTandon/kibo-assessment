import { useQuery } from '@tanstack/react-query';
import { inventoryApi } from '../api/inventory';

export function useInventory() {
  return useQuery({
    queryKey: ['inventory'],
    queryFn: inventoryApi.getAll,
    staleTime: 30_000,
    refetchInterval: 60_000
  });
}
