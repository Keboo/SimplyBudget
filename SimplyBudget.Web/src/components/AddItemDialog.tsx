import { useState, useEffect } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Tabs, Tab, Autocomplete,
  FormControlLabel, Switch, Box
} from '@mui/material'
import { DatePicker } from '@mui/x-date-pickers'
import dayjs, { type Dayjs } from 'dayjs'
import type { ExpenseCategory } from '../types/api'
import { getExpenseCategories, createTransaction, createIncome, createTransfer } from '../services/api'

interface Props {
  open: boolean
  onClose: () => void
}

export default function AddItemDialog({ open, onClose }: Props) {
  const [tab, setTab] = useState(0)
  const [categories, setCategories] = useState<ExpenseCategory[]>([])
  const [date, setDate] = useState<Dayjs>(dayjs())
  const [description, setDescription] = useState('')
  const [ignoreBudget, setIgnoreBudget] = useState(false)
  const [amount, setAmount] = useState('')
  const [category, setCategory] = useState<ExpenseCategory | null>(null)
  const [toCategory, setToCategory] = useState<ExpenseCategory | null>(null)

  useEffect(() => {
    if (open) {
      getExpenseCategories().then(setCategories).catch(console.error)
    }
  }, [open])

  const reset = () => {
    setDescription('')
    setAmount('')
    setCategory(null)
    setToCategory(null)
    setIgnoreBudget(false)
    setDate(dayjs())
  }

  const handleClose = () => { reset(); onClose() }

  const handleSubmit = async () => {
    const amt = Math.round(parseFloat(amount) * 100)
    if (isNaN(amt) || amt <= 0) return
    try {
      if (tab === 0 && category) {
        await createTransaction({ date: date.format('YYYY-MM-DD'), description, ignoreBudget, items: [{ categoryId: category.id, amount: amt }] })
      } else if (tab === 1 && category) {
        await createIncome({ date: date.format('YYYY-MM-DD'), description, ignoreBudget, items: [{ categoryId: category.id, amount: amt }] })
      } else if (tab === 2 && category && toCategory) {
        await createTransfer({ date: date.format('YYYY-MM-DD'), description, ignoreBudget, fromCategoryId: category.id, toCategoryId: toCategory.id, amount: amt })
      }
      handleClose()
    } catch (e) {
      console.error(e)
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Item</DialogTitle>
      <DialogContent>
        <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
          <Tab label="Expense" />
          <Tab label="Income" />
          <Tab label="Transfer" />
        </Tabs>

        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <DatePicker label="Date" value={date} onChange={d => d && setDate(d)} />
          <TextField label="Description" value={description} onChange={e => setDescription(e.target.value)} fullWidth />
          <TextField label="Amount ($)" value={amount} onChange={e => setAmount(e.target.value)} type="number" inputProps={{ min: 0, step: 0.01 }} fullWidth />

          <Autocomplete
            options={categories}
            getOptionLabel={c => c.name || '(no name)'}
            value={category}
            onChange={(_, v) => setCategory(v)}
            renderInput={params => <TextField {...params} label={tab === 2 ? 'From Category' : 'Category'} />}
          />

          {tab === 2 && (
            <Autocomplete
              options={categories}
              getOptionLabel={c => c.name || '(no name)'}
              value={toCategory}
              onChange={(_, v) => setToCategory(v)}
              renderInput={params => <TextField {...params} label="To Category" />}
            />
          )}

          <FormControlLabel
            control={<Switch checked={ignoreBudget} onChange={e => setIgnoreBudget(e.target.checked)} />}
            label="Ignore budget"
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit}>Save</Button>
      </DialogActions>
    </Dialog>
  )
}
