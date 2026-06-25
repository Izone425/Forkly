import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Vite config for the Forkly frontend (Vue 3 SFC).
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    open: true,
  },
})
