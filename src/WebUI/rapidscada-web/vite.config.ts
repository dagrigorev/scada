import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      // Identity Service (Auth, Users) → Port 5003
      '/api/identity': {
        target: 'https://localhost:5003',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api\/identity/, '/api'),
      },
      
      // Realtime Service (SignalR) → Port 5005
      '/api/realtime': {
        target: 'https://localhost:5005',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api\/realtime/, '/api'),
      },
      
      // Communicator Service (Device Polling) → Port 5007
      '/api/communicator': {
        target: 'https://localhost:5007',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api\/communicator/, '/api'),
      },
      
      // Archiver Service (Historical Data) → Port 5009
      '/api/archiver': {
        target: 'https://localhost:5009',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api\/archiver/, '/api'),
      },
      
      // WebAPI (Main BFF - Devices, Tags, Alarms) → Port 5001
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
      
      // SignalR Hub WebSocket → Port 5005
      '/scadahub': {
        target: 'wss://localhost:5005',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
})
