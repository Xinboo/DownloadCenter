<template>
  <el-container class="layout-container">
    <el-header class="layout-header" height="60px">
      <LayoutHeader />
    </el-header>
    <el-container style="overflow: hidden">
      <el-aside v-if="isAdmin" class="layout-aside" width="240px">
        <LayoutAside />
      </el-aside>
      <el-main class="layout-main">
        <LayoutMain />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { UserRole } from '@/enums/user'
import LayoutHeader from './components/LayoutHeader.vue'
import LayoutAside from './components/LayoutAside.vue'
import LayoutMain from './components/LayoutMain.vue'

const authStore = useAuthStore()
const isAdmin = computed(() => authStore.userInfo?.role === UserRole.Admin)
</script>

<style scoped>
.layout-container {
  height: 100vh;
}
.layout-header {
  background-color: #303030;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 10;
}
.layout-main {
  display: flex;
  flex-direction: column;
}
.layout-main :deep(> *) {
  display: flex;
  flex-direction: column;
  flex: 1;
}
</style>
