import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '../stores/authStore';
import toast from 'react-hot-toast';

class SignalRService {
  private connection: signalR.HubConnection;
  private callbacks: Map<string, Function[]> = new Map();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/scadahub', {
        accessTokenFactory: () => useAuthStore.getState().token || '',
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    // Tag value updates
    this.connection.on('TagValuesUpdated', (updates) => {
      console.log('Tag values updated:', updates);
      this.trigger('tagValuesUpdated', updates);
    });

    // Alarm triggered
    this.connection.on('AlarmTriggered', (alarm) => {
      toast.error(`🚨 ${alarm.message}`, {
        duration: 10000,
        style: {
          background: '#7f1d1d',
          color: '#fff',
        },
      });
      this.trigger('alarmTriggered', alarm);
    });

    // Device status updated
    this.connection.on('DeviceStatusUpdated', (status) => {
      this.trigger('deviceStatusUpdated', status);
    });

    // System broadcast message
    this.connection.on('SystemMessage', (message) => {
      toast(message.message, {
        icon: '📢',
      });
    });

    // Connection state changes
    this.connection.onreconnecting(() => {
      toast.loading('Reconnecting to server...', { id: 'reconnect' });
    });

    this.connection.onreconnected(() => {
      toast.success('Reconnected!', { id: 'reconnect' });
    });

    this.connection.onclose(() => {
      toast.error('Connection lost', { id: 'reconnect' });
    });
  }

  async start() {
    try {
      await this.connection.start();
      console.log('SignalR Connected');
      return true;
    } catch (err) {
      console.error('SignalR Connection Error:', err);
      // Retry after 5 seconds
      setTimeout(() => this.start(), 5000);
      return false;
    }
  }

  async stop() {
    await this.connection.stop();
  }

  async subscribeToTags(tagIds: number[]) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToTags', tagIds);
    }
  }

  async unsubscribeFromTags(tagIds: number[]) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeFromTags', tagIds);
    }
  }

  async subscribeToDevice(deviceId: number) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToDevice', deviceId);
    }
  }

  async unsubscribeFromDevice(deviceId: number) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeFromDevice', deviceId);
    }
  }

  async broadcastSystemMessage(message: string) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('BroadcastSystemMessage', message);
    }
  }

  // Event subscription
  on(event: string, callback: Function) {
    if (!this.callbacks.has(event)) {
      this.callbacks.set(event, []);
    }
    this.callbacks.get(event)!.push(callback);
  }

  off(event: string, callback: Function) {
    const callbacks = this.callbacks.get(event);
    if (callbacks) {
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    }
  }

  private trigger(event: string, data: any) {
    const callbacks = this.callbacks.get(event);
    if (callbacks) {
      callbacks.forEach((callback) => callback(data));
    }
  }

  get connectionState() {
    return this.connection.state;
  }

  get isConnected() {
    return this.connection.state === signalR.HubConnectionState.Connected;
  }
}

export const signalrService = new SignalRService();
