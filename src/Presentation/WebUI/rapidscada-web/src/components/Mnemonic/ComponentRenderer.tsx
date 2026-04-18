import React from 'react';
import { Group, Rect, Circle, Line, Path, Text } from 'react-konva';
import { MnemoComponent } from '../types/mnemonic';

interface ComponentRendererProps {
  component: MnemoComponent;
  isSelected: boolean;
  onSelect: () => void;
  onDragEnd: (e: any) => void;
}

export const ComponentRenderer: React.FC<ComponentRendererProps> = ({
  component,
  isSelected,
  onSelect,
  onDragEnd,
}) => {
  const renderComponent = () => {
    switch (component.type) {
      case 'transformer':
        return <TransformerComponent component={component} />;
      case 'circuit-breaker':
        return <CircuitBreakerComponent component={component} />;
      case 'disconnector':
        return <DisconnectorComponent component={component} />;
      case 'bus-bar':
        return <BusBarComponent component={component} />;
      case 'generator':
        return <GeneratorComponent component={component} />;
      case 'pump':
        return <PumpComponent component={component} />;
      case 'valve':
        return <ValveComponent component={component} />;
      case 'tank':
        return <TankComponent component={component} />;
      case 'pipe':
        return <PipeComponent component={component} />;
      case 'gauge':
        return <GaugeComponent component={component} />;
      case 'indicator':
        return <IndicatorComponent component={component} />;
      case 'text-label':
        return <TextLabelComponent component={component} />;
      case 'alarm-indicator':
        return <AlarmIndicatorComponent component={component} />;
      default:
        return <DefaultComponent component={component} />;
    }
  };

  return (
    <Group
      x={component.x}
      y={component.y}
      rotation={component.rotation}
      scaleX={component.scaleX}
      scaleY={component.scaleY}
      draggable
      onClick={onSelect}
      onTap={onSelect}
      onDragEnd={onDragEnd}
      opacity={component.opacity}
    >
      {renderComponent()}
      {isSelected && (
        <Rect
          x={-5}
          y={-5}
          width={component.width + 10}
          height={component.height + 10}
          stroke="#00ffff"
          strokeWidth={2}
          dash={[5, 5]}
        />
      )}
    </Group>
  );
};

// Transformer Component
const TransformerComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <>
    {/* Primary winding */}
    <Circle
      x={component.width / 2}
      y={component.height * 0.3}
      radius={component.width * 0.35}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
      fill="transparent"
    />
    {/* Secondary winding */}
    <Circle
      x={component.width / 2}
      y={component.height * 0.7}
      radius={component.width * 0.35}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
      fill="transparent"
    />
    {/* Core */}
    <Line
      points={[
        component.width * 0.2, 0,
        component.width * 0.2, component.height,
      ]}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
    />
    <Line
      points={[
        component.width * 0.8, 0,
        component.width * 0.8, component.height,
      ]}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
    />
    {/* Connection points */}
    <Circle x={component.width / 2} y={0} radius={3} fill={component.fillColor} />
    <Circle x={component.width / 2} y={component.height} radius={3} fill={component.fillColor} />
  </>
);

// Circuit Breaker Component
const CircuitBreakerComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isOpen = component.state === 'offline';
  const rotation = isOpen ? 45 : 0;

  return (
    <>
      {/* Frame */}
      <Rect
        x={0}
        y={0}
        width={component.width}
        height={component.height}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
        fill="transparent"
        cornerRadius={5}
      />
      {/* Contacts */}
      <Line
        points={[
          component.width / 2, component.height * 0.2,
          component.width / 2, component.height * 0.45,
        ]}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth * 2}
      />
      <Group x={component.width / 2} y={component.height * 0.5} rotation={rotation}>
        <Line
          points={[0, 0, 0, component.height * 0.3]}
          stroke={isOpen ? '#ef4444' : '#22c55e'}
          strokeWidth={component.strokeWidth * 2}
        />
      </Group>
      <Line
        points={[
          component.width / 2, component.height * 0.8,
          component.width / 2, component.height,
        ]}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth * 2}
      />
      {/* Status indicator */}
      <Circle
        x={component.width * 0.85}
        y={component.height * 0.15}
        radius={5}
        fill={isOpen ? '#ef4444' : '#22c55e'}
      />
    </>
  );
};

