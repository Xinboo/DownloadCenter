<template>
  <div class="user-manage">
    <div class="page-header">
      <h2>用户管理</h2>
      <el-button type="primary" @click="openCreate">新建用户</el-button>
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
      <el-table-column prop="phone" label="手机" width="160" />
      <el-table-column prop="createdAt" label="创建时间" width="180" />
      <el-table-column min-width="1" />
      <el-table-column label="操作" width="100" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" text size="small" @click="openEdit(row)" style="padding: 0 4px">编辑</el-button>
          <el-button type="danger" text size="small" @click="handleDelete(row)" style="margin-left: 4px; padding: 0 4px">删除</el-button>
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

    <!-- 新建 / 编辑用户 Dialog -->
    <el-dialog v-model="dialogVisible" :title="isEdit ? '编辑用户' : '新建用户'" width="480px" @closed="resetForm">
      <el-form
        ref="formRef"
        :model="form"
        :rules="currentRules"
        label-position="top"
        @submit.prevent="handleSubmit"
      >
        <template v-if="!isEdit">
          <el-form-item label="用户名" prop="userName">
            <el-input v-model="form.userName" placeholder="登录用户名" />
          </el-form-item>
          <el-form-item label="密码" prop="password">
            <el-input v-model="form.password" type="password" show-password placeholder="至少 6 位" />
          </el-form-item>
        </template>
        <el-form-item label="昵称" prop="nickName">
          <el-input v-model="form.nickName" placeholder="显示名称" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role" style="width: 100%">
            <el-option label="管理员" value="Admin" />
            <el-option label="用户" value="User" />
          </el-select>
        </el-form-item>
        <el-form-item label="公司">
          <el-input v-model="form.company" placeholder="选填" />
        </el-form-item>
        <el-form-item label="手机">
          <el-input v-model="form.phone" placeholder="选填" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          {{ isEdit ? '确认修改' : '确认创建' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { getUserPage, createUser, updateUser, deleteUser, type UserDto } from '@/api/user'
import { UserRole } from '@/enums/user'

const userList = ref<UserDto[]>([])
const tableLoading = ref(false)
const pageNumber = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const searchForm = reactive({
  userName: '',
  nickName: '',
})
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const editingId = ref<string | null>(null)

const isEdit = computed(() => !!editingId.value)

const form = reactive({
  userName: '',
  password: '',
  nickName: '',
  role: UserRole.User as string,
  company: '',
  phone: '',
})

const createRules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
}

const editRules: FormRules = {
  nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
}

const currentRules = computed(() => (isEdit.value ? editRules : createRules))

async function fetchList() {
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
  fetchList()
}

function handleReset() {
  searchForm.userName = ''
  searchForm.nickName = ''
  pageNumber.value = 1
  fetchList()
}

function handleSizeChange() {
  pageNumber.value = 1
  fetchList()
}

function resetForm() {
  editingId.value = null
  form.userName = ''
  form.password = ''
  form.nickName = ''
  form.role = UserRole.User
  form.company = ''
  form.phone = ''
  formRef.value?.clearValidate()
}

function openCreate() {
  editingId.value = null
  dialogVisible.value = true
}

function openEdit(row: UserDto) {
  editingId.value = row.id
  form.nickName = row.nickName
  form.role = row.role
  form.company = row.company ?? ''
  form.phone = row.phone ?? ''
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      if (isEdit.value) {
        await updateUser(editingId.value!, {
          nickName: form.nickName,
          role: form.role as UserRole,
          company: form.company || undefined,
          phone: form.phone || undefined,
        })
        ElMessage.success('修改成功')
      } else {
        await createUser({
          userName: form.userName,
          password: form.password,
          nickName: form.nickName,
          role: form.role as UserRole,
          company: form.company || undefined,
          phone: form.phone || undefined,
        })
        ElMessage.success('用户创建成功')
      }
      dialogVisible.value = false
      await fetchList()
    } catch {
      // 失败提示已由 request 拦截器统一弹出
    } finally {
      submitLoading.value = false
    }
  })
}

async function handleDelete(row: UserDto) {
  try {
    await ElMessageBox.confirm(`确定要删除用户「${row.nickName}」吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
    await deleteUser(row.id)
    ElMessage.success('删除成功')
    await fetchList()
  } catch {
    // 取消或失败，不处理
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
