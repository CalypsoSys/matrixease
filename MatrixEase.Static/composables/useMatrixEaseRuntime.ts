type MatrixEaseRuntime = {
  platform: string
  apiBase: string
  docsBase: string
  marketingBase: string
  embeddedApi: boolean
}

export function useMatrixEaseRuntime(): MatrixEaseRuntime
{
  const config = useRuntimeConfig()
  return config.public.matrixease as MatrixEaseRuntime
}
