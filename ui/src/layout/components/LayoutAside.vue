<template>
  <el-menu :router="true" :default-active="activeMenu" class="aside-menu">
    <el-menu-item index="/home">
      <el-icon><Box /></el-icon>
      <span>产品中心</span>
    </el-menu-item>

    <template v-if="isAdmin">
      <el-sub-menu index="/admin/product-group">
        <template #title>
          <el-icon><Goods /></el-icon>
          <span>产品管理</span>
        </template>
        <el-menu-item index="/admin/product">产品列表</el-menu-item>
        <el-menu-item index="/admin/access">产品授权</el-menu-item>
      </el-sub-menu>

      <el-menu-item index="/admin/user">
        <el-icon><User /></el-icon>
        <span>用户管理</span>
      </el-menu-item>

      <el-menu-item index="/admin/file">
        <el-icon><FolderOpened /></el-icon>
        <span>文件管理</span>
      </el-menu-item>
    </template>
  </el-menu>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { UserRole } from '@/enums/user'

const route = useRoute()
const authStore = useAuthStore()

const activeMenu = computed(() => route.path)
const isAdmin = computed(() => authStore.userInfo?.role === UserRole.Admin)
</script>

<style scoped>
.aside-menu {
  --el-menu-bg-color: #545c64;
  --el-menu-text-color: #fff;
  --el-menu-hover-bg-color: #434a50;
  --el-menu-active-color: #409eff;
  height: 100%;
}

.aside-menu :deep(.el-menu-item.is-active) {
  background-color: #434a50 !important;
}

</style>
