<script setup lang="ts">
import { useSnackbarStore } from '@/stores/snackbar'

const store = useSnackbarStore()

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
    :style="{ marginBottom: `${index * 56}px` }"
    location="bottom"
    @update:model-value="(val: boolean) => !val && close(message.id)"
  >
    {{ message.text }}
    <template #actions>
      <v-btn variant="text" @click="close(message.id)">Close</v-btn>
    </template>
  </v-snackbar>
</template>
