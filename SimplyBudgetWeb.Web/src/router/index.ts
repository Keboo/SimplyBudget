import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/Layout.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: Layout,
      children: [
        { path: '', redirect: '/budget' },
        { path: 'budget', name: 'budget', component: () => import('@/pages/Budget.vue') },
        { path: 'history', name: 'history', component: () => import('@/pages/History.vue') },
        { path: 'accounts', name: 'accounts', component: () => import('@/pages/Accounts.vue') },
        { path: 'settings', name: 'settings', component: () => import('@/pages/Settings.vue') },
        { path: 'import', name: 'import', component: () => import('@/pages/Import.vue') },
        { path: ':pathMatch(.*)*', redirect: '/budget' },
      ],
    },
  ],
})

export default router
