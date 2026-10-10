import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import path from 'path'

export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true })],
  resolve: {
    alias: { '@': path.resolve(import.meta.dirname, './src') },
  },
  define: {
    __API_BASE_URL__: JSON.stringify('http://api.test'),
    __APPLICATIONINSIGHTS_CONNECTION_STRING__: JSON.stringify(''),
    __ENTRA_CLIENT_ID__: JSON.stringify('client-id'),
    __ENTRA_TENANT_ID__: JSON.stringify('tenant-id'),
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.ts'],
    css: false,
    setupFiles: ['./vitest.setup.ts'],
    server: { deps: { inline: ['vuetify'] } },
  },
})
