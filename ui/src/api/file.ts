import { request } from '@/utils/request'
import type { PagedResult } from '@/types/api'

export interface FileUploadResult {
  fileId: number
}

export interface FileDto {
  id: number
  originalName: string
  extension: string
  size: number
  createdAt: string
}

/** 上传文件（Admin） */
export function uploadFile(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return request<FileUploadResult>({
    url: '/file/upload',
    method: 'post',
    data: formData,
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

export interface FilePageInput {
  pageNumber?: number
  pageSize?: number
  originalName?: string
}

/** 文件分页列表（Admin） */
export function getFilePage(params?: FilePageInput) {
  return request<PagedResult<FileDto>>({ url: '/file', method: 'get', params })
}

/** 删除文件（Admin） */
export function deleteFile(id: number) {
  return request<void>({ url: `/file/${id}`, method: 'delete' })
}
