import { defineConfig } from 'astro/config';
import node from '@astrojs/node';

export default defineConfig({
  output: 'server',

  vite: {
    define: {
      'process.env.NEXT_PUBLIC_API_URL': JSON.stringify(''), // Empty means same-origin proxy
      'process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY': JSON.stringify('')
    },
    server: {
      fs: {
        allow: ['..']
      },
      proxy: {
        '/api': {
          target: 'https://localhost:5001',
          changeOrigin: true,
          secure: false // Bypass self-signed SSL errors!
        }
      }
    }
  },

  adapter: node({
    mode: 'standalone'
  })
});