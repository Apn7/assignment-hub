import type { HealthResponse } from '@/types/api';
import { apiClient } from './client';

/** Query key used by TanStack Query to cache the health probe. */
export const healthQueryKey = ['health'] as const;

/** Calls `GET /api/health` on the backend. */
export async function getHealth(): Promise<HealthResponse> {
  const { data } = await apiClient.get<HealthResponse>('/api/health');
  return data;
}
