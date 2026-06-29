import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Forkly auth app (login + register). Runs on its own port so the landing page
// can redirect to it via VITE_LOGIN_URL.
// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5174,
    // Fail loudly if 5174 is taken instead of silently drifting to another port —
    // the landing's login iframe is hard-coded to 5174 (VITE_LOGIN_URL).
    strictPort: true,
  },
})
