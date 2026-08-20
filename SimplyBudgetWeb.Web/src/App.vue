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
    <router-view />
    <SnackbarHost />
    <v-snackbar :model-value="needRefresh" :timeout="-1" location="bottom">
      A new version is available.
      <template #actions>
        <v-btn variant="text" @click="refreshToLatestVersion">Refresh</v-btn>
      </template>
    </v-snackbar>
  </v-app>
</template>
