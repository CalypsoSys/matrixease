const SUPABASE_URL = import.meta.env.VITE_SUPABASE_URL?.trim()
const SUPABASE_PUBLISHABLE_KEY = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY?.trim()

export type SupabaseSession = {
  access_token?: string
  refresh_token?: string
  expires_in?: number
  expires_at?: number
  type?: string
  user?: {
    id?: string
    email?: string
  }
}

export type SupabaseMessageResponse = {
  msg?: string
  message?: string
}

export type SupabaseUserResponse = {
  id?: string
  email?: string
}

function getSupabaseHeaders(): Record<string, string> {
  if (!SUPABASE_URL || !SUPABASE_PUBLISHABLE_KEY) {
    throw new Error('Supabase auth is not configured.')
  }

  return {
    'Content-Type': 'application/json',
    apikey: SUPABASE_PUBLISHABLE_KEY
  }
}

function parseSupabaseBody(text: string): Record<string, unknown> {
  if (!text) {
    return {}
  }

  try {
    return JSON.parse(text) as Record<string, unknown>
  } catch {
    return { message: text }
  }
}

function getSupabaseErrorMessage(body: Record<string, unknown>): string {
  const msg = body.msg
  const message = body.message

  return (typeof msg === 'string' && msg) || (typeof message === 'string' && message) || 'Supabase authentication failed.'
}

async function supabaseRequest<TResponse extends object>(path: string, payload: unknown): Promise<TResponse> {
  if (!SUPABASE_URL) {
    throw new Error('Supabase auth is not configured.')
  }

  const response = await fetch(`${SUPABASE_URL}/auth/v1/${path}`, {
    method: 'POST',
    headers: getSupabaseHeaders(),
    body: JSON.stringify(payload)
  })

  const body = parseSupabaseBody(await response.text())
  if (!response.ok) {
    throw new Error(getSupabaseErrorMessage(body))
  }

  return body as TResponse
}

async function supabaseAuthorizedRequest<TResponse extends object>(path: string, accessToken: string, payload?: unknown): Promise<TResponse> {
  if (!SUPABASE_URL) {
    throw new Error('Supabase auth is not configured.')
  }

  const response = await fetch(`${SUPABASE_URL}/auth/v1/${path}`, {
    method: payload === undefined ? 'GET' : 'PUT',
    headers: {
      ...getSupabaseHeaders(),
      Authorization: `Bearer ${accessToken}`
    },
    body: payload === undefined ? undefined : JSON.stringify(payload)
  })

  const body = parseSupabaseBody(await response.text())
  if (!response.ok) {
    throw new Error(getSupabaseErrorMessage(body))
  }

  return body as TResponse
}

export function signInWithPassword(email: string, password: string): Promise<SupabaseSession> {
  return supabaseRequest<SupabaseSession>('token?grant_type=password', { email, password })
}

export function signUpWithPassword(email: string, password: string): Promise<SupabaseSession> {
  return supabaseRequest<SupabaseSession>('signup', { email, password })
}

export function refreshSupabaseSession(refreshToken: string): Promise<SupabaseSession> {
  return supabaseRequest<SupabaseSession>('token?grant_type=refresh_token', { refresh_token: refreshToken })
}

export function sendSupabaseMagicLink(email: string): Promise<SupabaseMessageResponse> {
  return supabaseRequest<SupabaseMessageResponse>('otp', { email, create_user: true })
}

export function sendSupabasePasswordRecovery(email: string): Promise<SupabaseMessageResponse> {
  return supabaseRequest<SupabaseMessageResponse>('recover', { email })
}

export function fetchSupabaseUser(accessToken: string): Promise<SupabaseUserResponse> {
  return supabaseAuthorizedRequest<SupabaseUserResponse>('user', accessToken)
}

export function updateSupabasePassword(accessToken: string, password: string): Promise<SupabaseUserResponse> {
  return supabaseAuthorizedRequest<SupabaseUserResponse>('user', accessToken, { password })
}
