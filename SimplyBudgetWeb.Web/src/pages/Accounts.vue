<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { AccountDto } from '@/types'
import { formatCents } from '@/utils/currency'

const snackbar = useSnackbarStore()

const accounts = ref<AccountDto[]>([])
const loading = ref(false)
const editId = ref<number | null>(null)
const editName = ref('')
const newName = ref('')
const addOpen = ref(false)

async function fetchAccounts() {
  loading.value = true
  try {
    accounts.value = await apiClient.get<AccountDto[]>('/api/accounts') ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load accounts', { variant: 'error' })
  } finally {
    loading.value = false
  }
}

async function handleAdd() {
  if (!newName.value.trim()) return
  try {
    await apiClient.post('/api/accounts', { name: newName.value.trim() })
    snackbar.enqueueSnackbar('Account added', { variant: 'success' })
    newName.value = ''
    addOpen.value = false
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to add account', { variant: 'error' })
  }
}

async function handleSaveEdit(id: number) {
  try {
    await apiClient.put(`/api/accounts/${id}`, { name: editName.value })
    snackbar.enqueueSnackbar('Account updated', { variant: 'success' })
    editId.value = null
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to update account', { variant: 'error' })
  }
}

async function handleSetDefault(id: number) {
  try {
    await apiClient.post(`/api/accounts/${id}/set-default`)
    snackbar.enqueueSnackbar('Default account updated', { variant: 'success' })
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to set default', { variant: 'error' })
  }
}

function startEdit(account: AccountDto) {
  editId.value = account.id
  editName.value = account.name ?? ''
}

onMounted(() => { void fetchAccounts() })
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <h5 class="text-h5">Accounts</h5>
      <v-btn color="primary" @click="addOpen = true">Add Account</v-btn>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading accounts" />
    </div>
    <v-list v-else>
      <v-card v-for="account in accounts" :key="account.id" class="mb-2">
        <v-list-item>
          <v-list-item-title>
            <div class="d-flex align-center" style="gap: 8px;">
              <v-text-field
                v-if="editId === account.id"
                v-model="editName"
                density="compact"
                hide-details
                autofocus
                style="max-width: 240px;"
                @keydown.enter="handleSaveEdit(account.id)"
              />
              <span v-else>{{ account.name }}</span>
              <v-chip v-if="account.isDefault" size="small" color="primary">Default</v-chip>
            </div>
          </v-list-item-title>
          <v-list-item-subtitle>
            Balance: {{ formatCents(account.currentAmount) }} · Validated: {{ new Date(account.validatedDate).toLocaleDateString() }}
          </v-list-item-subtitle>
          <template #append>
            <div v-if="editId === account.id" class="d-flex" style="gap: 4px;">
              <v-btn icon="mdi-content-save" color="primary" variant="text" aria-label="Save" @click="handleSaveEdit(account.id)" />
              <v-btn icon="mdi-close" variant="text" aria-label="Cancel" @click="editId = null" />
            </div>
            <div v-else class="d-flex align-center" style="gap: 4px;">
              <v-btn v-if="!account.isDefault" size="small" variant="text" @click="handleSetDefault(account.id)">Set Default</v-btn>
              <v-btn icon="mdi-pencil" variant="text" aria-label="Edit account name" @click="startEdit(account)" />
            </div>
          </template>
        </v-list-item>
      </v-card>
    </v-list>

    <v-dialog v-model="addOpen" max-width="480">
      <v-card>
        <v-card-title>Add Account</v-card-title>
        <v-card-text>
          <v-text-field label="Account Name" v-model="newName" autofocus @keydown.enter="handleAdd" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="addOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAdd">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>
