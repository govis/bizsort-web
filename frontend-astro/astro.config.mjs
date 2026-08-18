// @ts-check
import { defineConfig } from 'astro/config';

import node from '@astrojs/node';

// https://astro.build/config
export default defineConfig({
  output: 'server',

  vite: {
    define: {
      'process.env.NEXT_PUBLIC_API_URL': JSON.stringify('https://localhost:5001'),
      'process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY': JSON.stringify('')
    },
    server: {
      fs: {
        allow: ['..']
      }
    }
  },

  adapter: node({
    mode: 'standalone'
  })
});