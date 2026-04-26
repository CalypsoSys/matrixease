import { buildGatewayUpstreamUrl } from '../../src/utils/cloudflareGateway.js'

type GatewayEnv = {
  API_BASE_URL?: string
  INTERNAL_API_KEY?: string
  GATEWAY_SECRET_HEADER_NAME?: string
}

export async function proxyGatewayRequest(request: Request, env: GatewayEnv)
{
  if (!env.API_BASE_URL) {
    return new Response('Missing API_BASE_URL configuration.', { status: 500 })
  }

  const headers = new Headers(request.headers)
  const gatewayHeaderName = env.GATEWAY_SECRET_HEADER_NAME || 'X-Internal-Api-Key'

  if (env.INTERNAL_API_KEY) {
    headers.set(gatewayHeaderName, env.INTERNAL_API_KEY)
  }

  headers.delete('Host')

  const upstreamResponse = await fetch(buildGatewayUpstreamUrl(request.url, env.API_BASE_URL), {
    method: request.method,
    headers,
    body: request.method === 'GET' || request.method === 'HEAD' ? undefined : request.body,
    redirect: 'manual'
  })

  const responseHeaders = new Headers(upstreamResponse.headers)
  responseHeaders.set('X-Api-Gateway', 'cloudflare-pages-function')

  return new Response(upstreamResponse.body, {
    status: upstreamResponse.status,
    statusText: upstreamResponse.statusText,
    headers: responseHeaders
  })
}
