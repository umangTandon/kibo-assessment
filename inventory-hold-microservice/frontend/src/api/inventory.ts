import type { InventoryItem } from './types';
import { apiClient } from './client';

export const inventoryApi = {
  getAll: () => apiClient.get<InventoryItem[]>('/api/inventory')
};
