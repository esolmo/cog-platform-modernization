import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5175,
    watch: {
      usePolling: true,
      interval: 1000,
    },
    proxy: {
      '/api/auth': {
        target: 'http://localhost:5010',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5030',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    pool: 'forks',
    exclude: ['**/node_modules/**', '**/e2e/**'],
  },
});
