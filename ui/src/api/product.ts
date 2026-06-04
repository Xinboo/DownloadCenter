import { request } from '@/utils/request'
import type { PagedResult } from '@/types/api'

// ===== 类型（对齐后端 DTO）=====

export interface ProductDto {
  id: number
  name: string
  model: string
  description: string | null
  coverFileId: number | null
  fileCount: number
  createdAt: string
}

export interface ProductFileDto {
  fileId: number
  displayName: string
  description: string
  extension: string
  size: number
  createdAt: string
}

export interface ProductDetailDto {
  id: number
  name: string
  model: string
  description: string | null
  coverFileId: number | null
  createdAt: string
}

export interface ProductResourcePageInput {
  pageNumber?: number
  pageSize?: number
  displayName?: string
}

export interface CreateProductRequest {
  name: string
  model: string
  description?: string
  coverFileId?: number
}

export interface UpdateProductRequest {
  name: string
  model: string
  description?: string
  coverFileId?: number
}

export interface PublishResourceRequest {
  displayName: string
  description: string
}

export interface ProductPageInput {
  pageNumber?: number
  pageSize?: number
  name?: string
  model?: string
}

export interface ProductListInput {
  name?: string
  model?: string
}

// ===== 接口 =====

/** 产品分页列表 */
export function getProductPage(params?: ProductPageInput) {
  return request<PagedResult<ProductDto>>({ url: '/product', method: 'get', params })
}

/** 产品不分页列表 */
export function getProductList(params?: ProductListInput) {
  return request<ProductDto[]>({ url: '/product/list', method: 'get', params })
}

/** 产品详情 */
export function getProductDetail(id: number) {
  return request<ProductDetailDto>({ url: `/product/${id}`, method: 'get' })
}

/** 产品资源分页列表 */
export function getResourcePage(productId: number, params?: ProductResourcePageInput) {
  return request<PagedResult<ProductFileDto>>({ url: `/product/${productId}/resource`, method: 'get', params })
}

/** 创建产品（Admin） */
export function createProduct(data: CreateProductRequest) {
  return request<void>({ url: '/product', method: 'post', data })
}

/** 编辑产品（Admin） */
export function updateProduct(id: number, data: UpdateProductRequest) {
  return request<void>({ url: `/product/${id}`, method: 'put', data })
}

/** 删除产品（Admin） */
export function deleteProduct(id: number) {
  return request<void>({ url: `/product/${id}`, method: 'delete' })
}

/** 发布产品资源（Admin） */
export function publishResource(productId: number, fileId: number, data: PublishResourceRequest) {
  return request<void>({ url: `/product/${productId}/resource/${fileId}`, method: 'post', data })
}

/** 删除产品资源（Admin） */
export function deleteResource(productId: number, fileId: number) {
  return request<void>({ url: `/product/${productId}/resource/${fileId}`, method: 'delete' })
}
