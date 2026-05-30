import { useState } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  Tab, Tabs, Box, MenuItem, Select, InputLabel, FormControl, IconButton,
  Typography, CircularProgress,
} from '@mui/material'
import { Add, Remove } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { ExpenseCategoryDto, TransactionRequest, TransferRequest } from '@/types'

interface Props {
  open: boolean
  onClose: () => void
  categories: ExpenseCategoryDto[]
  onSuccess: () => void
}

interface LineItem {
  expenseCategoryId: number | ''
  amount: string
}

const emptyLine = (): LineItem => ({ expenseCategoryId: '', amount: '' })

function LineItems({
  items,
  categories,
  onChange,
}: {
  items: LineItem[]
  categories: ExpenseCategoryDto[]
  onChange: (items: LineItem[]) => void
}) {
  const update = (index: number, patch: Partial<LineItem>) => {
    onChange(items.map((item, i) => (i === index ? { ...item, ...patch } : item)))
  }
  const remove = (index: number) => onChange(items.filter((_, i) => i !== index))
  const add = () => onChange([...items, emptyLine()])

  return (
    <Box>
      {items.map((item, index) => (
        <Box key={index} sx={{ display: 'flex', gap: 1, mb: 1, alignItems: 'center' }}>
          <FormControl size="small" sx={{ flex: 2 }}>
            <InputLabel>Category</InputLabel>
            <Select
              label="Category"
              value={item.expenseCategoryId}
              onChange={e => update(index, { expenseCategoryId: e.target.value as number | '' })}
            >
              {categories.map(c => (
                <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            size="small"
            label="Amount ($)"
            type="number"
            slotProps={{ htmlInput: { step: '0.01', min: '0' } }}
            value={item.amount}
            onChange={e => update(index, { amount: e.target.value })}
            sx={{ flex: 1 }}
          />
          {items.length > 1 && (
            <IconButton size="small" onClick={() => remove(index)}><Remove /></IconButton>
          )}
        </Box>
      ))}
      <Button size="small" startIcon={<Add />} onClick={add}>Add Line</Button>
    </Box>
  )
}

export default function AddTransactionDialog({ open, onClose, categories, onSuccess }: Props) {
  const [tab, setTab] = useState(0)
  const [description, setDescription] = useState('')
  const [date, setDate] = useState(() => new Date().toISOString().split('T')[0])
  const [lines, setLines] = useState<LineItem[]>([emptyLine()])
  const [transferAmount, setTransferAmount] = useState('')
  const [fromCategoryId, setFromCategoryId] = useState<number | ''>('')
  const [toCategoryId, setToCategoryId] = useState<number | ''>('')
  const [submitting, setSubmitting] = useState(false)
  const { enqueueSnackbar } = useSnackbar()

  const resetForm = () => {
    setDescription('')
    setDate(new Date().toISOString().split('T')[0])
    setLines([emptyLine()])
    setTransferAmount('')
    setFromCategoryId('')
    setToCategoryId('')
  }

  const handleClose = () => {
    resetForm()
    onClose()
  }

  const dollarsToCents = (s: string) => Math.round(parseFloat(s) * 100)

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      if (tab === 0 || tab === 1) {
        const endpoint = tab === 0 ? '/api/transactions/transaction' : '/api/transactions/income'
        const payload: TransactionRequest = {
          description,
          date,
          items: lines
            .filter(l => l.expenseCategoryId !== '' && l.amount !== '')
            .map(l => ({
              expenseCategoryId: l.expenseCategoryId as number,
              amount: dollarsToCents(l.amount),
            })),
        }
        await apiClient.post(endpoint, payload)
        enqueueSnackbar(tab === 0 ? 'Transaction added' : 'Income added', { variant: 'success' })
      } else {
        const payload: TransferRequest = {
          description,
          date,
          amount: dollarsToCents(transferAmount),
          fromCategoryId: fromCategoryId as number,
          toCategoryId: toCategoryId as number,
        }
        await apiClient.post('/api/transactions/transfer', payload)
        enqueueSnackbar('Transfer added', { variant: 'success' })
      }
      resetForm()
      onSuccess()
    } catch (e: unknown) {
      enqueueSnackbar(e instanceof Error ? e.message : 'Failed to save', { variant: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Transaction</DialogTitle>
      <DialogContent>
        <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
          <Tab label="Transaction" />
          <Tab label="Income" />
          <Tab label="Transfer" />
        </Tabs>

        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField
            label="Description"
            fullWidth
            value={description}
            onChange={e => setDescription(e.target.value)}
          />
          <TextField
            label="Date"
            type="date"
            fullWidth
            value={date}
            onChange={e => setDate(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />

          {(tab === 0 || tab === 1) && (
            <>
              <Typography variant="subtitle2">Items</Typography>
              <LineItems items={lines} categories={categories} onChange={setLines} />
            </>
          )}

          {tab === 2 && (
            <>
              <TextField
                label="Amount ($)"
                type="number"
                slotProps={{ htmlInput: { step: '0.01', min: '0' } }}
                fullWidth
                value={transferAmount}
                onChange={e => setTransferAmount(e.target.value)}
              />
              <FormControl fullWidth>
                <InputLabel>From Category</InputLabel>
                <Select
                  label="From Category"
                  value={fromCategoryId}
                  onChange={e => setFromCategoryId(e.target.value as number | '')}
                >
                  {categories.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>To Category</InputLabel>
                <Select
                  label="To Category"
                  value={toCategoryId}
                  onChange={e => setToCategoryId(e.target.value as number | '')}
                >
                  {categories.map(c => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
                </Select>
              </FormControl>
            </>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={submitting}>
          {submitting ? <CircularProgress size={20} /> : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
