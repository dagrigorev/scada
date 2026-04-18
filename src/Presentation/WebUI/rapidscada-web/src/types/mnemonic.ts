// Mnemonic Scheme Types

export type ComponentType =
  // Electrical Substation Components
  | 'transformer'
  | 'circuit-breaker'
  | 'disconnector'
  | 'bus-bar'
  | 'power-line'
  | 'generator'
  | 'load'
  | 'capacitor'
  | 'reactor'
  | 'voltage-transformer'
  | 'current-transformer'
  | 'surge-arrester'
  // Energetic Components
  | 'pump'
  | 'valve'
  | 'tank'
  | 'pipe'
  | 'heat-exchanger'
  | 'compressor'
  | 'turbine'
  | 'boiler'
  // Indicators & Display
  | 'gauge'
  | 'indicator'
  | 'text-label'
  | 'alarm-indicator';

export type ComponentState = 'normal' | 'warning' | 'alarm' | 'offline' | 'maintenance';

export interface TagBinding {
  tagId: number;
  tagName: string;
  property: 'value' | 'status' | 'color' | 'rotation' | 'visibility';
}

export interface MnemoComponent {
  id: string;
  type: ComponentType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  scaleX: number;
  scaleY: number;
  
  // Visual properties
  fillColor: string;
  strokeColor: string;
  strokeWidth: number;
  opacity: number;
  
  // State & Animation
  state: ComponentState;
  animated: boolean;
  animationSpeed: number;
  
  // Tag bindings
  bindings: TagBinding[];
  
  // Component-specific properties
  properties: Record<string, any>;
  
  // Metadata
  name: string;
  description: string;
  layer: number;
  locked: boolean;
  visible: boolean;
}

export interface Connection {
  id: string;
  from: string; // component ID
  to: string;   // component ID
  fromPort?: string;
  toPort?: string;
  points: number[]; // [x1, y1, x2, y2, ...]
  strokeColor: string;
  strokeWidth: number;
  animated: boolean;
  flowDirection: 'forward' | 'backward' | 'bidirectional';
}

export interface MnemoScheme {
  id: string;
  name: string;
  description: string;
  width: number;
  height: number;
  backgroundColor: string;
  gridSize: number;
  snapToGrid: boolean;
  
  components: MnemoComponent[];
  connections: Connection[];
  
  // Alarm tracking
  activeAlarms: number;
  warningCount: number;
  
  // Metadata
  createdAt: string;
  updatedAt: string;
  version: number;
}

export interface EditorState {
  mode: 'edit' | 'display';
  selectedTool: ComponentType | 'select' | 'pan' | 'connect';
  selectedComponents: string[];
  clipboard: MnemoComponent[];
  history: MnemoScheme[];
  historyIndex: number;
  zoom: number;
  panX: number;
  panY: number;
  showGrid: boolean;
  snapToGrid: boolean;
  showAlarms: boolean;
}

export interface RealTimeData {
  tagId: number;
  value: number;
  quality: number;
  timestamp: string;
}
