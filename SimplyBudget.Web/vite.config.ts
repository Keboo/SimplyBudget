import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

const backendUrl =
  process.env.SIMPLYBUDGET_API_HTTP ||
  process.env['services__simplybudget-api__http__0'] ||
  process.env.SIMPLYBUDGET_API_HTTPS ||
  process.env['services__simplybudget-api__https__0'] ||
  'https://localhost:7001'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'SimplyBudget',
        short_name: 'SimplyBudget',
        theme_color: '#1976d2',
        icons: [
          { src: '/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icon-512.png', sizes: '512x512', type: 'image/png' },
        ]
      }
    })
  ],
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    proxy: {
      '/api': {
        target: backendUrl,
        changeOrigin: true,
        secure: false
      },
      '/health': {
        target: backendUrl,
        changeOrigin: true,
        secure: false
      }
    }
  }
})
