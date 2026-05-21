import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  refreshSupabaseSession,
  signInWithPassword,
  signUpWithPassword,
  type SupabaseSession
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
  const isReady = ref(false)

  function loadSession(): void {
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

  async function initialize(): Promise<void> {
    if (!isReady.value) {
      loadSession()
    }

    const nowSeconds = Math.floor(Date.now() / 1000)
    if (accessToken.value && expiresAt.value && expiresAt.value > nowSeconds + 30) {
      return
    }

    if (!refreshToken.value) {
      return
    }

    try {
      setSession(await refreshSupabaseSession(refreshToken.value))
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

  function clearSession(): void {
    accessToken.value = null
    refreshToken.value = null
    expiresAt.value = null
    email.value = null
    window.localStorage.removeItem(SESSION_KEY)
  }

  loadSession()

  return {
    accessToken,
    refreshToken,
    expiresAt,
    email,
    isReady,
    isAuthenticated: computed(() => Boolean(accessToken.value)),
    initialize,
    signIn,
    signUp,
    clearSession
  }
})
