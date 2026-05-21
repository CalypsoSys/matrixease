export function buildGatewayUpstreamUrl(requestUrl: string, apiBaseUrl: string): string {
  const request = new URL(requestUrl)
  const upstream = new URL(apiBaseUrl)
  const basePath = upstream.pathname.replace(/\/+$/, '')

  upstream.pathname = `${basePath}${request.pathname}`.replace(/\/{2,}/g, '/')
  upstream.search = request.search

  return upstream.toString()
}

export function buildGatewayForwardHeaders(
  requestUrl: string,
  requestHeaders: HeadersInit,
  internalApiKey?: string,
  gatewaySecretHeaderName = 'X-Internal-Api-Key'
): Headers {
  const request = new URL(requestUrl)
  const headers = new Headers(requestHeaders)

  headers.set('X-Forwarded-Host', request.host)
  headers.set('X-Forwarded-Proto', request.protocol.replace(':', ''))

  if (internalApiKey && gatewaySecretHeaderName) {
    headers.set(gatewaySecretHeaderName, internalApiKey)
  }

  headers.delete('Host')

  return headers
}
