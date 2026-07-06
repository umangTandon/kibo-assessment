import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query';
import { holdsApi } from '../api/holds';
import type { CreateHoldRequest, Hold } from '../api/types';

export function useCreateHold(): UseMutationResult<Hold, Error, CreateHoldRequest, unknown> {
  const queryClient = useQueryClient();
  return useMutation<Hold, Error, CreateHoldRequest>({
    mutationFn: (request: CreateHoldRequest) => holdsApi.create(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] })
  });
}

export function useReleaseHold(): UseMutationResult<Hold, Error, string, unknown> {
  const queryClient = useQueryClient();
  return useMutation<Hold, Error, string>({
    mutationFn: (holdId: string) => holdsApi.release(holdId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] })
  });
}
