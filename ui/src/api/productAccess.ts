import { request } from '@/utils/request'

// ===== 类型 =====

export interface AuthorizedProductItem {
  id: number
  name: string
  model: string
}

export interface AuthorizedProductsResult {
  canAccessAllProducts: boolean
  products: AuthorizedProductItem[]
}

// ===== 接口 =====

/** 查询用户已授权产品（Admin） */
export function getAuthorizedProducts(userId: string) {
  return request<AuthorizedProductsResult>({
    url: `/product-access/${userId}/products`,
    method: 'get',
  })
}

/** 设置"查看全部产品"开关（Admin） */
export function setAccessAll(userId: string, canAccessAll: boolean) {
  return request<void>({
    url: `/product-access/${userId}/access-all`,
    method: 'put',
    data: canAccessAll,
    headers: { 'Content-Type': 'application/json' },
  })
}

/** 设置用户授权产品列表（Admin，全量覆盖） */
export function setAuthorizedProducts(userId: string, productIds: number[]) {
  return request<void>({
    url: `/product-access/${userId}/products`,
    method: 'put',
    data: { productIds },
  })
}
