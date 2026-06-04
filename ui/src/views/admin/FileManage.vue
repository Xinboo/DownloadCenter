<template>
  <div class="file-manage">
    <div class="page-header">
      <h2>文件管理</h2>
      <el-button type="primary" @click="uploadDialogVisible = true">上传文件</el-button>
    </div>

    <el-form :model="searchForm" inline class="search-bar">
      <el-form-item label="文件名">
        <el-input v-model="searchForm.originalName" placeholder="搜索文件名" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">搜索</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="fileList" v-loading="tableLoading" stripe>
      <el-table-column prop="originalName" label="文件名" min-width="200" show-overflow-tooltip />
      <el-table-column prop="extension" label="类型" width="80" />
      <el-table-column label="大小" width="100">
        <template #default="{ row }">{{ formatSize(row.size) }}</template>
      </el-table-column>
      <el-table-column prop="createdAt" label="上传时间" width="180" />
      <el-table-column min-width="1" />
      <el-table-column label="操作" width="160" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" text size="small" style="padding: 0 4px" @click="handleDownload(row)">下载</el-button>
          <el-button
            v-if="isImage(row.extension)"
            type="success" text size="small" style="margin-left: 4px; padding: 0 4px"
            @click="handlePreview(row)"
          >预览</el-button>
          <el-button type="danger" text size="small" style="margin-left: 4px; padding: 0 4px" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <div class="pagination-wrapper">
      <el-pagination
        v-model:current-page="pageNumber"
        v-model:page-size="pageSize"
        :total="totalCount"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @current-change="fetchList"
        @size-change="handleSizeChange"
      />
    </div>

    <!-- 上传文件 Dialog -->
    <el-dialog v-model="uploadDialogVisible" title="上传文件" width="520px" @closed="handleUploadClosed">
      <el-upload
        drag
        multiple
        :http-request="handleUpload"
        :show-file-list="true"
        :on-success="handleUploadSuccess"
      >
        <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
        <div class="el-upload__text">将文件拖到此处，或 <em>点击上传</em></div>
        <template #tip>
          <div class="el-upload__tip">支持常见文档、图片、压缩包，单文件最大 100MB</div>
        </template>
      </el-upload>
    </el-dialog>

    <!-- 图片预览 -->
    <el-image-viewer
      v-if="previewVisible"
      :url-list="previewUrls"
      @close="previewVisible = false"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { UploadFilled } from '@element-plus/icons-vue'
import { ElImageViewer } from 'element-plus'
import { getFilePage, uploadFile, deleteFile, type FileDto } from '@/api/file'
import service from '@/utils/request'

const fileList = ref<FileDto[]>([])
const tableLoading = ref(false)
const pageNumber = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const searchForm = reactive({
  originalName: '',
})
const uploadDialogVisible = ref(false)
const previewVisible = ref(false)
const previewUrls = ref<string[]>([])
let hasUploaded = false

async function fetchList() {
  tableLoading.value = true
  try {
    const res = await getFilePage({
      pageNumber: pageNumber.value,
      pageSize: pageSize.value,
      originalName: searchForm.originalName || undefined,
    })
    fileList.value = res.items
    totalCount.value = res.totalCount
  } finally {
    tableLoading.value = false
  }
}

function handleSearch() {
  pageNumber.value = 1
  fetchList()
}

function handleReset() {
  searchForm.originalName = ''
  pageNumber.value = 1
  fetchList()
}

function handleSizeChange() {
  pageNumber.value = 1
  fetchList()
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

const IMAGE_EXTS = new Set(['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.svg'])

function isImage(ext: string): boolean {
  return IMAGE_EXTS.has(ext.toLowerCase())
}

// ========== 上传 ==========

async function handleUpload(options: { file: File; onSuccess: (res: unknown) => void; onError: (err: Error) => void }) {
  try {
    const res = await uploadFile(options.file)
    options.onSuccess(res)
  } catch (e) {
    options.onError(e as Error)
  }
}

function handleUploadSuccess() {
  hasUploaded = true
}

function handleUploadClosed() {
  if (hasUploaded) {
    hasUploaded = false
    fetchList()
  }
}

// ========== 下载 ==========

async function handleDownload(row: FileDto) {
  const blob = await service.get(`/file/download/${row.id}`, { responseType: 'blob' }) as unknown as Blob
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = row.originalName
  link.click()
  URL.revokeObjectURL(link.href)
}

// ========== 预览 ==========

async function handlePreview(row: FileDto) {
  const blob = await service.get(`/file/preview/${row.id}`, { responseType: 'blob' }) as unknown as Blob
  previewUrls.value = [URL.createObjectURL(blob)]
  previewVisible.value = true
}

// ========== 删除 ==========

async function handleDelete(row: FileDto) {
  try {
    await ElMessageBox.confirm(`确定要删除文件「${row.originalName}」吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
    await deleteFile(row.id)
    ElMessage.success('删除成功')
    await fetchList()
  } catch {
    // 取消或失败
  }
}

onMounted(fetchList)
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.page-header h2 {
  margin: 0;
  font-size: 18px;
}

.search-bar {
  margin-bottom: 16px;
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: auto;
  padding-top: 16px;
}
</style>
