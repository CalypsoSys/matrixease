import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  fetchSupabaseUser,
  refreshSupabaseSession,
  sendSupabaseMagicLink,
  sendSupabasePasswordRecovery,
  signInWithPassword,
  signUpWithPassword,
  updateSupabasePassword,
  type SupabaseSession,
  type SupabaseUserResponse
} from '@/services/supabase-auth'

type SessionRecord = {
  accessToken: string
  refreshToken: string
  expiresAt: number
  email: string | null
}

const SESSION_KEY = 'matrixease.session'

function getExpiresAt(session: SupabaseSession): number {
  return session.expires_at ?? Math.floor(Date.now() / 1000) + (session.expires_in ?? 3600)
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const expiresAt = ref<number | null>(null)
  const email = ref<string | null>(null)
  const isPasswordRecovery = ref(false)
  const isReady = ref(false)

  function loadSession(): void {
    if (consumeRedirectSession()) {
      isReady.value = true
      return
    }

    const raw = window.localStorage.getItem(SESSION_KEY)
    if (!raw) {
      isReady.value = true
      return
    }

    try {
      const parsed = JSON.parse(raw) as SessionRecord
      accessToken.value = parsed.accessToken || null
      refreshToken.value = parsed.refreshToken || null
      expiresAt.value = parsed.expiresAt || null
      email.value = parsed.email || null
    } catch {
      clearSession()
    }

    isReady.value = true
  }

  function persistSession(): void {
    if (!accessToken.value || !refreshToken.value || !expiresAt.value) {
      window.localStorage.removeItem(SESSION_KEY)
      return
    }

    window.localStorage.setItem(
      SESSION_KEY,
      JSON.stringify({
        accessToken: accessToken.value,
        refreshToken: refreshToken.value,
        expiresAt: expiresAt.value,
        email: email.value
      } satisfies SessionRecord)
    )
  }

  function setSession(session: SupabaseSession): boolean {
    if (!session.access_token || !session.refresh_token) {
      return false
    }

    accessToken.value = session.access_token
    refreshToken.value = session.refresh_token
    expiresAt.value = getExpiresAt(session)
    email.value = session.user?.email ?? email.value
    persistSession()
    return true
  }

  function parseNumber(value: string | null): number | undefined {
    if (!value) {
      return undefined
    }

    const parsed = Number(value)
    return Number.isFinite(parsed) ? parsed : undefined
  }

  function removeAuthHash(): void {
    window.history.replaceState(null, document.title, `${window.location.pathname}${window.location.search}`)
  }

  function consumeRedirectSession(): boolean {
    if (!window.location.hash.includes('access_token=')) {
      return false
    }

    const params = new URLSearchParams(window.location.hash.slice(1))
    const redirectAccessToken = params.get('access_token')
    const redirectRefreshToken = params.get('refresh_token')

    if (!redirectAccessToken || !redirectRefreshToken) {
      return false
    }

    const hasSession = setSession({
      access_token: redirectAccessToken,
      refresh_token: redirectRefreshToken,
      expires_in: parseNumber(params.get('expires_in')),
      expires_at: parseNumber(params.get('expires_at')),
      type: params.get('type') ?? undefined,
      user: {
        email: params.get('email') ?? undefined
      }
    })

    if (hasSession) {
      isPasswordRecovery.value = params.get('type') === 'recovery'
      removeAuthHash()
    }

    return hasSession
  }

  function applyUser(user: SupabaseUserResponse): void {
    email.value = user.email ?? email.value
    persistSession()
  }

  async function hydrateUser(): Promise<void> {
    if (!accessToken.value || email.value) {
      return
    }

    try {
      applyUser(await fetchSupabaseUser(accessToken.value))
    } catch {
      // A missing email should not block sign-in or project loading.
    }
  }

  async function initialize(): Promise<void> {
    if (!isReady.value) {
      loadSession()
    } else {
      consumeRedirectSession()
    }

    const nowSeconds = Math.floor(Date.now() / 1000)
    if (accessToken.value && expiresAt.value && expiresAt.value > nowSeconds + 30) {
      await hydrateUser()
      return
    }

    if (!refreshToken.value) {
      return
    }

    try {
      setSession(await refreshSupabaseSession(refreshToken.value))
      await hydrateUser()
    } catch {
      clearSession()
    }
  }

  async function signIn(emailAddress: string, password: string): Promise<boolean> {
    return setSession(await signInWithPassword(emailAddress, password))
  }

  async function signUp(emailAddress: string, password: string): Promise<boolean> {
    return setSession(await signUpWithPassword(emailAddress, password))
  }

  async function sendMagicLink(emailAddress: string): Promise<void> {
    await sendSupabaseMagicLink(emailAddress)
  }

  async function sendPasswordRecovery(emailAddress: string): Promise<void> {
    await sendSupabasePasswordRecovery(emailAddress)
  }

  async function updatePassword(password: string): Promise<void> {
    if (!accessToken.value) {
      throw new Error('Password recovery session is not active.')
    }

    applyUser(await updateSupabasePassword(accessToken.value, password))
    isPasswordRecovery.value = false
  }

  function clearSession(): void {
    accessToken.value = null
    refreshToken.value = null
    expiresAt.value = null
    email.value = null
    isPasswordRecovery.value = false
    window.localStorage.removeItem(SESSION_KEY)
  }

  loadSession()

  return {
    accessToken,
    refreshToken,
    expiresAt,
    email,
    isReady,
    isPasswordRecovery,
    isAuthenticated: computed(() => Boolean(accessToken.value)),
    initialize,
    signIn,
    signUp,
    sendMagicLink,
    sendPasswordRecovery,
    updatePassword,
    clearSession
  }
})
