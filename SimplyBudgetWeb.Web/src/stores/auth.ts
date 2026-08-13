import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  PublicClientApplication,
  InteractionRequiredAuthError,
  type AccountInfo,
} from '@azure/msal-browser'
import { msalConfig, loginRequest, apiScopes } from '@/authConfig'
import { setTokenProvider } from '@/services/apiClient'

const msalInstance = new PublicClientApplication(msalConfig)
let initialized: Promise<void> | null = null

export const useAuthStore = defineStore('auth', () => {
  const account = ref<AccountInfo | null>(null)
  const isAuthenticated = computed(() => account.value !== null)

  function refreshAccount() {
    const accounts = msalInstance.getAllAccounts()
    account.value = accounts[0] ?? null
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

  return { account, isAuthenticated, initialize, login, logout, getToken }
})
