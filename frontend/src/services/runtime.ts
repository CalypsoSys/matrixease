export type MatrixEaseRuntime = {
  platform: string
  apiBase: string
  docsBase: string
  marketingBase: string
  embeddedApi: boolean
}

export function resolveRuntimeConfig(): MatrixEaseRuntime
{
  return {
    platform: import.meta.env.VITE_MATRIXEASE_PLATFORM ?? 'web',
    apiBase: import.meta.env.VITE_MATRIXEASE_API_BASE ?? 'https://api.matrixease.com',
    docsBase: import.meta.env.VITE_MATRIXEASE_DOCS_BASE ?? 'https://docs.matrixease.com',
    marketingBase: import.meta.env.VITE_MATRIXEASE_MARKETING_BASE ?? 'https://www.matrixease.com',
    embeddedApi: import.meta.env.VITE_MATRIXEASE_EMBEDDED_API === 'true'
  }
}
