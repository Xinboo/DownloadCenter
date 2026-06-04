<template>
  <div class="product-home">
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

    <div v-loading="loading">
      <div class="card-grid" v-if="productList.length > 0">
        <div v-for="product in productList" :key="product.id">
          <div class="product-card" @click="openDetail(product)">
            <div class="card-cover">
              <el-image
                v-if="coverUrls.get(product.id)"
                :src="coverUrls.get(product.id)"
                fit="cover"
                class="cover-img"
              />
              <div v-else class="cover-placeholder">
                <el-icon :size="40"><Box /></el-icon>
              </div>
            </div>
            <div class="card-body">
              <div class="card-name">{{ product.name }}</div>
              <div class="card-model">{{ product.model }}</div>
              <div class="card-desc" :title="product.description ?? ''">{{ product.description || '暂无描述' }}</div>
              <div class="card-meta">
                <el-icon><Paperclip /></el-icon>
                <span>{{ product.fileCount }} 个资源文件</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <el-empty v-else-if="!loading" description="暂无产品" />
    </div>

    <div class="pagination-wrapper" v-if="totalCount > 0">
      <el-pagination
        v-model:current-page="pageNumber"
        v-model:page-size="pageSize"
        :total="totalCount"
        :page-sizes="[12, 24, 48]"
        layout="total, sizes, prev, pager, next"
        @current-change="fetchList"
        @size-change="handleSizeChange"
      />
    </div>

    <!-- 产品详情 Drawer -->
    <el-drawer
      v-model="drawerVisible"
      :title="drawerProduct?.name ?? ''"
      size="600px"
    >
      <div v-loading="drawerLoading" class="drawer-content">
        <!-- 产品信息 -->
        <div class="detail-header">
          <el-image
            v-if="drawerCoverUrl"
            :src="drawerCoverUrl"
            fit="cover"
            class="detail-cover"
          />
          <div v-else class="detail-cover-placeholder">
            <el-icon :size="48"><Box /></el-icon>
          </div>
          <div class="detail-info">
            <h3>{{ drawerProduct?.name }}</h3>
            <div class="detail-model">型号：{{ drawerProduct?.model }}</div>
            <div class="detail-desc">{{ drawerProduct?.description || '暂无描述' }}</div>
          </div>
        </div>

        <!-- 资源列表 -->
        <div class="detail-section-title">资源文件</div>
        <el-form inline class="resource-search">
          <el-form-item>
            <el-input v-model="resourceSearch" placeholder="搜索文件名" clearable @keyup.enter="handleResourceSearch" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleResourceSearch">搜索</el-button>
          </el-form-item>
        </el-form>
        <el-table :data="resourceList" stripe v-loading="resourceLoading">
          <el-table-column prop="displayName" label="文件名" min-width="160" show-overflow-tooltip />
          <el-table-column prop="description" label="描述" min-width="120" show-overflow-tooltip />
          <el-table-column prop="extension" label="类型" width="70" />
          <el-table-column label="大小" width="90">
            <template #default="{ row }">{{ formatSize(row.size) }}</template>
          </el-table-column>
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ row }">
              <el-button type="primary" text size="small" @click="handleDownload(row)">下载</el-button>
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
            @current-change="fetchResources"
            @size-change="handleResourceSizeChange"
          />
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { Box, Paperclip } from '@element-plus/icons-vue'
import {
  getProductPage, getResourcePage,
  type ProductDto, type ProductFileDto,
} from '@/api/product'
import service from '@/utils/request'

// ========== 产品列表 ==========

const productList = ref<ProductDto[]>([])
const loading = ref(false)
const pageNumber = ref(1)
const pageSize = ref(12)
const totalCount = ref(0)
const searchForm = reactive({
  name: '',
  model: '',
})
const coverUrls = ref(new Map<number, string>())

async function fetchList() {
  loading.value = true
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
    loading.value = false
  }
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

// ========== 产品详情 Drawer ==========

const drawerVisible = ref(false)
const drawerLoading = ref(false)
const drawerProduct = ref<ProductDto | null>(null)
const drawerCoverUrl = ref<string | null>(null)
const resourceList = ref<ProductFileDto[]>([])
const resourceLoading = ref(false)
const resourcePageNumber = ref(1)
const resourcePageSize = ref(10)
const resourceTotalCount = ref(0)
const resourceSearch = ref('')

async function openDetail(product: ProductDto) {
  drawerProduct.value = product
  drawerCoverUrl.value = coverUrls.value.get(product.id) ?? null
  drawerVisible.value = true
  drawerLoading.value = true
  resourceSearch.value = ''
  resourcePageNumber.value = 1
  try {
    await fetchResources()
  } finally {
    drawerLoading.value = false
  }
}

async function fetchResources() {
  if (!drawerProduct.value) return
  resourceLoading.value = true
  try {
    const res = await getResourcePage(drawerProduct.value.id, {
      pageNumber: resourcePageNumber.value,
      pageSize: resourcePageSize.value,
      displayName: resourceSearch.value || undefined,
    })
    resourceList.value = res.items
    resourceTotalCount.value = res.totalCount
  } finally {
    resourceLoading.value = false
  }
}

function handleResourceSearch() {
  resourcePageNumber.value = 1
  fetchResources()
}

function handleResourceSizeChange() {
  resourcePageNumber.value = 1
  fetchResources()
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
  padding-top: 20px;
}

/* ========== 卡片网格 ========== */

.card-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 20px;
}

@media (max-width: 1400px) {
  .card-grid { grid-template-columns: repeat(4, 1fr); }
}
@media (max-width: 1100px) {
  .card-grid { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 768px) {
  .card-grid { grid-template-columns: repeat(2, 1fr); }
}

/* ========== 卡片 ========== */

.product-card {
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.25s ease;
  background: #fff;
}

.product-card:hover {
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.1);
  transform: translateY(-3px);
}

.card-cover {
  height: 140px;
  background: #f5f7fa;
  overflow: hidden;
}

.cover-img {
  width: 100%;
  height: 100%;
}

.cover-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #c0c4cc;
}

.card-body {
  padding: 10px 12px;
}

.card-name {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-model {
  font-size: 13px;
  color: #909399;
  margin-top: 4px;
}

.card-desc {
  font-size: 13px;
  color: #a8abb2;
  margin-top: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-meta {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-top: 10px;
  font-size: 13px;
  color: #409eff;
}

/* ========== 详情 Drawer ========== */

.detail-header {
  display: flex;
  gap: 20px;
  margin-bottom: 24px;
}

.detail-cover {
  width: 160px;
  height: 120px;
  border-radius: 8px;
  flex-shrink: 0;
}

.detail-cover-placeholder {
  width: 160px;
  height: 120px;
  border-radius: 8px;
  background: #f5f7fa;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #c0c4cc;
  flex-shrink: 0;
}

.detail-info {
  flex: 1;
  min-width: 0;
}

.detail-info h3 {
  margin: 0 0 8px;
  font-size: 18px;
  color: #303133;
}

.detail-model {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.detail-desc {
  font-size: 14px;
  color: #606266;
  line-height: 1.6;
}

.detail-section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #ebeef5;
}

.resource-search {
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

.drawer-content {
  display: flex;
  flex-direction: column;
  flex: 1;
}
</style>
