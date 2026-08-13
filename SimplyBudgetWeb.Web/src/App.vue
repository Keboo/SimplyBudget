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
    <router-view />
    <SnackbarHost />
  </v-app>
</template>
