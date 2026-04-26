import { describe, expect, it } from 'vitest'
import { buildGatewayUpstreamUrl } from '@/utils/cloudflareGateway'

describe('buildGatewayUpstreamUrl', () =>
{
  it('preserves path and query while retargeting the upstream host', () =>
  {
    expect(
      buildGatewayUpstreamUrl(
        'https://shared.inctrak.com/api/session/bootstrap?matrixease_id=abc',
        'https://api.shared.inctrak.com'
      )
    ).toBe('https://api.shared.inctrak.com/api/session/bootstrap?matrixease_id=abc')
  })
})
