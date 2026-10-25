<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useDisplay, useTheme } from 'vuetify'
import { useAuthStore } from '@/stores/auth'

const THEME_STORAGE_KEY = 'SimplyBudgetWebTheme'

const route = useRoute()
const authStore = useAuthStore()
const theme = useTheme()
const { xs: phone } = useDisplay()
const moreOpen = ref(false)
const activityOpen = ref(false)
const activityActive = computed(() => ['/history', '/pending-expenses'].includes(route.path))
const moreActive = computed(() => ['/settings', '/import'].includes(route.path))
const taskTitle = computed(() => navItems.find(item => item.to === route.path)?.title ?? 'Simply Budget')

watch(() => route.fullPath, () => {
  moreOpen.value = false
  activityOpen.value = false
})

function monthDestination(path: string) {
  return { path, query: route.query.month ? { month: route.query.month } : {} }
}

const isDark = computed(() => theme.global.name.value === 'dark')

const navItems = [
  { title: 'Budget', to: '/budget', icon: 'mdi-cash-multiple' },
  { title: 'Expenses', to: '/history', icon: 'mdi-history' },
  { title: 'Pending Expenses', to: '/pending-expenses', icon: 'mdi-receipt-text-clock' },
  { title: 'Receipts', to: '/receipts', icon: 'mdi-receipt-text-check' },
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
  <v-app-bar :height="phone ? 56 : 64" :class="{ 'phone-task-header': phone }">
    <v-toolbar-title :aria-live="phone ? 'polite' : undefined">
      <span v-if="phone">{{ taskTitle }}</span>
      <v-btn v-else to="/budget" variant="text" class="text-h6 text-none">Simply Budget</v-btn>
    </v-toolbar-title>

    <template v-if="!phone">
    <v-spacer />

    <v-btn
      icon
      :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
      @click="toggleTheme"
    >
      <v-icon>{{ isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night' }}</v-icon>
    </v-btn>

    <template v-if="authStore.isAuthenticated">
      <span v-if="authStore.displayName" class="mx-2">{{ authStore.displayName }}</span>
      <v-btn @click="authStore.logout()">Sign out</v-btn>
    </template>
    <v-btn v-else @click="authStore.login()">Sign in</v-btn>
    </template>
  </v-app-bar>

  <v-navigation-drawer
    v-if="authStore.isAuthenticated && !phone"
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

  <v-bottom-navigation v-if="authStore.isAuthenticated && phone" grow height="64" class="phone-navigation">
    <v-btn :to="monthDestination('/budget')" :active="route.path === '/budget'" aria-label="Budget">
      <v-icon>mdi-cash-multiple</v-icon><span>Budget</span>
    </v-btn>
    <v-menu v-model="activityOpen" location="top">
      <template #activator="{ props }">
        <v-btn v-bind="props" :active="activityActive" aria-label="Activity">
          <v-icon>mdi-history</v-icon><span>Activity</span>
        </v-btn>
      </template>
      <v-list aria-label="Activity destinations">
        <v-list-item title="Expenses" :to="monthDestination('/history')" />
        <v-list-item title="Pending Expenses" :to="monthDestination('/pending-expenses')" />
      </v-list>
    </v-menu>
    <v-btn to="/receipts" :active="route.path === '/receipts'" aria-label="Receipts">
      <v-icon>mdi-receipt-text-check</v-icon><span>Receipts</span>
    </v-btn>
    <v-btn :active="moreActive || moreOpen" aria-label="More" aria-haspopup="dialog" @click="moreOpen = true">
      <v-icon>mdi-dots-horizontal</v-icon><span>More</span>
    </v-btn>
  </v-bottom-navigation>

  <v-dialog v-model="moreOpen" max-width="440">
    <v-card title="More">
      <v-card-text class="text-wrap">{{ authStore.displayName }}</v-card-text>
      <v-list aria-label="More destinations and account">
        <v-list-item to="/settings" title="Settings" prepend-icon="mdi-cog" />
        <v-list-item to="/import" title="Import" prepend-icon="mdi-file-import" />
        <v-list-item :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'" prepend-icon="mdi-theme-light-dark" @click="toggleTheme" />
        <v-list-item v-if="authStore.isAuthenticated" title="Sign out" prepend-icon="mdi-logout" @click="authStore.logout()" />
        <v-list-item v-else title="Sign in" @click="authStore.login()" />
      </v-list>
      <v-card-actions><v-btn min-height="44" @click="moreOpen = false">Close</v-btn></v-card-actions>
    </v-card>
  </v-dialog>

  <v-main>
    <v-container :class="phone ? 'py-3 phone-content' : 'py-6'">
      <router-view />
    </v-container>
  </v-main>
</template>

<style scoped>
.phone-task-header {
  padding-top: env(safe-area-inset-top);
  height: calc(56px + env(safe-area-inset-top)) !important;
}
.phone-navigation {
  padding-bottom: env(safe-area-inset-bottom);
  height: calc(64px + env(safe-area-inset-bottom)) !important;
}
.phone-navigation :deep(.v-btn) {
  min-width: 0;
  min-height: 44px;
}
.phone-content {
  padding-top: calc(12px + env(safe-area-inset-top));
  padding-bottom: calc(24px + env(safe-area-inset-bottom));
}
.phone-content :deep([aria-label="Add Transaction"]) {
  bottom: calc(88px + env(safe-area-inset-bottom)) !important;
  right: 16px !important;
}
:deep(.v-btn:focus-visible), :deep(.v-list-item:focus-visible) {
  outline: 2px solid currentColor;
  outline-offset: -3px;
}
</style>
