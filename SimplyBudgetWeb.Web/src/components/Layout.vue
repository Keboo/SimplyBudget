<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useTheme } from 'vuetify'
import { useAuthStore } from '@/stores/auth'

const THEME_STORAGE_KEY = 'SimplyBudgetWebTheme'

const router = useRouter()
const authStore = useAuthStore()
const theme = useTheme()

const isDark = computed(() => theme.global.name.value === 'dark')

const navItems = [
  { title: 'Budget', to: '/budget', icon: 'mdi-cash-multiple' },
  { title: 'History', to: '/history', icon: 'mdi-history' },
  { title: 'Accounts', to: '/accounts', icon: 'mdi-bank' },
  { title: 'Settings', to: '/settings', icon: 'mdi-cog' },
  { title: 'Import', to: '/import', icon: 'mdi-file-import' },
]

function toggleTheme() {
  const newMode = isDark.value ? 'light' : 'dark'
  theme.global.name.value = newMode
  localStorage.setItem(THEME_STORAGE_KEY, newMode)
}
</script>

<template>
  <v-app-bar>
    <v-toolbar-title
      style="cursor: pointer;"
      @click="router.push('/budget')"
    >
      Simply Budget
    </v-toolbar-title>

    <v-spacer />

    <v-btn
      icon
      :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
      @click="toggleTheme"
    >
      <v-icon>{{ isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night' }}</v-icon>
    </v-btn>

    <template v-if="authStore.isAuthenticated">
      <span v-if="authStore.account?.name" class="mx-2">{{ authStore.account.name }}</span>
      <v-btn @click="authStore.logout()">Sign out</v-btn>
    </template>
    <v-btn v-else @click="authStore.login()">Sign in</v-btn>
  </v-app-bar>

  <v-navigation-drawer
    v-if="authStore.isAuthenticated"
    expand-on-hover
    rail
    permanent
  >
    <v-list density="compact" nav>
      <v-list-item
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
      />
    </v-list>
  </v-navigation-drawer>

  <v-main>
    <v-container class="py-6">
      <router-view />
    </v-container>
  </v-main>

  <v-footer app class="justify-center">
    <span class="text-body-2 text-medium-emphasis">&copy; {{ new Date().getFullYear() }} Simply Budget</span>
  </v-footer>
</template>
