// 后端统一响应格式（Newtonsoft 驼峰序列化，字段均为小写开头）
export interface ApiResult<T = unknown> {
  code: number
  success: boolean
  message: string | null
  data: T
}

// 后端分页响应格式
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}