// Disconnector Component
const DisconnectorComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isOpen = component.state === 'offline';

  return (
    <>
      {/* Terminals */}
      <Circle x={component.width * 0.5} y={component.height * 0.2} radius={4} fill={component.strokeColor} />
      <Circle x={component.width * 0.5} y={component.height * 0.8} radius={4} fill={component.strokeColor} />
      
      {/* Blade */}
      <Line
        points={[
          component.width * 0.5, component.height * 0.2,
          component.width * (isOpen ? 0.8 : 0.5), component.height * (isOpen ? 0.5 : 0.8),
        ]}
        stroke={isOpen ? '#ef4444' : component.fillColor}
        strokeWidth={component.strokeWidth * 1.5}
        lineCap="round"
      />
      
      {/* Insulator */}
      <Line
        points={[
          component.width * (isOpen ? 0.8 : 0.5), component.height * (isOpen ? 0.5 : 0.8),
          component.width * (isOpen ? 0.85 : 0.5), component.height * (isOpen ? 0.6 : 0.8),
        ]}
        stroke="#94a3b8"
        strokeWidth={component.strokeWidth * 2}
      />
    </>
  );
};

// Bus Bar Component
const BusBarComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <>
    <Rect
      x={0}
      y={0}
      width={component.width}
      height={component.height}
      fill={component.fillColor}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
      cornerRadius={component.height / 4}
    />
    {/* Connection points */}
    {[0.25, 0.5, 0.75].map((pos, i) => (
      <Circle
        key={i}
        x={component.width * pos}
        y={component.height / 2}
        radius={3}
        fill="#ffffff"
      />
    ))}
  </>
);

// Generator Component
const GeneratorComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <>
    <Circle
      x={component.width / 2}
      y={component.height / 2}
      radius={component.width * 0.4}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
      fill={component.state === 'normal' ? component.fillColor : '#64748b'}
    />
    <Text
      x={component.width * 0.35}
      y={component.height * 0.42}
      text="G"
      fontSize={component.width * 0.4}
      fill={component.strokeColor}
      fontStyle="bold"
    />
    {/* Connection point */}
    <Circle x={component.width / 2} y={0} radius={3} fill={component.fillColor} />
  </>
);

// Pump Component
const PumpComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isRunning = component.state === 'normal';

  return (
    <>
      {/* Casing */}
      <Circle
        x={component.width / 2}
        y={component.height / 2}
        radius={component.width * 0.45}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
        fill={component.fillColor}
      />
      {/* Impeller (animated if running) */}
      <Group x={component.width / 2} y={component.height / 2}>
        {[0, 120, 240].map((angle) => (
          <Line
            key={angle}
            points={[0, 0, component.width * 0.3 * Math.cos((angle * Math.PI) / 180), component.width * 0.3 * Math.sin((angle * Math.PI) / 180)]}
            stroke={isRunning ? '#22c55e' : component.strokeColor}
            strokeWidth={component.strokeWidth * 1.5}
          />
        ))}
      </Group>
      {/* Status indicator */}
      <Circle
        x={component.width * 0.85}
        y={component.height * 0.15}
        radius={5}
        fill={isRunning ? '#22c55e' : '#ef4444'}
      />
    </>
  );
};

// Valve Component
const ValveComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isOpen = component.state === 'normal';

  return (
    <>
      {/* Body */}
      <Path
        data={`M ${component.width * 0.5} ${component.height * 0.1} 
               L ${component.width * 0.9} ${component.height * 0.5} 
               L ${component.width * 0.5} ${component.height * 0.9} 
               L ${component.width * 0.1} ${component.height * 0.5} Z`}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
        fill={isOpen ? component.fillColor : '#64748b'}
      />
      {/* Stem */}
      <Line
        points={[component.width * 0.5, component.height * 0.1, component.width * 0.5, 0]}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
      />
      {/* Status */}
      <Text
        x={component.width * 0.35}
        y={component.height * 0.42}
        text={isOpen ? 'O' : 'C'}
        fontSize={component.width * 0.3}
        fill="#ffffff"
        fontStyle="bold"
      />
    </>
  );
};

// Tank Component
const TankComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const level = component.properties?.value || 50; // 0-100

  return (
    <>
      {/* Tank body */}
      <Rect
        x={0}
        y={0}
        width={component.width}
        height={component.height}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
        fill="transparent"
        cornerRadius={5}
      />
      {/* Liquid level */}
      <Rect
        x={2}
        y={component.height * (1 - level / 100)}
        width={component.width - 4}
        height={component.height * (level / 100) - 2}
        fill={component.fillColor}
        opacity={0.7}
      />
      {/* Level markers */}
      {[0, 25, 50, 75, 100].map((mark) => (
        <Line
          key={mark}
          points={[
            0, component.height * (1 - mark / 100),
            10, component.height * (1 - mark / 100),
          ]}
          stroke={component.strokeColor}
          strokeWidth={1}
        />
      ))}
      {/* Level text */}
      <Text
        x={component.width / 2 - 15}
        y={component.height * 0.45}
        text={`${level.toFixed(0)}%`}
        fontSize={16}
        fill="#ffffff"
        fontStyle="bold"
      />
    </>
  );
};

