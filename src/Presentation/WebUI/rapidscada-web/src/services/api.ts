import axios from 'axios';
import { useAuthStore } from '../stores/authStore';

const api = axios.create({
  baseURL: 'https://localhost:{}/api',
  timeout: 30000,
});


// Request interceptor - add JWT token
api.interceptors.request.use(
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
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = useAuthStore.getState().refreshToken;
        const response = await axios.post('/api/auth/refresh', { refreshToken });
        const { accessToken } = response.data;

        useAuthStore.getState().updateToken(accessToken);
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;

        return api(originalRequest);
      } catch (refreshError) {
        useAuthStore.getState().logout();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;

// Auth API
export const authService = {
  login: (username: string, password: string) =>
    api.post('/auth/login', { username, password }),
  
  register: (data: { userName: string; email: string; password: string }) =>
    api.post('/auth/register', data),
  
  logout: () => api.post('/auth/logout'),
  
  refreshToken: (refreshToken: string) =>
    api.post('/auth/refresh', { refreshToken }),
};

// Device API
export const deviceService = {
  getAll: () => api.get('/devices'),
  
  getById: (id: number) => api.get(`/devices/${id}`),
  
  create: (device: any) => api.post('/devices', device),
  
  update: (id: number, device: any) => api.put(`/devices/${id}`, device),
  
  delete: (id: number) => api.delete(`/devices/${id}`),
  
  getTags: (id: number) => api.get(`/devices/${id}/tags`),
};

// Tag API
export const tagService = {
  getAll: () => api.get('/tags'),
  
  getById: (id: number) => api.get(`/tags/${id}`),
  
  create: (tag: any) => api.post('/tags', tag),
  
  update: (id: number, tag: any) => api.put(`/tags/${id}`, tag),
  
  delete: (id: number) => api.delete(`/tags/${id}`),
  
  write: (id: number, value: number | string | boolean) =>
    api.post(`/tags/${id}/write`, { value }),
};

// Alarm API
export const alarmService = {
  getActive: () => api.get('/alarms/active'),
  
  getHistory: (params?: { from?: string; to?: string; severity?: number }) =>
    api.get('/alarms/history', { params }),
  
  acknowledge: (id: string) => api.post(`/alarms/${id}/acknowledge`),
  
  getRules: () => api.get('/alarms/rules'),
  
  createRule: (rule: any) => api.post('/alarms/rules', rule),
  
  updateRule: (id: string, rule: any) => api.put(`/alarms/rules/${id}`, rule),
  
  deleteRule: (id: string) => api.delete(`/alarms/rules/${id}`),
};

// Historical Data API
export const historicalService = {
  getTagHistory: (tagId: number, start: string, end: string, interval?: string) =>
    api.get(`/historical/tags/${tagId}`, {
      params: { start, end, interval },
    }),
  
  getMultipleTagsHistory: (tagIds: number[], start: string, end: string) =>
    api.post('/historical/multiple', { tagIds, start, end }),
};

// System API
export const systemService = {
  getServicesStatus: () => api.get('/system/services'),
  
  restartService: (serviceName: string) =>
    api.post(`/system/services/${serviceName}/restart`),
  
  getCommunicatorStatus: () => api.get('/system/communicator'),
  
  getLogs: (serviceName?: string, level?: string) =>
    api.get('/system/logs', { params: { serviceName, level } }),
};

// User API
export const userService = {
  getAll: () => api.get('/users'),
  
  getById: (id: number) => api.get(`/users/${id}`),
  
  create: (user: any) => api.post('/users', user),
  
  update: (id: number, user: any) => api.put(`/users/${id}`, user),
  
  delete: (id: number) => api.delete(`/users/${id}`),
  
  changePassword: (id: number, oldPassword: string, newPassword: string) =>
    api.post(`/users/${id}/change-password`, { oldPassword, newPassword }),
};
