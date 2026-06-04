import { createRouter, createWebHistory } from 'vue-router'

import UserLogin from '@/views/auth/UserLogin.vue'
import SetupAdmin from '@/views/auth/SetupAdmin.vue'
import LayoutIndex from '@/layout/LayoutIndex.vue'
import ProductHome from '@/views/product/ProductHome.vue'
import { setupGuard } from './guard'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      component: UserLogin,
    },
    {
      path: '/setup',
      component: SetupAdmin,
    },
    {
      path: '/',
      component: LayoutIndex,
      redirect: '/home',
      children: [
        { path: 'home', component: ProductHome },
        { path: 'admin/product', component: () => import('@/views/admin/ProductManage.vue') },
        { path: 'admin/user', component: () => import('@/views/admin/UserManage.vue') },
        { path: 'admin/access', component: () => import('@/views/admin/AccessManage.vue') },
        { path: 'admin/file', component: () => import('@/views/admin/FileManage.vue') },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/error/NotFound.vue'),
    },
  ],
})

setupGuard(router)

export default router
