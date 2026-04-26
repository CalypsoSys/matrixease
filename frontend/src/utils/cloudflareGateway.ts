export function buildGatewayUpstreamUrl(requestUrl: string, apiBaseUrl: string)
{
  const incomingUrl = new URL(requestUrl)
  const upstreamBaseUrl = apiBaseUrl.endsWith('/')
    ? apiBaseUrl.slice(0, -1)
    : apiBaseUrl

  return new URL(incomingUrl.pathname + incomingUrl.search, upstreamBaseUrl).toString()
}
