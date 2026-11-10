
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],

  server: {
    proxy: {
      '/classes': {
        target: 'http://localhost:5270',
        changeOrigin: true,
      },
      '/holds': {
        target: 'http://localhost:5270',
        changeOrigin: true,
      },
      '/me': {
        target: 'http://localhost:5270',
        changeOrigin: true,
      },
    },
  },

  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/setupTests.js',
    css: true,
  },
})
