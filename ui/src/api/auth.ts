import { request } from '@/utils/request'

// ===== 类型（对齐后端 DTO）=====

export interface LoginRequest {
  userName: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface RefreshTokenRequest {
  accessToken: string
  refreshToken: string
}

// ===== 接口 =====

export function login(data: LoginRequest) {
  return request<LoginResponse>({
    url: '/auth/login',
    method: 'post',
    data,
  })
}

export function refreshToken(data: RefreshTokenRequest) {
  return request<LoginResponse>({
    url: '/auth/refresh',
    method: 'post',
    data,
  })
}
