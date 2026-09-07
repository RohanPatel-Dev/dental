import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The dev proxy target is supplied by Aspire (VITE_DEV_PROXY_TARGET) so the SPA never hard codes a
// port. In production the SPA is served from the same origin as the API and no proxy exists at all.
const proxyTarget = process.env.VITE_DEV_PROXY_TARGET ?? 'http://localhost:5030';

export default defineConfig({
  plugins: [react(), tailwindcss()],

  // The alias is declared here as well as in tsconfig.json: tsconfig paths are for the type
  // checker, and the dev server resolves imports itself.
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },

  server: {
    port: Number(process.env.PORT ?? 5174),
    strictPort: true,
    proxy: {
      '/api': { target: proxyTarget, changeOrigin: true },
      '/health': { target: proxyTarget, changeOrigin: true },
    },
  },
  build: {
    // Fail the build rather than shipping a chunk nobody meant to create.
    chunkSizeWarningLimit: 600,
    sourcemap: true,
  },
});
