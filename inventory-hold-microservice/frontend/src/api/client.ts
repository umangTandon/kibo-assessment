import type { ApiError } from './types';

export class ApiErrorWithStatus extends Error {
  constructor(message: string, public status: number, public code?: string) {
    super(message);
  }
}

const defaultHeaders = {
  'Content-Type': 'application/json'
};

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as unknown as T;
  }

  const body = await response.json().catch(() => null);
  if (!response.ok) {
    const error = body as ApiError | null;
    throw new ApiErrorWithStatus(error?.message ?? response.statusText, response.status, error?.code);
  }

  return body as T;
}

export const apiClient = {
  async get<T>(url: string) {
    const response = await fetch(url, { headers: defaultHeaders });
    return parseResponse<T>(response);
  },

  async post<T>(url: string, body: unknown) {
    const response = await fetch(url, {
      method: 'POST',
      headers: defaultHeaders,
      body: JSON.stringify(body)
    });
    return parseResponse<T>(response);
  },

  async delete<T>(url: string) {
    const response = await fetch(url, {
      method: 'DELETE',
      headers: defaultHeaders
    });
    return parseResponse<T>(response);
  }
};
