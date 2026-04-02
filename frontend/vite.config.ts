import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Vite dev server configuration.
// The 'proxy' setting tells Vite: "any request starting with /api or /hubs,
// forward it to the ASP.NET Core backend instead of trying to serve it as a file."
// This means the frontend can use relative URLs like '/api/polls' and they'll
// reach the backend automatically during development.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        secure: false,       // accept self-signed certs from ASP.NET Core dev server
        changeOrigin: true
      },
      '/hubs': {
        target: 'https://localhost:5001',
        secure: false,
        ws: true             // enable WebSocket proxying — required for SignalR
      }
    }
  }
})
