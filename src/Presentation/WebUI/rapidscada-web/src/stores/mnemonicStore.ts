import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { MnemoScheme, MnemoComponent, Connection, EditorState, ComponentType } from '../types/mnemonic';

interface MnemonicStore extends EditorState {
  currentScheme: MnemoScheme | null;
  schemes: MnemoScheme[];
  
  // Scheme management
  createScheme: (name: string) => void;
  loadScheme: (id: string) => void;
  saveScheme: () => void;
  deleteScheme: (id: string) => void;
  setCurrentScheme: (scheme: MnemoScheme) => void;
  
  // Component management
  addComponent: (type: ComponentType, x: number, y: number) => void;
  updateComponent: (id: string, updates: Partial<MnemoComponent>) => void;
  deleteComponent: (id: string) => void;
  duplicateComponent: (id: string) => void;
  
  // Connection management
  addConnection: (fromId: string, toId: string) => void;
  updateConnection: (id: string, updates: Partial<Connection>) => void;
  deleteConnection: (id: string) => void;
  
  // Selection
  selectComponent: (id: string, multi?: boolean) => void;
  clearSelection: () => void;
  selectAll: () => void;
  
  // Clipboard
  copy: () => void;
  paste: () => void;
  cut: () => void;
  
  // History (Undo/Redo)
  undo: () => void;
  redo: () => void;
  pushHistory: () => void;
  
  // Editor state
  setMode: (mode: 'edit' | 'display') => void;
  setSelectedTool: (tool: ComponentType | 'select' | 'pan' | 'connect') => void;
  setZoom: (zoom: number) => void;
  setPan: (x: number, y: number) => void;
  toggleGrid: () => void;
  toggleSnapToGrid: () => void;
  
  // Real-time data
  updateComponentValue: (componentId: string, tagId: number, value: number) => void;
  updateAlarmCounts: (activeAlarms: number, warningCount: number) => void;
}

