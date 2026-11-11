import { ClassItem, CreateClassRequest, HoldResponse, HoldStatusResponse, ConfirmHoldResponse, StudentHold, ApiError } from '../types';

const API_BASE = ''; // Uses Vite reverse proxy

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers || {});
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (!res.ok) {
    let errorData: ApiError;
    try {
      errorData = await res.json();
    } catch {
      errorData = {
        status: res.status,
        error: res.statusText,
        message: 'An unexpected network error occurred.',
        timestamp: new Date().toISOString(),
      };
    }
    throw errorData;
  }

  if (res.status === 204) {
    return {} as T;
  }

  return res.json() as Promise<T>;
}

export const api = {
  // Classes
  getClasses: (page?: number, pageSize?: number) => {
    const params = new URLSearchParams();
    if (page !== undefined && pageSize !== undefined) {
      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());
    }
    const query = params.toString() ? `?${params.toString()}` : '';
    return request<ClassItem[]>(`/classes${query}`);
  },

  getClassById: (classId: string) =>
    request<ClassItem>(`/classes/${encodeURIComponent(classId)}`),

  createClass: (data: CreateClassRequest) =>
    request<{ id: string; name: string; totalSeats: number }>('/classes', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Holds
  createHold: (classId: string, studentId: string) =>
    request<HoldResponse>(`/classes/${encodeURIComponent(classId)}/holds`, {
      method: 'POST',
      headers: { 'X-Student-Id': studentId },
    }),

  getHoldById: (holdId: string) =>
    request<HoldStatusResponse>(`/holds/${encodeURIComponent(holdId)}`),

  confirmHold: (holdId: string, studentId: string) =>
    request<ConfirmHoldResponse>(`/holds/${encodeURIComponent(holdId)}/confirm`, {
      method: 'POST',
      headers: { 'X-Student-Id': studentId },
    }),

  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${encodeURIComponent(holdId)}`, {
      method: 'DELETE',
      headers: { 'X-Student-Id': studentId },
    }),

  // Student specific holds
  getMyHolds: (studentId: string) =>
    request<StudentHold[]>('/me/holds', {
      headers: { 'X-Student-Id': studentId },
    }),
};
