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

function toggleTheme() {
  const newMode = isDark.value ? 'light' : 'dark'
  theme.global.name.value = newMode
  localStorage.setItem(THEME_STORAGE_KEY, newMode)
}
</script>

<template>
  <v-app-bar>
    <v-toolbar-title
      style="flex: 0 0 auto; margin-right: 24px; cursor: pointer;"
      @click="router.push('/budget')"
    >
      Simply Budget
    </v-toolbar-title>

    <div v-if="authStore.isAuthenticated" class="d-flex" style="gap: 4px; flex-grow: 1;">
      <v-btn @click="router.push('/budget')">Budget</v-btn>
      <v-btn @click="router.push('/history')">History</v-btn>
      <v-btn @click="router.push('/accounts')">Accounts</v-btn>
      <v-btn @click="router.push('/settings')">Settings</v-btn>
      <v-btn @click="router.push('/import')">Import</v-btn>
    </div>
    <v-spacer v-else />

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

  <v-main>
    <v-container class="py-6">
      <router-view />
    </v-container>
  </v-main>

  <v-footer app class="justify-center">
    <span class="text-body-2 text-medium-emphasis">&copy; {{ new Date().getFullYear() }} Simply Budget</span>
  </v-footer>
</template>
