import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/Layout.vue'
import Login from '@/pages/Login.vue'
import Budget from '@/pages/Budget.vue'
import History from '@/pages/History.vue'
import Settings from '@/pages/Settings.vue'
import Import from '@/pages/Import.vue'
import PendingExpenses from '@/pages/PendingExpenses.vue'
import { useAuthStore } from '@/stores/auth'

// Pages are imported eagerly (not lazy) so the initial navigation resolves
// synchronously and the Layout/header render on first paint without an
// extra network round-trip for a dynamically-imported chunk.
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: Login },
    {
      path: '/',
      component: Layout,
      children: [
        { path: '', redirect: '/budget' },
        { path: 'budget', name: 'budget', component: Budget },
        { path: 'history', name: 'history', component: History },
        { path: 'accounts', redirect: '/settings' },
        { path: 'settings', name: 'settings', component: Settings },
        { path: 'import', name: 'import', component: Import },
        { path: 'pending-expenses', name: 'pending-expenses', component: PendingExpenses },
        { path: ':pathMatch(.*)*', redirect: '/budget' },
      ],
    },
  ],
})

// Landing page becomes the login page for anonymous users: any navigation to a
// protected route is redirected to /login until MSAL reports an authenticated
// account, and an already-authenticated user visiting /login is sent to /budget.
router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  await authStore.initialize()

  if (!authStore.isAuthenticated && to.name !== 'login') {
    return { name: 'login' }
  }

  if (authStore.isAuthenticated && to.name === 'login') {
    return '/budget'
  }
})

export default router
