<template>
  <div class="setup-container">
    <el-card class="setup-card">
      <template #header>
        <div class="setup-header">
          <h2>系统初始化</h2>
          <p>创建管理员账号</p>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @submit.prevent="handleSubmit"
      >
        <el-form-item label="用户名" prop="userName">
          <el-input v-model="form.userName" :prefix-icon="User" placeholder="登录用户名" />
        </el-form-item>

        <el-form-item label="密码" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            :prefix-icon="Lock"
            placeholder="至少 6 位"
          />
        </el-form-item>

        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            v-model="form.confirmPassword"
            type="password"
            show-password
            :prefix-icon="Lock"
            placeholder="再次输入密码"
          />
        </el-form-item>

        <el-form-item label="昵称" prop="nickName">
          <el-input v-model="form.nickName" :prefix-icon="Avatar" placeholder="显示名称" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" native-type="submit" :loading="loading" style="width: 100%">
            创建并进入
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Lock, Avatar } from '@element-plus/icons-vue'
import { registerAdmin } from '@/api/user'
import { markSystemInitialized } from '@/router/guard'

const router = useRouter()

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({
  userName: '',
  password: '',
  confirmPassword: '',
  nickName: '',
})

const rules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== form.password) callback(new Error('两次密码不一致'))
        else callback()
      },
      trigger: 'blur',
    },
  ],
  nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
}

async function handleSubmit() {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      await registerAdmin({
        userName: form.userName,
        password: form.password,
        nickName: form.nickName,
      })
      ElMessage.success('管理员创建成功，请登录')
      markSystemInitialized()
      router.push('/login')
    } catch {
      // 失败提示已由 request 拦截器统一弹出
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
.setup-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #e4ecfb 100%);
}

.setup-card {
  width: 400px;
}

.setup-header {
  text-align: center;
}

.setup-header h2 {
  margin: 0 0 8px;
  font-size: 20px;
}

.setup-header p {
  margin: 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
