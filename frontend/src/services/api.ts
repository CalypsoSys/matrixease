import axios, { AxiosError, type AxiosRequestConfig } from 'axios'
import { buildApiUrl } from '@/services/runtime-config'

const SESSION_KEY = 'matrixease.session'

export type ApiFailure = {
  Success?: boolean
  Message?: string
  Error?: string
  success?: boolean
  message?: string
  error?: string
}

type SessionShape = {
  accessToken?: string
}

function getAccessToken(): string | null {
  const raw = window.localStorage.getItem(SESSION_KEY)
  if (!raw) {
    return null
  }

  try {
    return (JSON.parse(raw) as SessionShape).accessToken ?? null
  } catch {
    return null
  }
}

export const apiClient = axios.create()

apiClient.interceptors.request.use((config) => {
  if (config.url) {
    config.url = buildApiUrl(config.url)
  }

  config.headers = config.headers ?? {}

  const accessToken = getAccessToken()
  if (accessToken && !config.headers.Authorization) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }

  return config
})

export function getApiMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const responseData = (error as AxiosError<ApiFailure>).response?.data
    if (responseData?.Message) {
      return responseData.Message
    }
    if (responseData?.Error) {
      return responseData.Error
    }
    if (responseData?.message) {
      return responseData.message
    }
    if (responseData?.error) {
      return responseData.error
    }
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

async function request<T>(config: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.request<T>(config)
  return response.data
}

export function apiGet<T>(url: string): Promise<T> {
  return request<T>({ method: 'GET', url })
}

export function apiPost<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  return request<T>({ method: 'POST', url, data, ...config })
}
