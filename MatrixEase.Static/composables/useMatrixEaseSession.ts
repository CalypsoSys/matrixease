const accessCookieName = 'authenticated-accepted-1'
const cookieConsentName = 'cookies-accepted-1'

export function useMatrixEaseSession()
{
  const api = useMatrixEaseApi()

  const bootstrap = useState<SessionBootstrap | null>('matrixease-bootstrap', () => null)
  const matrixEaseId = computed(() => bootstrap.value?.MatrixEaseId ?? '')
  const accessToken = useCookie<string | null>(accessCookieName, { default: () => null })
  const cookiesAccepted = useCookie<string | null>(cookieConsentName, { default: () => null })

  const refreshBootstrap = async () =>
  {
    bootstrap.value = await api.bootstrapSession()
    return bootstrap.value
  }

  const acceptCookies = () =>
  {
    cookiesAccepted.value = 'acceptedxxx'
    if (bootstrap.value) {
      bootstrap.value = {
        ...bootstrap.value,
        CookiesAccepted: true
      }
    }
  }

  const hasAcceptedCookies = computed(() =>
  {
    return cookiesAccepted.value === 'acceptedxxx' || bootstrap.value?.CookiesAccepted === true
  })

  const hasAccessToken = computed(() => Boolean(accessToken.value))

  const validateAccess = async () =>
  {
    if (!matrixEaseId.value || !accessToken.value) {
      return false
    }

    const response = await api.validateCatalogAccess(matrixEaseId.value, accessToken.value)
    return response.Success && response.AccessToken === accessToken.value
  }

  return {
    bootstrap,
    matrixEaseId,
    accessToken,
    hasAcceptedCookies,
    hasAccessToken,
    refreshBootstrap,
    acceptCookies,
    validateAccess
  }
}
