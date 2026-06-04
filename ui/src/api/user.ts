import { request } from '@/utils/request'
import type { PagedResult } from '@/types/api'
import type { UserRole } from '@/enums/user'

// ===== 类型（对齐后端 DTO）=====
export interface RegisterAdminRequest {
  userName: string
  password: string
  nickName: string
}

// 系统状态：是否已初始化（已有用户）
export interface SystemStatus {
  initialized: boolean
}

export interface ChangePasswordRequest {
  oldPassword: string
  newPassword: string
  confirmPassword: string
}

export interface UserDto {
  id: string
  userName: string
  role: UserRole
  nickName: string
  company: string | null
  phone: string | null
  createdAt: string
}

export interface CreateUserRequest {
  userName: string
  password: string
  role: UserRole
  nickName: string
  company?: string
  phone?: string
}

export interface UpdateUserRequest {
  role: UserRole
  nickName: string
  company?: string
  phone?: string
}

// ===== 接口 =====

/** 检测系统是否已初始化（是否已有用户） */
export function getSystemStatus() {
  return request<SystemStatus>({
    url: '/user/status',
    method: 'get',
  })
}

/** 注册管理员（仅系统无用户时可用） */
export function registerAdmin(data: RegisterAdminRequest) {
  return request<void>({
    url: '/user/register-admin',
    method: 'post',
    data,
  })
}

/** 修改密码（需登录） */
export function changePassword(data: ChangePasswordRequest) {
  return request<void>({
    url: '/user/change-password',
    method: 'put',
    data,
  })
}

export interface UserPageInput {
  pageNumber?: number
  pageSize?: number
  userName?: string
  nickName?: string
}

export interface UserListInput {
  userName?: string
  nickName?: string
}

/** 用户分页列表（Admin） */
export function getUserPage(params?: UserPageInput) {
  return request<PagedResult<UserDto>>({
    url: '/user',
    method: 'get',
    params,
  })
}

/** 用户不分页列表（Admin） */
export function getUserList(params?: UserListInput) {
  return request<UserDto[]>({
    url: '/user/list',
    method: 'get',
    params,
  })
}

/** 创建用户（Admin） */
export function createUser(data: CreateUserRequest) {
  return request<void>({
    url: '/user',
    method: 'post',
    data,
  })
}

/** 删除用户（Admin） */
export function deleteUser(id: string) {
  return request<void>({
    url: `/user/${id}`,
    method: 'delete',
  })
}

/** 编辑用户（Admin） */
export function updateUser(id: string, data: UpdateUserRequest) {
  return request<void>({
    url: `/user/${id}`,
    method: 'put',
    data,
  })
}
