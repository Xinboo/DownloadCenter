import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { login as loginApi, refreshToken as refreshApi } from '@/api/auth'
import { TOKEN_KEY, REFRESH_TOKEN_KEY } from '@/constants/storage'
import { UserRole } from '@/enums/user'

export interface UserInfo {
  userId: string
  userName: string
  nickName: string
  role: UserRole
  canAccessAllProducts: boolean
}

const CLAIM_KEYS = {
  userId: ['nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier', 'sub'],
  userName: ['unique_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name', 'name'],
  nickName: ['given_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'],
  role: ['role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
}

function parseJwtPayload(token: string): Record<string, string> | null {
  try {
    const base64 = token.split('.')[1]
    if (!base64) return null
    const bytes = Uint8Array.from(atob(base64.replace(/-/g, '+').replace(/_/g, '/')), (c) => c.charCodeAt(0))
    const json = new TextDecoder().decode(bytes)
    return JSON.parse(json)
  } catch {
    return null
  }
}

function findClaim(payload: Record<string, string>, keys: string[]): string {
  for (const key of keys) {
    if (payload[key]) return payload[key]
  }
  return ''
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const refreshTokenValue = ref<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY))

  const isLoggedIn = computed(() => !!accessToken.value)

  const userInfo = computed<UserInfo | null>(() => {
    if (!accessToken.value) return null
    const payload = parseJwtPayload(accessToken.value)
    if (!payload) return null
    return {
      userId: findClaim(payload, CLAIM_KEYS.userId),
      userName: findClaim(payload, CLAIM_KEYS.userName),
      nickName: findClaim(payload, CLAIM_KEYS.nickName),
      role: (findClaim(payload, CLAIM_KEYS.role) as UserRole) || UserRole.User,
      canAccessAllProducts: payload['CanAccessAllProducts'] === 'True',
    }
  })

  function setTokens(access: string, refresh: string) {
    accessToken.value = access
    refreshTokenValue.value = refresh
    localStorage.setItem(TOKEN_KEY, access)
    localStorage.setItem(REFRESH_TOKEN_KEY, refresh)
  }

  function clearTokens() {
    accessToken.value = null
    refreshTokenValue.value = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  }

  async function login(userName: string, password: string) {
    const res = await loginApi({ userName, password })
    setTokens(res.accessToken, res.refreshToken)
  }

  async function refresh() {
    if (!accessToken.value || !refreshTokenValue.value) {
      logout()
      return
    }
    try {
      const res = await refreshApi({
        accessToken: accessToken.value,
        refreshToken: refreshTokenValue.value,
      })
      setTokens(res.accessToken, res.refreshToken)
    } catch {
      logout()
    }
  }

  function logout() {
    clearTokens()
    // 动态导入避免 router → guard → store → router 循环依赖
    import('@/router').then(({ default: r }) => r.push('/login'))
  }

  return {
    accessToken,
    refreshTokenValue,
    isLoggedIn,
    userInfo,
    login,
    refresh,
    logout,
  }
})
