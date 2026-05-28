import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

function readFrontendFile(relativePath: string): string {
  return readFileSync(resolve(process.cwd(), relativePath), 'utf8')
}

describe('frontend SEO shell', () => {
  it('publishes metadata, robots, and sitemap for the app host', () => {
    const html = readFrontendFile('index.html')
    const robots = readFrontendFile('public/robots.txt')
    const sitemap = readFrontendFile('public/sitemap.xml')

    expect(html).toMatch(/<meta name="description" content="[^"]+"/)
    expect(html).toMatch(/<meta name="robots" content="index, follow"/)
    expect(html).toMatch(/<link rel="canonical" href="https:\/\/app\.matrixease\.com\/"/)
    expect(html).toMatch(/<meta property="og:title" content="MatrixEase"/)
    expect(html).toMatch(/<meta property="og:url" content="https:\/\/app\.matrixease\.com\/"/)
    expect(html).toMatch(/<meta name="twitter:card" content="summary"/)
    expect(robots).toMatch(/User-agent: \*/)
    expect(robots).toMatch(/Sitemap: https:\/\/app\.matrixease\.com\/sitemap\.xml/)
    expect(sitemap).toMatch(/<loc>https:\/\/app\.matrixease\.com\/<\/loc>/)
  })
})
