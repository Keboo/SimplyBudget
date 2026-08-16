<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <v-container class="d-flex flex-column align-center justify-center" style="min-height: 100vh;">
    <v-card class="pa-8 text-center" max-width="420" width="100%" elevation="4">
      <v-card-title class="text-h5 justify-center mb-2">Simply Budget</v-card-title>
      <v-card-subtitle class="mb-6 text-wrap">
        Sign in with your Microsoft account to manage your budget.
      </v-card-subtitle>
      <v-btn color="primary" size="large" block @click="authStore.login()">Sign in</v-btn>
    </v-card>
  </v-container>
</template>
