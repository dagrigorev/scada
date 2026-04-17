// Device types
export interface Device {
  id: number;
  name: string;
  deviceTypeId: number;
  address: number;
  communicationLineId: number;
  enabled: boolean;
  pollInterval: number;
  timeout: number;
  isOnline?: boolean;
  lastCommunication?: string;
  tagCount?: number;
}

// Tag types
export interface Tag {
  id: number;
  tagNumber: number;
  name: string;
  deviceId: number;
  deviceName?: string;
  dataType: 'Double' | 'Integer' | 'Boolean' | 'String';
  currentValue?: number | string | boolean;
  quality?: number;
  timestamp?: string;
  isWritable: boolean;
  archiveEnabled: boolean;
  unit?: string;
}

export interface TagUpdate {
  tagId: number;
  value: number | string | boolean;
  quality: number;
  timestamp: string;
}

// Alarm types
export enum AlarmSeverity {
  Critical = 0,
  High = 1,
  Warning = 2,
  Low = 3,
  Info = 4,
}

export enum AlarmState {
  Active = 0,
  Acknowledged = 1,
  Cleared = 2,
  Suppressed = 3,
}

export interface Alarm {
  id: string;
  ruleId: string;
  ruleName: string;
  message: string;
  severity: AlarmSeverity;
  state: AlarmState;
  priority: number;
  tagId?: number;
  tagName?: string;
  deviceId?: number;
  deviceName?: string;
  triggeredAt: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  clearedAt?: string;
}

export interface AlarmRule {
  id: string;
  name: string;
  enabled: boolean;
  tagId: number;
  severity: AlarmSeverity;
  priority: number;
  condition: string;
  threshold?: number;
  message: string;
}

// User types
export interface User {
  id: number;
  userName: string;
  email: string;
  roles: string[];
  isActive: boolean;
  lastLogin?: string;
  createdAt: string;
}

// System types
export interface ServiceStatus {
  name: string;
  displayName: string;
  status: 'Running' | 'Stopped' | 'Error';
  uptime: string;
  cpuUsage: number;
  memoryUsage: number;
  port?: number;
}

export interface CommunicationLine {
  id: number;
  name: string;
  enabled: boolean;
  deviceCount: number;
  pollInterval: number;
  successRate: number;
  lastPoll?: string;
}

// Historical data types
export interface HistoricalDataPoint {
  timestamp: string;
  value: number;
  quality: number;
}

export interface HistoricalQuery {
  tagIds: number[];
  startTime: string;
  endTime: string;
  aggregation?: 'raw' | 'avg' | 'min' | 'max';
  interval?: string;
}
