<template>
  <div class="access-manage">
    <div class="page-header">
      <h2>产品授权</h2>
    </div>

    <el-form :model="searchForm" inline class="search-bar">
      <el-form-item label="用户名">
        <el-input v-model="searchForm.userName" placeholder="搜索用户名" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item label="昵称">
        <el-input v-model="searchForm.nickName" placeholder="搜索昵称" clearable @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">搜索</el-button>
        <el-button @click="handleReset">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="userList" v-loading="tableLoading" stripe>
      <el-table-column prop="userName" label="用户名" width="140" />
      <el-table-column prop="nickName" label="昵称" width="140" />
      <el-table-column prop="role" label="角色" width="100">
        <template #default="{ row }">
          <el-tag :type="row.role === 'Admin' ? '' : 'info'" size="small">
            {{ row.role === 'Admin' ? '管理员' : '用户' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="company" label="公司" width="160" />
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column min-width="1" />
      <el-table-column label="操作" width="100" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" text size="small" @click="openDrawer(row)">配置授权</el-button>
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
        @current-change="fetchUsers"
        @size-change="handleSizeChange"
      />
    </div>

    <!-- 授权配置 Drawer -->
    <el-drawer
      v-model="drawerVisible"
      :title="`${drawerUser?.nickName ?? ''} 的产品授权`"
      size="680px"
    >
      <div v-loading="drawerLoading">
        <div class="access-all-row">
          <span>查看全部产品</span>
          <el-switch v-model="canAccessAll" @change="handleAccessAllChange" />
        </div>

        <template v-if="!canAccessAll">
          <el-transfer
            v-model="authorizedIds"
            :data="transferData"
            filterable
            filter-placeholder="搜索产品"
            :titles="['未授权', '已授权']"
            @change="handleTransferChange"
          />
        </template>

        <div v-else class="access-all-hint">
          <el-icon :size="32" color="#67c23a"><CircleCheckFilled /></el-icon>
          <span>该用户可访问所有产品</span>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { CircleCheckFilled } from '@element-plus/icons-vue'
import { getUserPage, type UserDto } from '@/api/user'
import { getProductList, type ProductDto } from '@/api/product'
import { getAuthorizedProducts, setAccessAll, setAuthorizedProducts } from '@/api/productAccess'

// ========== 用户分页列表 ==========

const userList = ref<UserDto[]>([])
const tableLoading = ref(false)
const pageNumber = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const searchForm = reactive({
  userName: '',
  nickName: '',
})

async function fetchUsers() {
  tableLoading.value = true
  try {
    const res = await getUserPage({
      pageNumber: pageNumber.value,
      pageSize: pageSize.value,
      userName: searchForm.userName || undefined,
      nickName: searchForm.nickName || undefined,
    })
    userList.value = res.items
    totalCount.value = res.totalCount
  } finally {
    tableLoading.value = false
  }
}

function handleSearch() {
  pageNumber.value = 1
  fetchUsers()
}

function handleReset() {
  searchForm.userName = ''
  searchForm.nickName = ''
  pageNumber.value = 1
  fetchUsers()
}

function handleSizeChange() {
  pageNumber.value = 1
  fetchUsers()
}

// ========== Drawer ==========

const drawerVisible = ref(false)
const drawerLoading = ref(false)
const drawerUser = ref<UserDto | null>(null)
const canAccessAll = ref(false)
const authorizedIds = ref<number[]>([])
const transferData = ref<{ key: number; label: string }[]>([])

async function openDrawer(user: UserDto) {
  drawerUser.value = user
  drawerVisible.value = true
  drawerLoading.value = true
  try {
    const [products, auth] = await Promise.all([
      getProductList(),
      getAuthorizedProducts(user.id),
    ])
    transferData.value = products.map((p: ProductDto) => ({
      key: p.id,
      label: `${p.name}（${p.model}）`,
    }))
    canAccessAll.value = auth.canAccessAllProducts
    authorizedIds.value = auth.products.map((p) => p.id)
  } finally {
    drawerLoading.value = false
  }
}

async function handleAccessAllChange(val: boolean | string | number) {
  if (!drawerUser.value) return
  try {
    await setAccessAll(drawerUser.value.id, val as boolean)
    ElMessage.success(val ? '已开启查看全部产品' : '已关闭查看全部产品')
  } catch {
    canAccessAll.value = !val
  }
}

async function handleTransferChange() {
  if (!drawerUser.value) return
  try {
    await setAuthorizedProducts(drawerUser.value.id, authorizedIds.value)
    ElMessage.success('授权已更新')
  } catch {
    // 错误已由拦截器弹出
  }
}

onMounted(fetchUsers)
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

.access-all-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  margin-bottom: 20px;
  background: #f5f7fa;
  border-radius: 8px;
  font-size: 14px;
}

.access-all-hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 60px 0;
  color: #67c23a;
  font-size: 15px;
}

:deep(.el-transfer) {
  display: flex;
  justify-content: center;
}

:deep(.el-transfer-panel) {
  width: 260px;
}

:deep(.el-transfer-panel .el-transfer-panel__body) {
  height: 500px;
}

:deep(.el-transfer__buttons) {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 0 16px;
}

:deep(.el-transfer__buttons .el-button) {
  margin: 0;
}
</style>
