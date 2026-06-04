<template>
  <div class="product-manage">
    <div class="page-header">
      <h2>产品管理</h2>
      <el-button type="primary" @click="openCreate">新建产品</el-button>
    </div>

    <el-form :model="searchForm" inline class="search-bar">
      <el-form-item label="产品名称">
        <el-input v-model="searchForm.name" placeholder="搜索产品名称" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item label="型号">
        <el-input v-model="searchForm.model" placeholder="搜索型号" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">搜索</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="productList" v-loading="tableLoading" stripe>
      <el-table-column label="封面" width="80">
        <template #default="{ row }">
          <el-image
            v-if="coverUrls.get(row.id)"
            :src="coverUrls.get(row.id)"
            fit="cover"
            class="cover-thumb"
          />
          <div v-else class="cover-placeholder">
            <el-icon><Picture /></el-icon>
          </div>
        </template>
      </el-table-column>
      <el-table-column prop="name" label="产品名称" width="160" />
      <el-table-column prop="model" label="型号" width="140" />
      <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
      <el-table-column prop="fileCount" label="资源数" width="80" align="center" />
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column min-width="1" />
      <el-table-column label="操作" width="160" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" text size="small" style="padding: 0 4px" @click="openEdit(row)">编辑</el-button>
          <el-button type="success" text size="small" style="margin-left: 4px; padding: 0 4px" @click="openResourceDrawer(row)">资源</el-button>
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

    <!-- 新建 / 编辑产品 Dialog -->
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑产品' : '新建产品'" width="520px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="formRules" label-position="top" @submit.prevent="handleSubmit">
        <el-form-item label="产品名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入产品名称" />
        </el-form-item>
        <el-form-item label="型号" prop="model">
          <el-input v-model="form.model" placeholder="请输入型号" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="选填" />
        </el-form-item>
        <el-form-item label="封面图">
          <div v-if="coverPreviewUrl" class="cover-preview-wrapper">
            <el-image :src="coverPreviewUrl" fit="cover" class="cover-preview" />
            <el-button text type="danger" size="small" @click="clearCover">删除封面</el-button>
          </div>
          <el-upload
            v-else
            :http-request="handleCoverUpload"
            :show-file-list="false"
            accept="image/*"
            v-loading="coverUploading"
          >
            <el-button type="primary" plain>上传封面</el-button>
          </el-upload>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          {{ isEdit ? '确认修改' : '确认创建' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 资源管理 Drawer -->
    <el-drawer
      v-model="resourceDrawerVisible"
      :title="`${drawerProduct?.name ?? ''} - 资源管理`"
      size="640px"
    >
      <div class="resource-header">
        <el-input v-model="resourceSearchName" placeholder="搜索文件名" clearable style="width: 200px" @keyup.enter="handleResourceSearch" />
        <div>
          <el-button type="primary" size="small" @click="handleResourceSearch">搜索</el-button>
          <el-button type="primary" size="small" @click="openPublishDialog">发布资源</el-button>
        </div>
      </div>
      <el-table :data="resourceList" v-loading="resourceLoading" stripe>
        <el-table-column prop="displayName" label="文件名" min-width="140" show-overflow-tooltip />
        <el-table-column prop="description" label="描述" min-width="120" show-overflow-tooltip />
        <el-table-column prop="extension" label="类型" width="70" />
        <el-table-column label="大小" width="90">
          <template #default="{ row }">{{ formatSize(row.size) }}</template>
        </el-table-column>
        <el-table-column prop="createdAt" label="上传时间" width="170" />
        <el-table-column label="操作" width="110" align="center">
          <template #default="{ row }">
            <el-button type="primary" text size="small" style="padding: 0 4px" @click="handleDownload(row)">下载</el-button>
            <el-button type="danger" text size="small" style="margin-left: 4px; padding: 0 4px" @click="handleDeleteResource(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="resource-pagination" v-if="resourceTotalCount > 0">
        <el-pagination
          v-model:current-page="resourcePageNumber"
          v-model:page-size="resourcePageSize"
          :total="resourceTotalCount"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          small
          @current-change="fetchResources(drawerProduct!.id)"
          @size-change="handleResourceSizeChange"
        />
      </div>
    </el-drawer>

    <!-- 发布资源 Dialog -->
    <el-dialog v-model="publishDialogVisible" title="发布资源" width="480px" append-to-body @closed="resetPublishForm">
      <el-form ref="publishFormRef" :model="publishForm" :rules="publishRules" label-position="top" @submit.prevent="handlePublishSubmit">
        <el-form-item label="选择文件" prop="fileId">
          <div>
            <el-upload
              v-if="!uploadedFileName"
              :http-request="handleResourceUpload"
              :show-file-list="false"
              v-loading="resourceUploading"
            >
              <el-button type="primary" plain>上传文件</el-button>
            </el-upload>
            <div v-else class="uploaded-file">
              <el-icon><Document /></el-icon>
              <span>{{ uploadedFileName }}</span>
              <el-button text type="danger" size="small" @click="clearUploadedFile">移除</el-button>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="publishForm.displayName" placeholder="文件在产品中的显示名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="publishForm.description" type="textarea" :rows="2" placeholder="版本说明、修复内容等" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="publishDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="publishLoading" @click="handlePublishSubmit">确认发布</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Picture, Document } from '@element-plus/icons-vue'
import {
  getProductPage, getResourcePage, createProduct, updateProduct, deleteProduct,
  publishResource, deleteResource,
  type ProductDto, type ProductFileDto,
} from '@/api/product'
import { uploadFile } from '@/api/file'
import service from '@/utils/request'

// ========== 产品列表 ==========

const productList = ref<ProductDto[]>([])
const tableLoading = ref(false)
const pageNumber = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const searchForm = reactive({
  name: '',
  model: '',
})
const coverUrls = ref(new Map<number, string>())

async function fetchList() {
  tableLoading.value = true
  try {
    const res = await getProductPage({
      pageNumber: pageNumber.value,
      pageSize: pageSize.value,
      name: searchForm.name || undefined,
      model: searchForm.model || undefined,
    })
    productList.value = res.items
    totalCount.value = res.totalCount
    await fetchCoverUrls(productList.value)
  } finally {
    tableLoading.value = false
  }
}

function handleSearch() {
  pageNumber.value = 1
  fetchList()
}

function handleReset() {
  searchForm.name = ''
  searchForm.model = ''
  pageNumber.value = 1
  fetchList()
}

function handleSizeChange() {
  pageNumber.value = 1
  fetchList()
}

async function fetchCoverUrls(products: ProductDto[]) {
  coverUrls.value.forEach((url) => URL.revokeObjectURL(url))
  const map = new Map<number, string>()

  const withCovers = products.filter((p) => p.coverFileId)
  const results = await Promise.allSettled(
    withCovers.map(async (p) => {
      const blob = await service.get(`/product/${p.id}/cover`, { responseType: 'blob' }) as unknown as Blob
      return { id: p.id, url: URL.createObjectURL(blob) }
    }),
  )
  for (const r of results) {
    if (r.status === 'fulfilled') {
      map.set(r.value.id, r.value.url)
    }
  }
  coverUrls.value = map
}

// ========== 新建 / 编辑产品 ==========

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const editingId = ref<number | null>(null)
const isEdit = computed(() => editingId.value !== null)
const coverUploading = ref(false)
const coverPreviewUrl = ref<string | null>(null)

const form = reactive({
  name: '',
  model: '',
  description: '',
  coverFileId: null as number | null,
})

const formRules: FormRules = {
  name: [{ required: true, message: '请输入产品名称', trigger: 'blur' }],
  model: [{ required: true, message: '请输入型号', trigger: 'blur' }],
}

function openCreate() {
  editingId.value = null
  dialogVisible.value = true
}

function openEdit(row: ProductDto) {
  editingId.value = row.id
  form.name = row.name
  form.model = row.model
  form.description = row.description ?? ''
  form.coverFileId = row.coverFileId

  if (row.coverFileId && coverUrls.value.has(row.id)) {
    coverPreviewUrl.value = coverUrls.value.get(row.id)!
  } else {
    coverPreviewUrl.value = null
  }

  dialogVisible.value = true
}

function resetForm() {
  editingId.value = null
  form.name = ''
  form.model = ''
  form.description = ''
  form.coverFileId = null
  coverPreviewUrl.value = null
  formRef.value?.clearValidate()
}

async function handleCoverUpload(options: { file: File }) {
  coverUploading.value = true
  try {
    const { fileId } = await uploadFile(options.file)
    form.coverFileId = fileId
    coverPreviewUrl.value = URL.createObjectURL(options.file)
  } catch {
    // 错误已由拦截器弹出
  } finally {
    coverUploading.value = false
  }
}

function clearCover() {
  form.coverFileId = null
  coverPreviewUrl.value = null
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      const data = {
        name: form.name,
        model: form.model,
        description: form.description || undefined,
        coverFileId: form.coverFileId ?? undefined,
      }
      if (isEdit.value) {
        await updateProduct(editingId.value!, data)
        ElMessage.success('修改成功')
      } else {
        await createProduct(data)
        ElMessage.success('创建成功')
      }
      dialogVisible.value = false
      await fetchList()
    } catch {
      // 错误已由拦截器弹出
    } finally {
      submitLoading.value = false
    }
  })
}

