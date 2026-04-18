import axios, { AxiosInstance } from 'axios';
import { useAuthStore } from '../stores/authStore';

// Development vs Production URLs
const isDevelopment = true;

// API Configuration
const API_ENDPOINTS = !isDevelopment
  ? {
      // Development - use Vite proxy
      identity: '/api/identity',
      webapi: '/api',
      realtime: '/api/realtime',
      communicator: '/api/communicator',
      archiver: '/api/archiver',
    }
  : {
      // Production - direct service URLs
      identity: 'https://localhost:5003/api',
      webapi: 'https://localhost:5001/api',
      realtime: 'https://localhost:5005/api',
      communicator: 'https://localhost:5007/api',
      archiver: 'https://localhost:5009/api',
    };

// Create axios instance with interceptors
const createApiInstance = (baseURL: string): AxiosInstance => {
  const instance = axios.create({
    baseURL,
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Request interceptor - add JWT token
  instance.interceptors.request.use(
    (config) => {
      const token = useAuthStore.getState().token;
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Response interceptor - handle 401 and refresh token
  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;

      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

        try {
          const refreshToken = useAuthStore.getState().refreshToken;
          
          // Use identity endpoint for refresh
          const refreshUrl = !isDevelopment
            ? '/identity/auth/refresh'
            : `${API_ENDPOINTS.identity}/auth/refresh`;
            
          const response = await axios.post(refreshUrl, { refreshToken });
          const { accessToken } = response.data;

          useAuthStore.getState().updateToken(accessToken);
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;

          return instance(originalRequest);
        } catch (refreshError) {
          useAuthStore.getState().logout();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }

      return Promise.reject(error);
    }
  );

  return instance;
};

// Create service-specific API instances
const identityApi = createApiInstance(API_ENDPOINTS.identity);
const webapiApi = createApiInstance(API_ENDPOINTS.webapi);
const realtimeApi = createApiInstance(API_ENDPOINTS.realtime);
const communicatorApi = createApiInstance(API_ENDPOINTS.communicator);
const archiverApi = createApiInstance(API_ENDPOINTS.archiver);

// Default export for backward compatibility
export default webapiApi;

// ============================================
// Auth API - Identity Service
// ============================================
export const authService = {
  login: (username: string, password: string) =>
    identityApi.post('/auth/login', { username, password }),

  register: (data: { userName: string; email: string; password: string }) =>
    identityApi.post('/auth/register', data),

  logout: () => identityApi.post('/auth/logout'),

  refreshToken: (refreshToken: string) =>
    identityApi.post('/auth/refresh', { refreshToken }),

  getCurrentUser: () => identityApi.get('/auth/me'),
};

// ============================================
// Device API - WebAPI Service
// ============================================
export const deviceService = {
  getAll: () => webapiApi.get('/devices'),
  getById: (id: number) => webapiApi.get(`/devices/${id}`),
  create: (device: any) => webapiApi.post('/devices', device),
  update: (id: number, device: any) => webapiApi.put(`/devices/${id}`, device),
  delete: (id: number) => webapiApi.delete(`/devices/${id}`),
  getTags: (id: number) => webapiApi.get(`/devices/${id}/tags`),
  getStatistics: () => webapiApi.get('/devices/statistics'),
};

// ============================================
// Tag API - WebAPI Service
// ============================================
export const tagService = {
  getAll: () => webapiApi.get('/tags'),
  getById: (id: number) => webapiApi.get(`/tags/${id}`),
  create: (tag: any) => webapiApi.post('/tags', tag),
  update: (id: number, tag: any) => webapiApi.put(`/tags/${id}`, tag),
  delete: (id: number) => webapiApi.delete(`/tags/${id}`),
  write: (id: number, value: number | string | boolean) =>
    webapiApi.post(`/tags/${id}/write`, { value }),
  getCurrentValues: (tagIds: number[]) =>
    webapiApi.post('/tags/current-values', { tagIds }),
};

// ============================================
// Alarm API - WebAPI Service
// ============================================
export const alarmService = {
  getActive: () => webapiApi.get('/alarms/active'),
  getHistory: (params?: { from?: string; to?: string; severity?: number }) =>
    webapiApi.get('/alarms/history', { params }),
  acknowledge: (id: string) => webapiApi.post(`/alarms/${id}/acknowledge`),
  getRules: () => webapiApi.get('/alarms/rules'),
  createRule: (rule: any) => webapiApi.post('/alarms/rules', rule),
  updateRule: (id: string, rule: any) => webapiApi.put(`/alarms/rules/${id}`, rule),
  deleteRule: (id: string) => webapiApi.delete(`/alarms/rules/${id}`),
  getStatistics: () => webapiApi.get('/alarms/statistics'),
};

// ============================================
// Historical Data API - Archiver Service
// ============================================
export const historicalService = {
  getTagHistory: (tagId: number, start: string, end: string, interval?: string) =>
    archiverApi.get(`/historical/tags/${tagId}`, {
      params: { start, end, interval },
    }),
  getMultipleTagsHistory: (tagIds: number[], start: string, end: string) =>
    archiverApi.post('/historical/multiple', { tagIds, start, end }),
  getAggregatedData: (
    tagId: number,
    start: string,
    end: string,
    aggregation: 'avg' | 'min' | 'max' | 'sum'
  ) =>
    archiverApi.get(`/historical/tags/${tagId}/aggregate`, {
      params: { start, end, aggregation },
    }),
  exportToCsv: (tagIds: number[], start: string, end: string) =>
    archiverApi.post(
      '/historical/export/csv',
      { tagIds, start, end },
      { responseType: 'blob' }
    ),
};

// ============================================
// System API - WebAPI Service
// ============================================
export const systemService = {
  getServicesStatus: () => webapiApi.get('/system/services'),
  restartService: (serviceName: string) =>
    webapiApi.post(`/system/services/${serviceName}/restart`),
  getCommunicatorStatus: () => communicatorApi.get('/status'),
  getLogs: (serviceName?: string, level?: string) =>
    webapiApi.get('/system/logs', { params: { serviceName, level } }),
  getSystemHealth: () => webapiApi.get('/system/health'),
};

// ============================================
// Communicator API - Communicator Service
// ============================================
export const communicatorService = {
  getStatus: () => communicatorApi.get('/status'),
  getDevices: () => communicatorApi.get('/devices'),
  getCommunicationLines: () => communicatorApi.get('/communication-lines'),
  startPolling: () => communicatorApi.post('/polling/start'),
  stopPolling: () => communicatorApi.post('/polling/stop'),
  getPollingStatistics: () => communicatorApi.get('/polling/statistics'),
  testConnection: (deviceId: number) =>
    communicatorApi.post(`/devices/${deviceId}/test-connection`),
};

// ============================================
// User API - Identity Service
// ============================================
export const userService = {
  getAll: () => identityApi.get('/users'),
  getById: (id: number) => identityApi.get(`/users/${id}`),
  create: (user: any) => identityApi.post('/users', user),
  update: (id: number, user: any) => identityApi.put(`/users/${id}`, user),
  delete: (id: number) => identityApi.delete(`/users/${id}`),
  changePassword: (id: number, oldPassword: string, newPassword: string) =>
    identityApi.post(`/users/${id}/change-password`, { oldPassword, newPassword }),
  getRoles: () => identityApi.get('/roles'),
  assignRole: (userId: number, roleId: number) =>
    identityApi.post(`/users/${userId}/roles/${roleId}`),
  removeRole: (userId: number, roleId: number) =>
    identityApi.delete(`/users/${userId}/roles/${roleId}`),
};

// ============================================
// Realtime API - Realtime Service
// ============================================
export const realtimeService = {
  getConnectedClients: () => realtimeApi.get('/clients'),
  broadcastMessage: (message: string) =>
    realtimeApi.post('/broadcast', { message }),
  getActiveSubscriptions: () => realtimeApi.get('/subscriptions'),
};

// ============================================
// Export API instances for advanced usage
// ============================================
export const apiInstances = {
  identity: identityApi,
  webapi: webapiApi,
  realtime: realtimeApi,
  communicator: communicatorApi,
  archiver: archiverApi,
};
