import { proxyGatewayRequest } from './_shared/gateway.js'

export async function onRequest(context: {
  request: Request
  env: {
    API_BASE_URL?: string
    INTERNAL_API_KEY?: string
    GATEWAY_SECRET_HEADER_NAME?: string
  }
})
{
  return proxyGatewayRequest(context.request, context.env)
}
