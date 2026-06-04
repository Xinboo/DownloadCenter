import axios, { type AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'
import type { ApiResult } from '@/types/api'
import { useAuthStore } from '@/stores/auth'

const service = axios.create({
  baseURL: import.meta.env.VITE_API_BASE,
  timeout: 15000,
})

// 请求拦截器：从 store 读 token
service.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`
  }
  return config
})

// 401 刷新锁：防止多个并发请求同时触发 refresh
let isRefreshing = false
let pendingRequests: Array<() => void> = []

// 响应拦截器：处理后端 ApiResult（业务失败也是 HTTP 200）
service.interceptors.response.use(
  (response: AxiosResponse<ApiResult>) => {
    // blob 响应（文件下载）直通，不走 ApiResult 解包
    if (response.config.responseType === 'blob') {
      return response.data as unknown as AxiosResponse
    }

    const res = response.data

    if (res.success) {
      return res.data as unknown as AxiosResponse
    }

    // 业务失败（如密码错误）→ 弹提示并 reject，让调用方进 catch
    ElMessage.error(res.message || '操作失败')
    return Promise.reject(new Error(res.message || '操作失败'))
  },
  async (error) => {
    const status = error.response?.status
    const originalConfig = error.config

    if (status === 401 && !originalConfig._retried) {
      const authStore = useAuthStore()

      if (isRefreshing) {
        // 已有刷新在进行，排队等待
        return new Promise((resolve) => {
          pendingRequests.push(() => resolve(service(originalConfig)))
        })
      }

      originalConfig._retried = true
      isRefreshing = true

      try {
        await authStore.refresh()
        // 刷新成功，重发队列中的请求
        pendingRequests.forEach((cb) => cb())
        pendingRequests = []
        // 重发当前请求
        return service(originalConfig)
      } catch {
        pendingRequests = []
        return Promise.reject(error)
      } finally {
        isRefreshing = false
      }
    }

    // 非 401 或重试后仍失败
    if (status !== 401) {
      ElMessage.error(error.response?.data?.message || error.message || '网络异常')
    }
    return Promise.reject(error)
  },
)

// 泛型包装：让调用方能标注返回类型，request<XxxDto>(...) 直接拿到 XxxDto
export function request<T = unknown>(config: Parameters<typeof service.request>[0]) {
  return service.request(config) as unknown as Promise<T>
}

export default service