export const useMnemonicStore = create<MnemonicStore>((set, get) => ({
  // Initial state
  mode: 'edit',
  selectedTool: 'select',
  selectedComponents: [],
  clipboard: [],
  history: [],
  historyIndex: -1,
  zoom: 1,
  panX: 0,
  panY: 0,
  showGrid: true,
  snapToGrid: true,
  showAlarms: true,
  currentScheme: null,
  schemes: [],

  // Scheme management
  createScheme: (name) => {
    const newScheme: MnemoScheme = {
      id: uuidv4(),
      name,
      description: '',
      width: 1920,
      height: 1080,
      backgroundColor: '#1a1a2e',
      gridSize: 20,
      snapToGrid: true,
      components: [],
      connections: [],
      activeAlarms: 0,
      warningCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      version: 1,
    };
    
    set((state) => ({
      schemes: [...state.schemes, newScheme],
      currentScheme: newScheme,
      history: [newScheme],
      historyIndex: 0,
    }));
  },

  loadScheme: (id) => {
    const scheme = get().schemes.find((s) => s.id === id);
    if (scheme) {
      set({ currentScheme: scheme, history: [scheme], historyIndex: 0 });
    }
  },

  saveScheme: () => {
    const { currentScheme, schemes } = get();
    if (!currentScheme) return;

    const updatedScheme = {
      ...currentScheme,
      updatedAt: new Date().toISOString(),
      version: currentScheme.version + 1,
    };

    set({
      currentScheme: updatedScheme,
      schemes: schemes.map((s) => (s.id === updatedScheme.id ? updatedScheme : s)),
    });
  },

  deleteScheme: (id) => {
    set((state) => ({
      schemes: state.schemes.filter((s) => s.id !== id),
      currentScheme: state.currentScheme?.id === id ? null : state.currentScheme,
    }));
  },

  setCurrentScheme: (scheme) => {
    set({ currentScheme: scheme, history: [scheme], historyIndex: 0 });
  },

  // Component management
  addComponent: (type, x, y) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    const componentDefaults = getComponentDefaults(type);
    const newComponent: MnemoComponent = {
      id: uuidv4(),
      type,
      x,
      y,
      rotation: 0,
      scaleX: 1,
      scaleY: 1,
      state: 'normal',
      animated: false,
      animationSpeed: 1,
      bindings: [],
      properties: {},
      name: `${type}-${Date.now()}`,
      description: '',
      layer: 0,
      locked: false,
      visible: true,
      ...componentDefaults,
    };

    set({
      currentScheme: {
        ...currentScheme,
        components: [...currentScheme.components, newComponent],
      },
    });

    get().pushHistory();
  },

  updateComponent: (id, updates) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({
      currentScheme: {
        ...currentScheme,
        components: currentScheme.components.map((c) =>
          c.id === id ? { ...c, ...updates } : c
        ),
      },
    });
  },

  deleteComponent: (id) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({
      currentScheme: {
        ...currentScheme,
        components: currentScheme.components.filter((c) => c.id !== id),
        connections: currentScheme.connections.filter(
          (conn) => conn.from !== id && conn.to !== id
        ),
      },
      selectedComponents: get().selectedComponents.filter((cid) => cid !== id),
    });

    get().pushHistory();
  },

  duplicateComponent: (id) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    const component = currentScheme.components.find((c) => c.id === id);
    if (!component) return;

    const duplicate: MnemoComponent = {
      ...component,
      id: uuidv4(),
      x: component.x + 20,
      y: component.y + 20,
      name: `${component.name}-copy`,
    };

    set({
      currentScheme: {
        ...currentScheme,
        components: [...currentScheme.components, duplicate],
      },
    });

    get().pushHistory();
  },

  // Connection management
  addConnection: (fromId, toId) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    const fromComponent = currentScheme.components.find((c) => c.id === fromId);
    const toComponent = currentScheme.components.find((c) => c.id === toId);

    if (!fromComponent || !toComponent) return;

    const newConnection: Connection = {
      id: uuidv4(),
      from: fromId,
      to: toId,
      points: [
        fromComponent.x + fromComponent.width / 2,
        fromComponent.y + fromComponent.height / 2,
        toComponent.x + toComponent.width / 2,
        toComponent.y + toComponent.height / 2,
      ],
      strokeColor: '#4a9eff',
      strokeWidth: 2,
      animated: false,
      flowDirection: 'forward',
    };

    set({
      currentScheme: {
        ...currentScheme,
        connections: [...currentScheme.connections, newConnection],
      },
    });

    get().pushHistory();
  },

  updateConnection: (id, updates) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({
      currentScheme: {
        ...currentScheme,
        connections: currentScheme.connections.map((c) =>
          c.id === id ? { ...c, ...updates } : c
        ),
      },
    });
  },

  deleteConnection: (id) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({
      currentScheme: {
        ...currentScheme,
        connections: currentScheme.connections.filter((c) => c.id !== id),
      },
    });

    get().pushHistory();
  },

  // Selection
  selectComponent: (id, multi = false) => {
    if (multi) {
      set((state) => ({
        selectedComponents: state.selectedComponents.includes(id)
          ? state.selectedComponents.filter((cid) => cid !== id)
          : [...state.selectedComponents, id],
      }));
    } else {
      set({ selectedComponents: [id] });
    }
  },

  clearSelection: () => {
    set({ selectedComponents: [] });
  },

  selectAll: () => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({ selectedComponents: currentScheme.components.map((c) => c.id) });
  },

  // Clipboard operations
  copy: () => {
    const { currentScheme, selectedComponents } = get();
    if (!currentScheme) return;

    const componentsToCopy = currentScheme.components.filter((c) =>
      selectedComponents.includes(c.id)
    );

    set({ clipboard: componentsToCopy });
  },

  paste: () => {
    const { currentScheme, clipboard } = get();
    if (!currentScheme || clipboard.length === 0) return;

    const pastedComponents = clipboard.map((c) => ({
      ...c,
      id: uuidv4(),
      x: c.x + 20,
      y: c.y + 20,
      name: `${c.name}-copy`,
    }));

    set({
      currentScheme: {
        ...currentScheme,
        components: [...currentScheme.components, ...pastedComponents],
      },
      selectedComponents: pastedComponents.map((c) => c.id),
    });

    get().pushHistory();
  },

  cut: () => {
    get().copy();
    const { selectedComponents } = get();
    selectedComponents.forEach((id) => get().deleteComponent(id));
  },

  // History
  pushHistory: () => {
    const { currentScheme, history, historyIndex } = get();
    if (!currentScheme) return;

    const newHistory = history.slice(0, historyIndex + 1);
    newHistory.push({ ...currentScheme });

    set({
      history: newHistory.slice(-50), // Keep last 50 states
      historyIndex: Math.min(newHistory.length - 1, 49),
    });
  },

  undo: () => {
    const { history, historyIndex } = get();
    if (historyIndex > 0) {
      set({
        currentScheme: history[historyIndex - 1],
        historyIndex: historyIndex - 1,
      });
    }
  },

  redo: () => {
    const { history, historyIndex } = get();
    if (historyIndex < history.length - 1) {
      set({
        currentScheme: history[historyIndex + 1],
        historyIndex: historyIndex + 1,
      });
    }
  },

  // Editor state
  setMode: (mode) => set({ mode }),
  setSelectedTool: (tool) => set({ selectedTool: tool }),
  setZoom: (zoom) => set({ zoom: Math.max(0.1, Math.min(5, zoom)) }),
  setPan: (x, y) => set({ panX: x, panY: y }),
  toggleGrid: () => set((state) => ({ showGrid: !state.showGrid })),
  toggleSnapToGrid: () => set((state) => ({ snapToGrid: !state.snapToGrid })),

  // Real-time data
  updateComponentValue: (componentId, tagId, value) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    const component = currentScheme.components.find((c) => c.id === componentId);
    if (!component) return;

    // Update component based on tag value
    const binding = component.bindings.find((b) => b.tagId === tagId);
    if (!binding) return;

    const updates: Partial<MnemoComponent> = {};

    switch (binding.property) {
      case 'value':
        updates.properties = { ...component.properties, value };
        break;
      case 'status':
        updates.state = value > 0 ? 'normal' : 'offline';
        break;
      case 'color':
        updates.fillColor = getColorForValue(value);
        break;
    }

    get().updateComponent(componentId, updates);
  },

  updateAlarmCounts: (activeAlarms, warningCount) => {
    const { currentScheme } = get();
    if (!currentScheme) return;

    set({
      currentScheme: {
        ...currentScheme,
        activeAlarms,
        warningCount,
      },
    });
  },
}));

