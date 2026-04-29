import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Table, TableHead, TableRow, TableCell,
  TableBody, IconButton, Button, Dialog, DialogTitle,
  DialogContent, DialogActions, TextField
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import AddIcon from '@mui/icons-material/Add'
import { DatePicker } from '@mui/x-date-pickers'
import dayjs, { type Dayjs } from 'dayjs'
import { getAccounts, createAccount, updateAccount, deleteAccount } from '../services/api'
import type { Account } from '../types/api'

function formatCurrency(cents: number) {
  return `$${(Math.abs(cents) / 100).toFixed(2)}`
}

interface AccountFormProps {
  open: boolean
  account: Account | null
  onClose: () => void
  onSaved: () => void
}

function AccountForm({ open, account, onClose, onSaved }: AccountFormProps) {
  const [name, setName] = useState(account?.name ?? '')
  const [validatedDate, setValidatedDate] = useState<Dayjs>(
    account ? dayjs(account.validatedDate) : dayjs()
  )

  useEffect(() => {
    setName(account?.name ?? '')
    setValidatedDate(account ? dayjs(account.validatedDate) : dayjs())
  }, [account])

  const handleSave = async () => {
    try {
      const data = { name, validatedDate: validatedDate.format('YYYY-MM-DD') }
      if (account) {
        await updateAccount(account.id, data)
      } else {
        await createAccount(data)
      }
      onSaved()
      onClose()
    } catch (e) { console.error(e) }
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{account ? 'Edit Account' : 'New Account'}</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        <TextField label="Name" value={name} onChange={e => setName(e.target.value)} fullWidth />
        <DatePicker label="Validated Date" value={validatedDate} onChange={d => d && setValidatedDate(d)} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave}>Save</Button>
      </DialogActions>
    </Dialog>
  )
}

export default function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [editAccount, setEditAccount] = useState<Account | null>(null)
  const [createOpen, setCreateOpen] = useState(false)

  const load = useCallback(() => {
    getAccounts().then(setAccounts).catch(console.error)
  }, [])

  useEffect(() => { load() }, [load])

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this account?')) return
    await deleteAccount(id)
    load()
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>Accounts</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          New Account
        </Button>
      </Box>

      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell align="right">Balance</TableCell>
            <TableCell>Validated</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {accounts.map(a => (
            <TableRow key={a.id}>
              <TableCell>{a.name}{a.isDefault && ' (default)'}</TableCell>
              <TableCell align="right">{formatCurrency(a.currentBalance)}</TableCell>
              <TableCell>{new Date(a.validatedDate).toLocaleDateString()}</TableCell>
              <TableCell>
                <IconButton size="small" onClick={() => setEditAccount(a)}>
                  <EditIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" onClick={() => handleDelete(a.id)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <AccountForm
        open={createOpen || editAccount !== null}
        account={editAccount}
        onClose={() => { setCreateOpen(false); setEditAccount(null) }}
        onSaved={load}
      />
    </Box>
  )
}
