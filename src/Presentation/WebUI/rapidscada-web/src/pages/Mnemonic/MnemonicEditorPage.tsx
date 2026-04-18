import React, { useState, useRef, useEffect } from 'react';
import { Stage, Layer, Line as KonvaLine, Rect as KonvaRect } from 'react-konva';
import { useTranslation } from 'react-i18next';
import {
  Play,
  Pause,
  Save,
  FolderOpen,
  Undo2,
  Redo2,
  ZoomIn,
  ZoomOut,
  Grid3x3,
  Layers,
  Settings,
  Copy,
  Scissors,
  Clipboard,
  Trash2,
  Move,
  MousePointer2,
  AlertCircle,
} from 'lucide-react';
import { useMnemonicStore } from '../../stores/mnemonicStore';
import { ComponentRenderer } from '../../components/Mnemonic/ComponentRenderer';
import { ComponentType } from '../../types/mnemonic';

const TOOLBAR_HEIGHT = 60;
const PROPERTIES_WIDTH = 300;

// Component icons mapping
const componentIcons = {
  'transformer': '⚡',
  'circuit-breaker': '🔌',
  'disconnector': '🔗',
  'bus-bar': '━',
  'generator': '⚙️',
  'pump': '♻️',
  'valve': '🎚️',
  'tank': '🏺',
  'pipe': '━',
  'gauge': '📊',
  'indicator': '💡',
  'text-label': 'T',
  'alarm-indicator': '🚨',
};