// Helper function to get component defaults
function getComponentDefaults(type: ComponentType): Partial<MnemoComponent> {
  const defaults: Record<ComponentType, Partial<MnemoComponent>> = {
    'transformer': { width: 60, height: 80, fillColor: '#4a9eff', strokeColor: '#2563eb', strokeWidth: 2, opacity: 1 },
    'circuit-breaker': { width: 40, height: 60, fillColor: '#22c55e', strokeColor: '#16a34a', strokeWidth: 2, opacity: 1 },
    'disconnector': { width: 40, height: 40, fillColor: '#ef4444', strokeColor: '#dc2626', strokeWidth: 2, opacity: 1 },
    'bus-bar': { width: 200, height: 10, fillColor: '#f59e0b', strokeColor: '#d97706', strokeWidth: 2, opacity: 1 },
    'power-line': { width: 100, height: 5, fillColor: '#8b5cf6', strokeColor: '#7c3aed', strokeWidth: 2, opacity: 1 },
    'generator': { width: 70, height: 70, fillColor: '#ec4899', strokeColor: '#db2777', strokeWidth: 2, opacity: 1 },
    'load': { width: 50, height: 50, fillColor: '#6366f1', strokeColor: '#4f46e5', strokeWidth: 2, opacity: 1 },
    'capacitor': { width: 40, height: 60, fillColor: '#14b8a6', strokeColor: '#0d9488', strokeWidth: 2, opacity: 1 },
    'reactor': { width: 40, height: 60, fillColor: '#f97316', strokeColor: '#ea580c', strokeWidth: 2, opacity: 1 },
    'voltage-transformer': { width: 50, height: 50, fillColor: '#84cc16', strokeColor: '#65a30d', strokeWidth: 2, opacity: 1 },
    'current-transformer': { width: 50, height: 50, fillColor: '#06b6d4', strokeColor: '#0891b2', strokeWidth: 2, opacity: 1 },
    'surge-arrester': { width: 30, height: 50, fillColor: '#a855f7', strokeColor: '#9333ea', strokeWidth: 2, opacity: 1 },
    'pump': { width: 60, height: 60, fillColor: '#3b82f6', strokeColor: '#2563eb', strokeWidth: 2, opacity: 1 },
    'valve': { width: 40, height: 40, fillColor: '#10b981', strokeColor: '#059669', strokeWidth: 2, opacity: 1 },
    'tank': { width: 80, height: 100, fillColor: '#64748b', strokeColor: '#475569', strokeWidth: 2, opacity: 1 },
    'pipe': { width: 100, height: 10, fillColor: '#94a3b8', strokeColor: '#64748b', strokeWidth: 2, opacity: 1 },
    'heat-exchanger': { width: 70, height: 70, fillColor: '#f59e0b', strokeColor: '#d97706', strokeWidth: 2, opacity: 1 },
    'compressor': { width: 60, height: 60, fillColor: '#8b5cf6', strokeColor: '#7c3aed', strokeWidth: 2, opacity: 1 },
    'turbine': { width: 70, height: 70, fillColor: '#06b6d4', strokeColor: '#0891b2', strokeWidth: 2, opacity: 1 },
    'boiler': { width: 80, height: 100, fillColor: '#ef4444', strokeColor: '#dc2626', strokeWidth: 2, opacity: 1 },
    'gauge': { width: 60, height: 60, fillColor: '#4ade80', strokeColor: '#22c55e', strokeWidth: 2, opacity: 1 },
    'indicator': { width: 30, height: 30, fillColor: '#fbbf24', strokeColor: '#f59e0b', strokeWidth: 2, opacity: 1 },
    'text-label': { width: 100, height: 30, fillColor: 'transparent', strokeColor: '#ffffff', strokeWidth: 1, opacity: 1 },
    'alarm-indicator': { width: 40, height: 40, fillColor: '#ef4444', strokeColor: '#dc2626', strokeWidth: 2, opacity: 1, animated: true },
  };

  return defaults[type] || { width: 50, height: 50, fillColor: '#64748b', strokeColor: '#475569', strokeWidth: 2, opacity: 1 };
}

// Helper function to get color based on value
function getColorForValue(value: number): string {
  if (value < 30) return '#22c55e'; // Green
  if (value < 70) return '#f59e0b'; // Orange
  return '#ef4444'; // Red
}
