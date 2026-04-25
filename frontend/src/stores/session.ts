import { defineStore } from 'pinia'
import type { MatrixEaseApi, SessionBootstrap } from '@/services/api'

const accessCookieName = 'authenticated-accepted-1'
const cookieConsentName = 'cookies-accepted-1'
const cookieConsentValue = 'acceptedxxx'

function readCookie(name: string)
{
  const prefix = `${name}=`

  for (const cookie of document.cookie.split(';')) {
    const trimmed = cookie.trim()
    if (trimmed.startsWith(prefix)) {
      return decodeURIComponent(trimmed.slice(prefix.length))
    }
  }

  return ''
}

function writeCookie(name: string, value: string)
{
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=2592000; SameSite=Lax`
}

export const useSessionStore = defineStore('session', {
  state: () => ({
    bootstrap: null as SessionBootstrap | null,
    accessToken: readCookie(accessCookieName),
    cookiesAcceptedCookie: readCookie(cookieConsentName)
  }),
  getters: {
    matrixEaseId: state => state.bootstrap?.MatrixEaseId ?? '',
    hasAcceptedCookies: state => state.cookiesAcceptedCookie === cookieConsentValue || state.bootstrap?.CookiesAccepted === true,
    hasAccessToken: state => Boolean(state.accessToken),
    googleSignedIn: state => state.bootstrap?.GoogleSignedIn === true,
    hasEmailIdentity: state => state.bootstrap?.HasEmailIdentity === true
  },
  actions: {
    async refreshBootstrap(api: MatrixEaseApi)
    {
      this.bootstrap = await api.bootstrapSession()
      this.accessToken = readCookie(accessCookieName)
      this.cookiesAcceptedCookie = readCookie(cookieConsentName)
      return this.bootstrap
    },

    acceptCookies()
    {
      writeCookie(cookieConsentName, cookieConsentValue)
      this.cookiesAcceptedCookie = cookieConsentValue

      if (this.bootstrap) {
        this.bootstrap = {
          ...this.bootstrap,
          CookiesAccepted: true
        }
      }
    },

    async validateAccess(api: MatrixEaseApi)
    {
      this.accessToken = readCookie(accessCookieName)

      if (!this.matrixEaseId || !this.accessToken) {
        return false
      }

      const response = await api.validateCatalogAccess(this.matrixEaseId, this.accessToken)
      return response.Success && response.AccessToken === this.accessToken
    }
  }
})
