import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  PublicClientApplication,
  InteractionRequiredAuthError,
  type AccountInfo,
} from '@azure/msal-browser'
import { msalConfig, loginRequest, apiScopes } from '@/authConfig'
import { apiClient, setTokenProvider } from '@/services/apiClient'
import type { CurrentUserDto } from '@/types'

const msalInstance = new PublicClientApplication(msalConfig)
let initialized: Promise<void> | null = null

export const useAuthStore = defineStore('auth', () => {
  const account = ref<AccountInfo | null>(null)
  const customDisplayName = ref<string | null>(null)
  const isAuthenticated = computed(() => account.value !== null)
  const displayName = computed(() => customDisplayName.value ?? account.value?.name ?? null)

  function refreshAccount() {
    const accounts = msalInstance.getAllAccounts()
    account.value = accounts[0] ?? null
    if (!account.value) {
      customDisplayName.value = null
    }
  }

  async function refreshCurrentUserProfile() {
    if (!account.value) {
      customDisplayName.value = null
      return
    }

    try {
      const profile = await apiClient.get<CurrentUserDto>('/api/current-user')
      customDisplayName.value = profile.displayName ?? account.value?.name ?? null
    } catch {
      customDisplayName.value = account.value?.name ?? null
    }
  }

  function setDisplayName(name: string | null) {
    customDisplayName.value = name
  }

  async function getToken(): Promise<string> {
    try {
      const result = await msalInstance.acquireTokenSilent({
        ...apiScopes,
        account: account.value ?? undefined,
      })
      return result.accessToken
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        await msalInstance.acquireTokenRedirect(apiScopes)
      }
      throw e
    }
  }

  async function initialize() {
    if (!initialized) {
      initialized = (async () => {
        await msalInstance.initialize()
        await msalInstance.handleRedirectPromise()
        refreshAccount()
        setTokenProvider(getToken)
        await refreshCurrentUserProfile()
      })()
    }
    await initialized
  }

  function login() {
    void msalInstance.loginRedirect(loginRequest)
  }

  function logout() {
    void msalInstance.logoutRedirect()
  }

  return {
    account,
    displayName,
    isAuthenticated,
    initialize,
    login,
    logout,
    getToken,
    refreshCurrentUserProfile,
    setDisplayName,
  }
})
