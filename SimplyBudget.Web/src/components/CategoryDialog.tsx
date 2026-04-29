import { useState, useEffect } from 'react'
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Autocomplete, Box
} from '@mui/material'
import {
  getExpenseCategories, createExpenseCategory, updateExpenseCategory, getAccounts
} from '../services/api'
import type { ExpenseCategory, Account } from '../types/api'

interface Props {
  open: boolean
  categoryId: number | null
  onClose: () => void
  onSaved: () => void
}

export default function CategoryDialog({ open, categoryId, onClose, onSaved }: Props) {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [name, setName] = useState('')
  const [categoryName, setCategoryName] = useState('')
  const [budgetedAmount, setBudgetedAmount] = useState('')
  const [budgetedPercentage, setBudgetedPercentage] = useState('')
  const [cap, setCap] = useState('')
  const [account, setAccount] = useState<Account | null>(null)
  const [existingCategories, setExistingCategories] = useState<ExpenseCategory[]>([])

  useEffect(() => {
    if (!open) return
    getAccounts().then(setAccounts).catch(console.error)
    getExpenseCategories(true).then(cats => {
      setExistingCategories(cats)
      if (categoryId !== null) {
        const cat = cats.find(c => c.id === categoryId)
        if (cat) {
          setName(cat.name || '')
          setCategoryName(cat.categoryName || '')
          setBudgetedAmount(cat.budgetedAmount > 0 ? String(cat.budgetedAmount / 100) : '')
          setBudgetedPercentage(cat.budgetedPercentage > 0 ? String(cat.budgetedPercentage) : '')
          setCap(cat.cap ? String(cat.cap / 100) : '')
        }
      }
    }).catch(console.error)
  }, [open, categoryId])

  useEffect(() => {
    if (accounts.length > 0 && categoryId !== null) {
      const cat = existingCategories.find(c => c.id === categoryId)
      if (cat) setAccount(accounts.find(a => a.id === cat.accountId) ?? null)
    }
  }, [accounts, categoryId, existingCategories])

  const reset = () => {
    setName('')
    setCategoryName('')
    setBudgetedAmount('')
    setBudgetedPercentage('')
    setCap('')
    setAccount(null)
  }

  const handleClose = () => { reset(); onClose() }

  const handleSave = async () => {
    const data = {
      name,
      categoryName: categoryName || null,
      budgetedAmount: Math.round((parseFloat(budgetedAmount) || 0) * 100),
      budgetedPercentage: parseInt(budgetedPercentage) || 0,
      cap: cap ? Math.round(parseFloat(cap) * 100) : null,
      accountId: account?.id ?? null
    }
    try {
      if (categoryId !== null) {
        await updateExpenseCategory(categoryId, data)
      } else {
        await createExpenseCategory(data)
      }
      onSaved()
      handleClose()
    } catch (e) { console.error(e) }
  }

  // Unique category names for autocomplete
  const categoryNames = [...new Set(existingCategories.map(c => c.categoryName).filter(Boolean) as string[])]

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>{categoryId ? 'Edit Category' : 'New Category'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField label="Name" value={name} onChange={e => setName(e.target.value)} fullWidth required />
          <Autocomplete
            freeSolo
            options={categoryNames}
            value={categoryName}
            onInputChange={(_, v) => setCategoryName(v)}
            renderInput={params => <TextField {...params} label="Category Group" />}
          />
          <TextField
            label="Budgeted Amount ($)"
            value={budgetedAmount}
            onChange={e => setBudgetedAmount(e.target.value)}
            type="number"
            inputProps={{ min: 0, step: 0.01 }}
            fullWidth
          />
          <TextField
            label="Budgeted Percentage (%)"
            value={budgetedPercentage}
            onChange={e => setBudgetedPercentage(e.target.value)}
            type="number"
            inputProps={{ min: 0, max: 99, step: 1 }}
            fullWidth
            helperText="Percentage of total budget. Overrides fixed amount if set."
          />
          <TextField
            label="Cap ($)"
            value={cap}
            onChange={e => setCap(e.target.value)}
            type="number"
            inputProps={{ min: 0, step: 0.01 }}
            fullWidth
          />
          <Autocomplete
            options={accounts}
            getOptionLabel={a => a.name || ''}
            value={account}
            onChange={(_, v) => setAccount(v)}
            renderInput={params => <TextField {...params} label="Account" />}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave} disabled={!name}>Save</Button>
      </DialogActions>
    </Dialog>
  )
}