// Pipe Component
const PipeComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <>
    <Rect
      x={0}
      y={0}
      width={component.width}
      height={component.height}
      fill={component.fillColor}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
    />
    {/* Flow direction indicator */}
    {component.animated && (
      <>
        {[0.2, 0.5, 0.8].map((pos, i) => (
          <Path
            key={i}
            data={`M ${component.width * pos} ${component.height * 0.3} 
                   L ${component.width * pos + 5} ${component.height * 0.5} 
                   L ${component.width * pos} ${component.height * 0.7} Z`}
            fill="#ffffff"
          />
        ))}
      </>
    )}
  </>
);

// Gauge Component
const GaugeComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const value = component.properties?.value || 0;
  const angle = -90 + (value / 100) * 180;

  return (
    <>
      {/* Dial */}
      <Circle
        x={component.width / 2}
        y={component.height / 2}
        radius={component.width * 0.45}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
        fill={component.fillColor}
      />
      {/* Tick marks */}
      {[0, 30, 60, 90, 120, 150, 180].map((tick) => {
        const tickAngle = -90 + tick;
        const radians = (tickAngle * Math.PI) / 180;
        const innerRadius = component.width * 0.35;
        const outerRadius = component.width * 0.42;
        return (
          <Line
            key={tick}
            points={[
              component.width / 2 + innerRadius * Math.cos(radians),
              component.height / 2 + innerRadius * Math.sin(radians),
              component.width / 2 + outerRadius * Math.cos(radians),
              component.height / 2 + outerRadius * Math.sin(radians),
            ]}
            stroke="#ffffff"
            strokeWidth={2}
          />
        );
      })}
      {/* Needle */}
      <Line
        points={[
          component.width / 2,
          component.height / 2,
          component.width / 2 + component.width * 0.35 * Math.cos((angle * Math.PI) / 180),
          component.height / 2 + component.width * 0.35 * Math.sin((angle * Math.PI) / 180),
        ]}
        stroke="#ef4444"
        strokeWidth={3}
      />
      {/* Center dot */}
      <Circle x={component.width / 2} y={component.height / 2} radius={4} fill="#ef4444" />
      {/* Value text */}
      <Text
        x={component.width * 0.3}
        y={component.height * 0.75}
        text={value.toFixed(1)}
        fontSize={14}
        fill="#ffffff"
        fontStyle="bold"
      />
    </>
  );
};

// Indicator Component
const IndicatorComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isActive = component.state === 'normal';

  return (
    <Circle
      x={component.width / 2}
      y={component.height / 2}
      radius={component.width / 2}
      fill={isActive ? component.fillColor : '#64748b'}
      stroke={component.strokeColor}
      strokeWidth={component.strokeWidth}
    />
  );
};

// Text Label Component
const TextLabelComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <Text
    x={0}
    y={0}
    text={component.properties?.text || component.name}
    fontSize={component.properties?.fontSize || 16}
    fill={component.strokeColor}
    fontStyle={component.properties?.bold ? 'bold' : 'normal'}
    width={component.width}
    align="center"
  />
);

// Alarm Indicator Component
const AlarmIndicatorComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => {
  const isAlarming = component.state === 'alarm';

  return (
    <>
      <Circle
        x={component.width / 2}
        y={component.height / 2}
        radius={component.width * 0.4}
        fill={isAlarming ? '#ef4444' : '#64748b'}
        stroke={component.strokeColor}
        strokeWidth={component.strokeWidth}
      />
      <Text
        x={component.width * 0.35}
        y={component.height * 0.35}
        text="!"
        fontSize={component.width * 0.5}
        fill="#ffffff"
        fontStyle="bold"
      />
    </>
  );
};

// Default fallback component
const DefaultComponent: React.FC<{ component: MnemoComponent }> = ({ component }) => (
  <Rect
    x={0}
    y={0}
    width={component.width}
    height={component.height}
    fill={component.fillColor}
    stroke={component.strokeColor}
    strokeWidth={component.strokeWidth}
    cornerRadius={5}
  />
);
