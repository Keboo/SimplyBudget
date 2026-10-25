<script setup lang="ts">
import { useSnackbarStore } from '@/stores/snackbar'
import { useDisplay } from 'vuetify'

const store = useSnackbarStore()
const { xs: phone } = useDisplay()

function close(id: number) {
  store.dismiss(id)
}
</script>

<template>
  <v-snackbar
    v-for="(message, index) in store.messages"
    :key="message.id"
    :model-value="true"
    :color="message.variant"
    :timeout="4000"
    :style="phone ? { marginTop: `calc(64px + env(safe-area-inset-top) + ${index * 56}px)` } : { marginBottom: `${index * 56}px` }"
    :location="phone ? 'top' : 'bottom'"
    :class="{ 'phone-snackbar': phone }"
    @update:model-value="(val: boolean) => !val && close(message.id)"
  >
    {{ message.text }}
    <template #actions>
      <v-btn variant="text" min-height="44" @click="close(message.id)">Close</v-btn>
    </template>

  </v-snackbar>
</template>

<style scoped>
.phone-snackbar :deep(.v-snackbar__wrapper) {
  min-width: 0;
  max-width: calc(100vw - 24px);
}
.phone-snackbar :deep(.v-snackbar__content) {
  overflow-wrap: anywhere;
}
</style>
