import { createContext, useContext, useEffect, ReactNode } from 'react'
import { useMsal, useIsAuthenticated } from '@azure/msal-react'
import { AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser'
import { loginRequest, apiScopes } from '@/authConfig'
import { setTokenProvider } from '@/services/apiClient'

interface AuthContextType {
  account: AccountInfo | null
  isAuthenticated: boolean
  login: () => void
  logout: () => void
  getToken: () => Promise<string>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const { instance, accounts } = useMsal()
  const isAuthenticated = useIsAuthenticated()
  const account = accounts[0] ?? null

  const login = () => instance.loginRedirect(loginRequest)
  const logout = () => instance.logoutRedirect()

  const getToken = async (): Promise<string> => {
    try {
      const result = await instance.acquireTokenSilent({
        ...apiScopes,
        account: account ?? undefined,
      })
      return result.accessToken
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        await instance.acquireTokenRedirect(apiScopes)
      }
      throw e
    }
  }

  useEffect(() => {
    setTokenProvider(getToken)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [account])

  return (
    <AuthContext.Provider value={{ account, isAuthenticated, login, logout, getToken }}>
      {children}
    </AuthContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}
