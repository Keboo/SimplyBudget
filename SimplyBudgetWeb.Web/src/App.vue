<script setup lang="ts">
import { onMounted } from 'vue'
import { useRegisterSW } from 'virtual:pwa-register/vue'
import { useAuthStore } from '@/stores/auth'
import SnackbarHost from '@/components/SnackbarHost.vue'

const authStore = useAuthStore()
const { needRefresh, updateServiceWorker } = useRegisterSW()

onMounted(() => {
  void authStore.initialize()
})

async function refreshToLatestVersion() {
  needRefresh.value = false
  await updateServiceWorker()
  window.location.reload()
}
</script>

<template>
  <v-app>
    <template v-if="authStore.isInitializing">
      <v-main>
        <v-container class="d-flex align-center justify-center" style="min-height: 100vh;">
          <v-progress-circular indeterminate color="primary" size="64" />
        </v-container>
      </v-main>
    </template>
    <template v-else>
      <router-view />
    </template>
    <SnackbarHost />
    <v-snackbar :model-value="needRefresh" :timeout="-1" location="bottom">
      A new version is available.
      <template #actions>
        <v-btn variant="text" @click="refreshToLatestVersion">Refresh</v-btn>
      </template>
    </v-snackbar>
  </v-app>
</template>
