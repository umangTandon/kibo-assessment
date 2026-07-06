import type { CreateHoldRequest, Hold } from './types';
import { apiClient } from './client';

export const holdsApi = {
  create: (request: CreateHoldRequest) => apiClient.post<Hold>('/api/holds', request),
  release: (holdId: string) => apiClient.delete<Hold>(`/api/holds/${holdId}`)
};
