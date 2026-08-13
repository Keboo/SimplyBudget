import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/components/Layout.vue'
import Budget from '@/pages/Budget.vue'
import History from '@/pages/History.vue'
import Accounts from '@/pages/Accounts.vue'
import Settings from '@/pages/Settings.vue'
import Import from '@/pages/Import.vue'

// Pages are imported eagerly (not lazy) so the initial navigation resolves
// synchronously and the Layout/header render on first paint without an
// extra network round-trip for a dynamically-imported chunk.
const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: Layout,
      children: [
        { path: '', redirect: '/budget' },
        { path: 'budget', name: 'budget', component: Budget },
        { path: 'history', name: 'history', component: History },
        { path: 'accounts', name: 'accounts', component: Accounts },
        { path: 'settings', name: 'settings', component: Settings },
        { path: 'import', name: 'import', component: Import },
        { path: ':pathMatch(.*)*', redirect: '/budget' },
      ],
    },
  ],
})

export default router