// ========== 删除产品 ==========

async function handleDelete(row: ProductDto) {
  try {
    await ElMessageBox.confirm(`确定要删除产品「${row.name}」吗？该操作将同时删除所有关联资源文件。`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
    await deleteProduct(row.id)
    ElMessage.success('删除成功')
    await fetchList()
  } catch {
    // 取消或失败
  }
}

// ========== 资源管理 Drawer ==========

const resourceDrawerVisible = ref(false)
const drawerProduct = ref<ProductDto | null>(null)
const resourceList = ref<ProductFileDto[]>([])
const resourceLoading = ref(false)
const resourcePageNumber = ref(1)
const resourcePageSize = ref(10)
const resourceTotalCount = ref(0)
const resourceSearchName = ref('')

function openResourceDrawer(row: ProductDto) {
  drawerProduct.value = row
  resourceSearchName.value = ''
  resourcePageNumber.value = 1
  resourceDrawerVisible.value = true
  fetchResources(row.id)
}

async function fetchResources(productId: number) {
  resourceLoading.value = true
  try {
    const res = await getResourcePage(productId, {
      pageNumber: resourcePageNumber.value,
      pageSize: resourcePageSize.value,
      displayName: resourceSearchName.value || undefined,
    })
    resourceList.value = res.items
    resourceTotalCount.value = res.totalCount
  } finally {
    resourceLoading.value = false
  }
}

function handleResourceSearch() {
  resourcePageNumber.value = 1
  if (drawerProduct.value) fetchResources(drawerProduct.value.id)
}

function handleResourceSizeChange() {
  resourcePageNumber.value = 1
  if (drawerProduct.value) fetchResources(drawerProduct.value.id)
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

async function handleDownload(row: ProductFileDto) {
  const pid = drawerProduct.value!.id
  const blob = await service.get(`/product/${pid}/resource/${row.fileId}/download`, {
    responseType: 'blob',
  }) as unknown as Blob
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = row.displayName + row.extension
  link.click()
  URL.revokeObjectURL(link.href)
}

async function handleDeleteResource(row: ProductFileDto) {
  const pid = drawerProduct.value!.id
  try {
    await ElMessageBox.confirm(`确定要删除资源「${row.displayName}」吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
    await deleteResource(pid, row.fileId)
    ElMessage.success('删除成功')
    await fetchResources(pid)
    await fetchList()
  } catch {
    // 取消或失败
  }
}

// ========== 发布资源 Dialog ==========

const publishDialogVisible = ref(false)
const publishLoading = ref(false)
const resourceUploading = ref(false)
const publishFormRef = ref<FormInstance>()
const uploadedFileName = ref('')

const publishForm = reactive({
  fileId: null as number | null,
  displayName: '',
  description: '',
})

const publishRules: FormRules = {
  fileId: [{ required: true, message: '请上传文件', validator: (_rule, _value, callback) => {
    if (!publishForm.fileId) callback(new Error('请上传文件'))
    else callback()
  }}],
  displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
  description: [{ required: true, message: '请输入描述', trigger: 'blur' }],
}

function openPublishDialog() {
  publishDialogVisible.value = true
}

function resetPublishForm() {
  publishForm.fileId = null
  publishForm.displayName = ''
  publishForm.description = ''
  uploadedFileName.value = ''
  publishFormRef.value?.clearValidate()
}

async function handleResourceUpload(options: { file: File }) {
  resourceUploading.value = true
  try {
    const { fileId } = await uploadFile(options.file)
    publishForm.fileId = fileId
    uploadedFileName.value = options.file.name
    publishFormRef.value?.validateField('fileId')
  } catch {
    // 错误已由拦截器弹出
  } finally {
    resourceUploading.value = false
  }
}

function clearUploadedFile() {
  publishForm.fileId = null
  uploadedFileName.value = ''
}

async function handlePublishSubmit() {
  if (!publishFormRef.value) return
  await publishFormRef.value.validate(async (valid) => {
    if (!valid) return
    publishLoading.value = true
    try {
      await publishResource(drawerProduct.value!.id, publishForm.fileId!, {
        displayName: publishForm.displayName,
        description: publishForm.description,
      })
      ElMessage.success('发布成功')
      publishDialogVisible.value = false
      await fetchResources(drawerProduct.value!.id)
      await fetchList()
    } catch {
      // 错误已由拦截器弹出
    } finally {
      publishLoading.value = false
    }
  })
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

.cover-thumb {
  width: 48px;
  height: 48px;
  border-radius: 4px;
}

.cover-placeholder {
  width: 48px;
  height: 48px;
  border-radius: 4px;
  background: #f5f7fa;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #c0c4cc;
  font-size: 20px;
}

.cover-preview-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
}

.cover-preview {
  width: 120px;
  height: 120px;
  border-radius: 4px;
}

.resource-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.resource-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: auto;
  padding-top: 12px;
}

:deep(.el-drawer__body) {
  display: flex;
  flex-direction: column;
}

.uploaded-file {
  display: flex;
  align-items: center;
  gap: 6px;
}
</style>
