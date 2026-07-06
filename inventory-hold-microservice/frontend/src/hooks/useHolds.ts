import { useMutation, useQueryClient } from '@tanstack/react-query';
import { holdsApi } from '../api/holds';
import type { CreateHoldRequest, Hold } from '../api/types';

export function useCreateHold() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateHoldRequest) => holdsApi.create(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] })
  });
}

export function useReleaseHold() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (holdId: string) => holdsApi.release(holdId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] })
  });
}
