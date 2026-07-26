import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { ApiError, apiFetch } from './api'

export type CurrentUser = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export type RegisterData = {
  displayName: string
  email: string
  password: string
  role: 'Participant' | 'Organizer'
}

type AuthContextValue = {
  user: CurrentUser | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (data: RegisterData) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const loadCurrentUser = async () => {
      try {
        setUser(await apiFetch<CurrentUser>('/auth/me'))
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 401) {
          setUser(null)
        }
      } finally {
        setIsLoading(false)
      }
    }

    void loadCurrentUser()
  }, [])

  const login = async (email: string, password: string) => {
    const currentUser = await apiFetch<CurrentUser>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, rememberMe: false }),
    })
    setUser(currentUser)
  }

  const register = async (data: RegisterData) => {
    const currentUser = await apiFetch<CurrentUser>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    setUser(currentUser)
  }

  const logout = async () => {
    await apiFetch<void>('/auth/logout', { method: 'POST' })
    setUser(null)
  }

  return (
    <AuthContext.Provider
      value={{ user, isLoading, login, register, logout }}
    >
      {children}
    </AuthContext.Provider>
  )
}

// oxlint-disable-next-line react/only-export-components
export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }

  return context
}
