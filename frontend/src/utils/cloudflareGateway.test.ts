import { describe, expect, it } from 'vitest'
import { buildGatewayForwardHeaders, buildGatewayUpstreamUrl } from '@/utils/cloudflareGateway'

describe('cloudflare gateway helpers', () => {
  it('preserves the /api path and query string', () => {
    const upstream = buildGatewayUpstreamUrl(
      'https://app.matrixease.com/api/matrixease/projects?filter=recent',
      'https://api.matrixease.com'
    )

    expect(upstream).toBe('https://api.matrixease.com/api/matrixease/projects?filter=recent')
  })

  it('adds forwarding and gateway secret headers', () => {
    const headers = buildGatewayForwardHeaders(
      'https://app.matrixease.com/api/matrixease/projects',
      { Accept: 'application/json', Host: 'app.matrixease.com' },
      'test-secret',
      'X-MatrixEase-Gateway'
    )

    expect(headers.get('Accept')).toBe('application/json')
    expect(headers.get('Host')).toBeNull()
    expect(headers.get('X-Forwarded-Host')).toBe('app.matrixease.com')
    expect(headers.get('X-Forwarded-Proto')).toBe('https')
    expect(headers.get('X-MatrixEase-Gateway')).toBe('test-secret')
  })
})
