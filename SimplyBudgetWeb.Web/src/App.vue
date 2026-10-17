<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import SnackbarHost from '@/components/SnackbarHost.vue'

const authStore = useAuthStore()

onMounted(() => {
  void authStore.initialize()
})
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
  </v-app>
</template>
