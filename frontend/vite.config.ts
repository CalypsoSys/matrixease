import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'

const proxiedPaths = ['/api', '/google', '/account', '/signin-google']

export default defineConfig(({ mode }) =>
{
  const env = loadEnv(mode, process.cwd(), '')
  const apiProxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5000'

  return {
    plugins: [vue(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    server: {
      host: 'localhost',
      port: 3000,
      proxy: Object.fromEntries(
        proxiedPaths.map(path => [
          path,
          {
            target: apiProxyTarget,
            changeOrigin: true,
            secure: false
          }
        ])
      )
    },
    preview: {
      host: 'localhost',
      port: 4173
    },
    test: {
      environment: 'node',
      include: ['tests/**/*.spec.ts']
    }
  }
})
