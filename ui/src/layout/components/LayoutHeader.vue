<template>
  <div class="header-content">
    <!-- 左边 logo -->
    <router-link to="/home" class="logo">
      <img src="/favicon.ico" alt="logo" />
      <span>下载中心</span>
    </router-link>

    <!-- 右边用户信息 -->
    <el-dropdown trigger="click" class="user-dropdown">
      <div class="user-info">
        <span class="username">{{ userInfo?.nickName }}</span>
        <el-avatar :size="32" src="/favicon.ico" />
      </div>

      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item disabled>账号：{{ userInfo?.userName }}</el-dropdown-item>
          <el-dropdown-item disabled>名称：{{ userInfo?.nickName }}</el-dropdown-item>
          <el-dropdown-item disabled>角色：{{ userInfo?.role }}</el-dropdown-item>
          <el-dropdown-item divided @click="passwordDialogVisible = true">修改密码</el-dropdown-item>
          <el-dropdown-item @click="handleLogout">退出登录</el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>

    <!-- 修改密码弹窗 -->
    <el-dialog v-model="passwordDialogVisible" title="修改密码" width="420px" @closed="resetForm">
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @submit.prevent="handleSubmit"
      >
        <el-form-item label="旧密码" prop="oldPassword">
          <el-input
            v-model="form.oldPassword"
            type="password"
            show-password
            placeholder="请输入当前密码"
          />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input
            v-model="form.newPassword"
            type="password"
            show-password
            placeholder="至少 6 位"
          />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            v-model="form.confirmPassword"
            type="password"
            show-password
            placeholder="再次输入新密码"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="passwordDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="handleSubmit">确认修改</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { changePassword } from '@/api/user'

const authStore = useAuthStore()
const userInfo = computed(() => authStore.userInfo)

const passwordDialogVisible = ref(false)
const loading = ref(false)
const formRef = ref<FormInstance>()

const form = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const rules: FormRules = {
  oldPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== form.newPassword) callback(new Error('两次密码不一致'))
        else callback()
      },
      trigger: 'blur',
    },
  ],
}

function resetForm() {
  form.oldPassword = ''
  form.newPassword = ''
  form.confirmPassword = ''
  formRef.value?.clearValidate()
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      await changePassword({
        oldPassword: form.oldPassword,
        newPassword: form.newPassword,
        confirmPassword: form.confirmPassword,
      })
      ElMessage.success('密码修改成功，请重新登录')
      authStore.logout()
    } catch {
      // 失败提示已由 request 拦截器统一弹出
    } finally {
      loading.value = false
    }
  })
}

function handleLogout() {
  authStore.logout()
}
</script>

<style scoped>
.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 100%;
  color: rgb(255, 255, 255);
}

.logo {
  display: flex;
  align-items: center;
  gap: 8px;
}

.logo:hover span {
  opacity: 0.8;
}

.logo img {
  height: 32px;
}

.user-dropdown {
  height: 100%;
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  color: #ffffff;
}

.user-info :deep(.el-avatar) {
  --el-avatar-bg-color: #ffffff;
}
</style>