export default function MnemonicEditorPage() {
  const { t } = useTranslation();
  const stageRef = useRef<any>(null);
  const [windowSize, setWindowSize] = useState({ width: window.innerWidth, height: window.innerHeight });

  const {
    mode,
    currentScheme,
    selectedTool,
    selectedComponents,
    zoom,
    panX,
    panY,
    showGrid,
    snapToGrid,
    setMode,
    setSelectedTool,
    addComponent,
    updateComponent,
    deleteComponent,
    selectComponent,
    clearSelection,
    copy,
    paste,
    cut,
    undo,
    redo,
    setZoom,
    setPan,
    toggleGrid,
    createScheme,
    saveScheme,
  } = useMnemonicStore();

  useEffect(() => {
    const handleResize = () => {
      setWindowSize({ width: window.innerWidth, height: window.innerHeight });
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  useEffect(() => {
    if (!currentScheme) {
      createScheme('Новая схема');
    }
  }, []);

  const handleCanvasClick = (e: any) => {
    if (e.target === e.target.getStage()) {
      clearSelection();
    }
  };

  const handleStageClick = (e: any) => {
    if (selectedTool !== 'select' && selectedTool !== 'pan' && selectedTool !== 'connect') {
      const stage = e.target.getStage();
      const pointerPosition = stage.getPointerPosition();
      
      if (pointerPosition) {
        const x = (pointerPosition.x - panX) / zoom;
        const y = (pointerPosition.y - panY) / zoom;
        
        addComponent(selectedTool as ComponentType, x, y);
        setSelectedTool('select');
      }
    }
  };

  const handleWheel = (e: any) => {
    e.evt.preventDefault();
    
    const scaleBy = 1.05;
    const stage = e.target.getStage();
    const oldScale = stage.scaleX();
    const pointer = stage.getPointerPosition();
    
    const newScale = e.evt.deltaY < 0 ? oldScale * scaleBy : oldScale / scaleBy;
    
    setZoom(Math.max(0.1, Math.min(5, newScale)));
  };

  const handleKeyPress = (e: KeyboardEvent) => {
    if (e.ctrlKey || e.metaKey) {
      switch (e.key.toLowerCase()) {
        case 'c':
          e.preventDefault();
          copy();
          break;
        case 'v':
          e.preventDefault();
          paste();
          break;
        case 'x':
          e.preventDefault();
          cut();
          break;
        case 'z':
          e.preventDefault();
          if (e.shiftKey) {
            redo();
          } else {
            undo();
          }
          break;
        case 's':
          e.preventDefault();
          saveScheme();
          break;
      }
    } else if (e.key === 'Delete' || e.key === 'Backspace') {
      e.preventDefault();
      selectedComponents.forEach(id => deleteComponent(id));
    }
  };

  useEffect(() => {
    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, [selectedComponents]);

  if (!currentScheme) return <div>Loading...</div>;

  const canvasWidth = windowSize.width - PROPERTIES_WIDTH;
  const canvasHeight = windowSize.height - TOOLBAR_HEIGHT - 64;

  return (
    <div className="flex flex-col h-screen bg-gray-900">
      {/* Top Toolbar */}
      <div className="h-16 bg-gray-800 border-b border-gray-700 flex items-center justify-between px-4">
        <div className="flex items-center gap-2">
          <h1 className="text-xl font-bold text-gray-100 mr-4">
            {currentScheme.name}
            {currentScheme.activeAlarms > 0 && (
              <span className="ml-2 px-2 py-1 bg-red-600 text-white text-xs rounded-full">
                {currentScheme.activeAlarms} тревог
              </span>
            )}
          </h1>
          
          <button
            onClick={() => setMode(mode === 'edit' ? 'display' : 'edit')}
            className={`p-2 rounded ${mode === 'display' ? 'bg-green-600' : 'bg-gray-700'} hover:bg-opacity-80`}
            title={mode === 'edit' ? 'Режим отображения' : 'Режим редактирования'}
          >
            {mode === 'display' ? <Play className="w-5 h-5" /> : <Pause className="w-5 h-5" />}
          </button>

          <div className="h-6 w-px bg-gray-600 mx-2" />

          <button onClick={saveScheme} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Сохранить">
            <Save className="w-5 h-5" />
          </button>
          <button className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Открыть">
            <FolderOpen className="w-5 h-5" />
          </button>

          <div className="h-6 w-px bg-gray-600 mx-2" />

          <button onClick={undo} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Отменить">
            <Undo2 className="w-5 h-5" />
          </button>
          <button onClick={redo} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Вернуть">
            <Redo2 className="w-5 h-5" />
          </button>

          <div className="h-6 w-px bg-gray-600 mx-2" />

          <button onClick={copy} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Копировать">
            <Copy className="w-5 h-5" />
          </button>
          <button onClick={paste} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Вставить">
            <Clipboard className="w-5 h-5" />
          </button>
          <button onClick={cut} className="p-2 rounded bg-gray-700 hover:bg-gray-600" title="Вырезать">
            <Scissors className="w-5 h-5" />
          </button>
          <button
            onClick={() => selectedComponents.forEach(id => deleteComponent(id))}
            className="p-2 rounded bg-gray-700 hover:bg-gray-600"
            title="Удалить"
          >
            <Trash2 className="w-5 h-5" />
          </button>
        </div>

        <div className="flex items-center gap-2">
          <button onClick={() => setZoom(zoom * 0.9)} className="p-2 rounded bg-gray-700 hover:bg-gray-600">
            <ZoomOut className="w-5 h-5" />
          </button>
          <span className="text-gray-300 w-16 text-center">{Math.round(zoom * 100)}%</span>
          <button onClick={() => setZoom(zoom * 1.1)} className="p-2 rounded bg-gray-700 hover:bg-gray-600">
            <ZoomIn className="w-5 h-5" />
          </button>

          <div className="h-6 w-px bg-gray-600 mx-2" />

          <button
            onClick={toggleGrid}
            className={`p-2 rounded ${showGrid ? 'bg-blue-600' : 'bg-gray-700'} hover:bg-opacity-80`}
            title="Сетка"
          >
            <Grid3x3 className="w-5 h-5" />
          </button>
        </div>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Component Toolbar */}
        <ComponentToolbar selectedTool={selectedTool} onSelectTool={setSelectedTool} />

        {/* Canvas */}
        <div className="flex-1 bg-gray-950 relative overflow-hidden">
          <Stage
            ref={stageRef}
            width={canvasWidth}
            height={canvasHeight}
            scaleX={zoom}
            scaleY={zoom}
            x={panX}
            y={panY}
            onClick={handleStageClick}
            onWheel={handleWheel}
            draggable={selectedTool === 'pan'}
            onDragEnd={(e) => setPan(e.target.x(), e.target.y())}
          >
            <Layer>
              {/* Grid */}
              {showGrid && <GridLayer width={canvasWidth} height={canvasHeight} zoom={zoom} />}

              {/* Connections */}
              {currentScheme.connections.map((conn) => (
                <KonvaLine
                  key={conn.id}
                  points={conn.points}
                  stroke={conn.strokeColor}
                  strokeWidth={conn.strokeWidth}
                  dash={conn.animated ? [10, 5] : []}
                />
              ))}

              {/* Components */}
              {currentScheme.components.map((component) => (
                <ComponentRenderer
                  key={component.id}
                  component={component}
                  isSelected={selectedComponents.includes(component.id)}
                  onSelect={() => selectComponent(component.id)}
                  onDragEnd={(e) => {
                    let x = e.target.x();
                    let y = e.target.y();
                    
                    if (snapToGrid) {
                      const gridSize = currentScheme.gridSize;
                      x = Math.round(x / gridSize) * gridSize;
                      y = Math.round(y / gridSize) * gridSize;
                    }
                    
                    updateComponent(component.id, { x, y });
                  }}
                />
              ))}
            </Layer>
          </Stage>

          {/* Mode indicator */}
          <div className="absolute top-4 right-4 px-4 py-2 bg-gray-800 rounded-lg border border-gray-700">
            <span className="text-gray-300">
              {mode === 'display' ? '🎬 Режим отображения' : '✏️ Режим редактирования'}
            </span>
          </div>
        </div>

        {/* Properties Panel */}
        <PropertiesPanel />
      </div>
    </div>
  );
}

// Component Toolbar
function ComponentToolbar({ selectedTool, onSelectTool }: { selectedTool: string; onSelectTool: (tool: any) => void }) {
  const categories = [
    {
      name: 'Инструменты',
      tools: [
        { id: 'select', icon: <MousePointer2 className="w-4 h-4" />, label: 'Выбор' },
        { id: 'pan', icon: <Move className="w-4 h-4" />, label: 'Панорама' },
      ],
    },
    {
      name: 'Электрооборудование',
      tools: [
        { id: 'transformer', icon: componentIcons.transformer, label: 'Трансформатор' },
        { id: 'circuit-breaker', icon: componentIcons['circuit-breaker'], label: 'Выключатель' },
        { id: 'disconnector', icon: componentIcons.disconnector, label: 'Разъединитель' },
        { id: 'bus-bar', icon: componentIcons['bus-bar'], label: 'Шина' },
        { id: 'generator', icon: componentIcons.generator, label: 'Генератор' },
      ],
    },
    {
      name: 'Энергетика',
      tools: [
        { id: 'pump', icon: componentIcons.pump, label: 'Насос' },
        { id: 'valve', icon: componentIcons.valve, label: 'Задвижка' },
        { id: 'tank', icon: componentIcons.tank, label: 'Резервуар' },
        { id: 'pipe', icon: componentIcons.pipe, label: 'Трубопровод' },
      ],
    },
    {
      name: 'Индикаторы',
      tools: [
        { id: 'gauge', icon: componentIcons.gauge, label: 'Измеритель' },
        { id: 'indicator', icon: componentIcons.indicator, label: 'Индикатор' },
        { id: 'alarm-indicator', icon: componentIcons['alarm-indicator'], label: 'Тревога' },
        { id: 'text-label', icon: componentIcons['text-label'], label: 'Текст' },
      ],
    },
  ];

  return (
    <div className="w-20 bg-gray-800 border-r border-gray-700 overflow-y-auto">
      {categories.map((category) => (
        <div key={category.name} className="py-2">
          <div className="px-2 text-xs text-gray-500 uppercase mb-2">{category.name}</div>
          {category.tools.map((tool) => (
            <button
              key={tool.id}
              onClick={() => onSelectTool(tool.id)}
              className={`w-full p-3 flex flex-col items-center gap-1 ${
                selectedTool === tool.id ? 'bg-blue-600' : 'hover:bg-gray-700'
              }`}
              title={tool.label}
            >
              {typeof tool.icon === 'string' ? (
                <span className="text-2xl">{tool.icon}</span>
              ) : (
                tool.icon
              )}
              <span className="text-xs text-gray-300 text-center leading-tight">{tool.label}</span>
            </button>
          ))}
        </div>
      ))}
    </div>
  );
}

// Properties Panel
function PropertiesPanel() {
  const { currentScheme, selectedComponents, updateComponent } = useMnemonicStore();

  if (selectedComponents.length === 0) {
    return (
      <div className="w-80 bg-gray-800 border-l border-gray-700 p-4">
        <h3 className="text-lg font-bold text-gray-100 mb-4">Свойства</h3>
        <p className="text-gray-400">Выберите компонент для редактирования</p>
      </div>
    );
  }

  const component = currentScheme?.components.find((c) => c.id === selectedComponents[0]);

  if (!component) return null;

  return (
    <div className="w-80 bg-gray-800 border-l border-gray-700 p-4 overflow-y-auto">
      <h3 className="text-lg font-bold text-gray-100 mb-4">Свойства компонента</h3>

      <div className="space-y-4">
        <div>
          <label className="block text-sm text-gray-300 mb-1">Имя</label>
          <input
            type="text"
            value={component.name}
            onChange={(e) => updateComponent(component.id, { name: e.target.value })}
            className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
          />
        </div>

        <div>
          <label className="block text-sm text-gray-300 mb-1">Тип</label>
          <input
            type="text"
            value={component.type}
            disabled
            className="w-full px-3 py-2 bg-gray-900 text-gray-400 rounded border border-gray-600"
          />
        </div>

        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-sm text-gray-300 mb-1">X</label>
            <input
              type="number"
              value={Math.round(component.x)}
              onChange={(e) => updateComponent(component.id, { x: Number(e.target.value) })}
              className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
            />
          </div>
          <div>
            <label className="block text-sm text-gray-300 mb-1">Y</label>
            <input
              type="number"
              value={Math.round(component.y)}
              onChange={(e) => updateComponent(component.id, { y: Number(e.target.value) })}
              className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-sm text-gray-300 mb-1">Ширина</label>
            <input
              type="number"
              value={component.width}
              onChange={(e) => updateComponent(component.id, { width: Number(e.target.value) })}
              className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
            />
          </div>
          <div>
            <label className="block text-sm text-gray-300 mb-1">Высота</label>
            <input
              type="number"
              value={component.height}
              onChange={(e) => updateComponent(component.id, { height: Number(e.target.value) })}
              className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm text-gray-300 mb-1">Поворот (°)</label>
          <input
            type="number"
            value={component.rotation}
            onChange={(e) => updateComponent(component.id, { rotation: Number(e.target.value) })}
            className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
          />
        </div>

        <div>
          <label className="block text-sm text-gray-300 mb-1">Цвет заливки</label>
          <input
            type="color"
            value={component.fillColor}
            onChange={(e) => updateComponent(component.id, { fillColor: e.target.value })}
            className="w-full h-10 bg-gray-700 rounded border border-gray-600"
          />
        </div>

        <div>
          <label className="block text-sm text-gray-300 mb-1">Цвет обводки</label>
          <input
            type="color"
            value={component.strokeColor}
            onChange={(e) => updateComponent(component.id, { strokeColor: e.target.value })}
            className="w-full h-10 bg-gray-700 rounded border border-gray-600"
          />
        </div>

        <div>
          <label className="block text-sm text-gray-300 mb-1">Состояние</label>
          <select
            value={component.state}
            onChange={(e) => updateComponent(component.id, { state: e.target.value as any })}
            className="w-full px-3 py-2 bg-gray-700 text-gray-100 rounded border border-gray-600"
          >
            <option value="normal">Норма</option>
            <option value="warning">Предупреждение</option>
            <option value="alarm">Тревога</option>
            <option value="offline">Офлайн</option>
            <option value="maintenance">Обслуживание</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={component.animated}
            onChange={(e) => updateComponent(component.id, { animated: e.target.checked })}
            className="w-4 h-4"
          />
          <label className="text-sm text-gray-300">Анимация</label>
        </div>
      </div>
    </div>
  );
}

// Grid Layer
function GridLayer({ width, height, zoom }: { width: number; height: number; zoom: number }) {
  const gridSize = 20;
  const lines = [];

  for (let i = 0; i < width / zoom; i += gridSize) {
    lines.push(
      <KonvaLine
        key={`v-${i}`}
        points={[i, 0, i, height / zoom]}
        stroke="#374151"
        strokeWidth={0.5}
      />
    );
  }

  for (let i = 0; i < height / zoom; i += gridSize) {
    lines.push(
      <KonvaLine
        key={`h-${i}`}
        points={[0, i, width / zoom, i]}
        stroke="#374151"
        strokeWidth={0.5}
      />
    );
  }

  return <>{lines}</>;
}
