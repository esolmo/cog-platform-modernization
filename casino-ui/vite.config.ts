'use strict'

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5178,
    watch: {
      usePolling: true,
      interval: 1000,
    },
    proxy: {
      '/api/auth': { target: 'http://localhost:5010', changeOrigin: true },
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
  test: {
    globals: true,
    environment: 'happy-dom',
    setupFiles: ['./src/test/setup.ts'],
    pool: 'forks',
  },
})
