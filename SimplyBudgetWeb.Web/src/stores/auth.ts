import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  PublicClientApplication,
  InteractionRequiredAuthError,
  type AccountInfo,
} from '@azure/msal-browser'
import { msalConfig, loginRequest, apiScopes } from '@/authConfig'
import { apiClient, setTokenProvider, setUnauthorizedHandler, SessionExpiredError } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import { navigateToLogin } from '@/services/sessionNavigation'
import type { CurrentUserDto } from '@/types'

const msalInstance = new PublicClientApplication(msalConfig)
let initialized: Promise<void> | null = null
let sessionExpiring: Promise<void> | null = null

export const useAuthStore = defineStore('auth', () => {
  const account = ref<AccountInfo | null>(null)
  const customDisplayName = ref<string | null>(null)
  const isInitializing = ref(true)
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
    const activeAccount = account.value ?? msalInstance.getAllAccounts()[0] ?? null
    if (!activeAccount) {
      await handleSessionExpired()
      throw new SessionExpiredError('You are not signed in. Please sign in again.')
    }

    try {
      // acquireTokenSilent renews an expired access token using the cached refresh token.
      const result = await msalInstance.acquireTokenSilent({
        ...apiScopes,
        account: activeAccount,
      })
      return result.accessToken
    } catch (e) {
      if (!(e instanceof InteractionRequiredAuthError)) {
        // Transient/cache issue: make one more explicit refresh attempt before giving up.
        try {
          const result = await msalInstance.acquireTokenSilent({
            ...apiScopes,
            account: activeAccount,
            forceRefresh: true,
          })
          return result.accessToken
        } catch (retryError) {
          if (!(retryError instanceof InteractionRequiredAuthError)) {
            throw retryError
          }
        }
      }

      // The session cannot be refreshed without user interaction: sign the user
      // out locally and send them back to the sign in page.
      await handleSessionExpired()
      throw new SessionExpiredError()
    }
  }

  /**
   * Called when the token can no longer be refreshed (or the API rejects it).
   * Clears the local session and returns the user to the sign in page.
   */
  async function handleSessionExpired(): Promise<void> {
    if (sessionExpiring) {
      await sessionExpiring
      return
    }

    const expiring = (async () => {
      const wasAuthenticated = account.value !== null
      account.value = null
      customDisplayName.value = null

      try {
        await msalInstance.clearCache()
      } catch {
        // Best effort - the local session is already considered gone.
      }
      refreshAccount()

      if (wasAuthenticated) {
        useSnackbarStore().enqueueSnackbar(
          'Your session has expired. Please sign in again.',
          { variant: 'warning' },
        )
      }
      navigateToLogin()
    })()

    sessionExpiring = expiring
    try {
      await expiring
    } finally {
      sessionExpiring = null
    }
  }

  async function initialize() {
    if (!initialized) {
      initialized = (async () => {
        try {
          await msalInstance.initialize()
          await msalInstance.handleRedirectPromise()
          refreshAccount()
          setTokenProvider(getToken)
          setUnauthorizedHandler(handleSessionExpired)
          await refreshCurrentUserProfile()
        } finally {
          isInitializing.value = false
        }
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
    isInitializing,
    initialize,
    login,
    logout,
    getToken,
    handleSessionExpired,
    refreshCurrentUserProfile,
    setDisplayName,
  }
})
