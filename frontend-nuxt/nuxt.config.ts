import { resolve } from 'path';

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  vite: {
    server: {
      fs: {
        allow: [resolve(__dirname, '..')]
      }
    }
  },
  vue: {
    compilerOptions: {
      isCustomElement: (tag) => tag.includes('-')
    }
  }
})
