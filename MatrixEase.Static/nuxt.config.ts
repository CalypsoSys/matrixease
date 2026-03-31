export default defineNuxtConfig({
  compatibilityDate: '2026-03-30',
  devtools: {
    enabled: true
  },
  ssr: false,
  modules: ['@nuxt/ui'],
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    public: {
      matrixease: {
        platform: process.env.NUXT_PUBLIC_MATRIXEASE_PLATFORM ?? 'web',
        apiBase: process.env.NUXT_PUBLIC_MATRIXEASE_API_BASE ?? 'https://api.matrixease.com',
        docsBase: process.env.NUXT_PUBLIC_MATRIXEASE_DOCS_BASE ?? 'https://docs.matrixease.com',
        marketingBase: process.env.NUXT_PUBLIC_MATRIXEASE_MARKETING_BASE ?? 'https://www.matrixease.com',
        embeddedApi: process.env.NUXT_PUBLIC_MATRIXEASE_EMBEDDED_API === 'true'
      }
    }
  },
  app: {
    head: {
      title: 'MatrixEase',
      meta: [
        {
          name: 'viewport',
          content: 'width=device-width, initial-scale=1'
        },
        {
          name: 'description',
          content: 'MatrixEase frontend scaffold for shared web and Electron delivery.'
        }
      ]
    }
  }
})
