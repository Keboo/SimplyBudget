import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Button, TextField, Paper, CircularProgress, List,
  ListItem, ListItemText, IconButton, Chip, Dialog, DialogTitle,
  DialogContent, DialogActions,
} from '@mui/material'
import { Edit, Save, Cancel } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { AccountDto } from '@/types'
import { formatCents } from '@/utils/currency'

export default function Accounts() {
  const [accounts, setAccounts] = useState<AccountDto[]>([])
  const [loading, setLoading] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [editName, setEditName] = useState('')
  const [newName, setNewName] = useState('')
  const [addOpen, setAddOpen] = useState(false)
  const { enqueueSnackbar } = useSnackbar()

  const fetchAccounts = useCallback(async () => {
    setLoading(true)
    try {
      const data = await apiClient.get<AccountDto[]>('/api/accounts')
      setAccounts(data ?? [])
    } catch {
      enqueueSnackbar('Failed to load accounts', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }, [enqueueSnackbar])

  useEffect(() => { fetchAccounts() }, [fetchAccounts])

  const handleAdd = async () => {
    if (!newName.trim()) return
    try {
      await apiClient.post('/api/accounts', { name: newName.trim() })
      enqueueSnackbar('Account added', { variant: 'success' })
      setNewName('')
      setAddOpen(false)
      fetchAccounts()
    } catch {
      enqueueSnackbar('Failed to add account', { variant: 'error' })
    }
  }

  const handleSaveEdit = async (id: number) => {
    try {
      await apiClient.put(`/api/accounts/${id}`, { name: editName })
      enqueueSnackbar('Account updated', { variant: 'success' })
      setEditId(null)
      fetchAccounts()
    } catch {
      enqueueSnackbar('Failed to update account', { variant: 'error' })
    }
  }

  const handleSetDefault = async (id: number) => {
    try {
      await apiClient.post(`/api/accounts/${id}/set-default`)
      enqueueSnackbar('Default account updated', { variant: 'success' })
      fetchAccounts()
    } catch {
      enqueueSnackbar('Failed to set default', { variant: 'error' })
    }
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Accounts</Typography>
        <Button variant="contained" onClick={() => setAddOpen(true)}>Add Account</Button>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>
      ) : (
        <List>
          {accounts.map(account => (
            <Paper key={account.id} sx={{ mb: 1 }}>
              <ListItem
                secondaryAction={
                  editId === account.id ? (
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <IconButton onClick={() => handleSaveEdit(account.id)} color="primary"><Save /></IconButton>
                      <IconButton onClick={() => setEditId(null)}><Cancel /></IconButton>
                    </Box>
                  ) : (
                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                      {!account.isDefault && (
                        <Button size="small" onClick={() => handleSetDefault(account.id)}>Set Default</Button>
                      )}
                      <IconButton onClick={() => { setEditId(account.id); setEditName(account.name ?? '') }}>
                        <Edit />
                      </IconButton>
                    </Box>
                  )
                }
              >
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {editId === account.id ? (
                        <TextField
                          size="small"
                          value={editName}
                          onChange={e => setEditName(e.target.value)}
                          onKeyDown={e => e.key === 'Enter' && handleSaveEdit(account.id)}
                          autoFocus
                        />
                      ) : (
                        <Typography>{account.name}</Typography>
                      )}
                      {account.isDefault && <Chip size="small" label="Default" color="primary" />}
                    </Box>
                  }
                  secondary={`Balance: ${formatCents(account.currentAmount)} · Validated: ${new Date(account.validatedDate).toLocaleDateString()}`}
                />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}

      <Dialog open={addOpen} onClose={() => setAddOpen(false)}>
        <DialogTitle>Add Account</DialogTitle>
        <DialogContent>
          <TextField
            label="Account Name"
            fullWidth
            value={newName}
            onChange={e => setNewName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleAdd()}
            sx={{ mt: 1 }}
            autoFocus
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAdd}>Add</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
