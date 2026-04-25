import { defineStore } from 'pinia'
import { resolveRuntimeConfig } from '@/services/runtime'

export const useRuntimeStore = defineStore('runtime', {
  state: () => ({
    runtime: resolveRuntimeConfig()
  })
})
