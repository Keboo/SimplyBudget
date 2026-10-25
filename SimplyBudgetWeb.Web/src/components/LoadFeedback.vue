<script setup lang="ts">
defineProps<{ label: string; error: string; loading: boolean; stale?: boolean }>()
defineEmits<{ retry: [] }>()
</script>

<template>
  <v-alert v-if="error" :type="stale ? 'warning' : 'error'" variant="tonal" class="mb-4 load-feedback">
    <div class="font-weight-medium">Could not load {{ label }}</div>
    <div>{{ error }}</div>
    <div v-if="stale" class="mt-2">Showing last loaded data; it may be out of date.</div>
    <v-btn variant="text" min-height="44" class="mt-2" :aria-label="`Retry ${label}`" :loading="loading" @click="$emit('retry')">Retry</v-btn>
  </v-alert>
  <div v-else-if="loading && stale" role="status" class="text-body-2 mb-3">Refreshing {{ label }}…</div>
</template>

<style scoped>
.load-feedback {
  overflow-wrap: anywhere;
}
</style>
