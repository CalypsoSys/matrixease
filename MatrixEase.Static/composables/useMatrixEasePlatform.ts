type MatrixEasePlatformLink = 'about' | 'contact' | 'docs' | 'blog' | 'support'

export function useMatrixEasePlatform()
{
  const runtime = useMatrixEaseRuntime()

  const openLink = (type: MatrixEasePlatformLink, spec = '') =>
  {
    const urls: Record<MatrixEasePlatformLink, string> = {
      about: `${runtime.marketingBase}/index.html#about`,
      contact: `${runtime.marketingBase}#contact-us`,
      docs: spec ? `${runtime.docsBase}${spec}` : runtime.docsBase,
      blog: 'https://blog.matrixease.com',
      support: 'https://support.matrixease.com'
    }

    window.open(urls[type], '_blank', 'noopener,noreferrer')
  }

  return {
    openLink
  }
}
