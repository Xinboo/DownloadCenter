import type { Router } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getSystemStatus } from '@/api/user'

const WHITE_LIST = ['/login', '/setup']

let systemChecked = false
let systemInitialized = true

export function markSystemInitialized() {
  systemInitialized = true
}

export function setupGuard(router: Router) {
  router.beforeEach(async (to) => {
    if (!systemChecked) {
      try {
        const status = await getSystemStatus()
        systemInitialized = status.initialized
      } catch {
        systemInitialized = true
      }
      systemChecked = true
    }

    if (!systemInitialized && to.path !== '/setup') {
      return '/setup'
    }

    if (WHITE_LIST.includes(to.path)) {
      return true
    }

    const authStore = useAuthStore()
    if (!authStore.isLoggedIn) {
      return '/login'
    }

    return true
  })
}
