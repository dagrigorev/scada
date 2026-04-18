import api from '@/services/api';
import { useQuery } from '@tanstack/react-query';

export interface ServiceInfo {
  name: string;
  description: string;
  version: string;
  baseUrl: string;
  healthEndpoint: string;
  endpoints: string[];
  capabilities: string[];
  status: string;
  requiresAuth: boolean;
}

export interface ServiceDiscoveryResponse {
  services: ServiceInfo[];
  totalServices: number;
  environment: string;
  timestamp: string;
}

export interface ServiceHealthInfo {
  serviceName: string;
  isHealthy: boolean;
  status: string;
  responseTimeMs: number;
  lastChecked: string;
  message: string;
}

export interface ServiceHealthResponse {
  overallStatus: string;
  services: ServiceHealthInfo[];
  timestamp: string;
}

export interface EndpointInfo {
  serviceName: string;
  path: string;
  fullUrl: string;
  method: string;
  requiresAuth: boolean;
  description: string;
}

export interface EndpointsResponse {
  endpoints: EndpointInfo[];
  totalEndpoints: number;
  timestamp: string;
}

export function useServiceDiscovery() {
  return useQuery<ServiceDiscoveryResponse>({
    queryKey: ['service-discovery'],
    queryFn: async () => {
      const response = await api.get('/discovery/services');
      return response.data;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });
}

export function useServiceHealth() {
  return useQuery<ServiceHealthResponse>({
    queryKey: ['service-health'],
    queryFn: async () => {
      const response = await api.get('/discovery/health');
      return response.data;
    },
    refetchInterval: 10000, // Refresh every 10 seconds
  });
}

export function useServiceEndpoints() {
  return useQuery<EndpointsResponse>({
    queryKey: ['service-endpoints'],
    queryFn: async () => {
      const response = await api.get('/discovery/endpoints');
      return response.data;
    },
  });
}
