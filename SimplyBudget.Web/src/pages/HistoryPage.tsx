import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, TextField, Table, TableHead, TableRow,
  TableCell, TableBody, IconButton, Autocomplete, Chip
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import { DatePicker } from '@mui/x-date-pickers'
import { useMonth } from '../contexts/MonthContext'
import { getTransactions, deleteTransaction, getExpenseCategories, getAccounts } from '../services/api'
import type { Transaction, ExpenseCategory, Account } from '../types/api'

function formatCurrency(cents: number) {
  const abs = Math.abs(cents) / 100
  return cents < 0 ? `-$${abs.toFixed(2)}` : `$${abs.toFixed(2)}`
}

export default function HistoryPage() {
  const { month, setMonth } = useMonth()
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [categories, setCategories] = useState<ExpenseCategory[]>([])
  const [accounts, setAccounts] = useState<Account[]>([])
  const [search, setSearch] = useState('')
  const [selectedCategories, setSelectedCategories] = useState<ExpenseCategory[]>([])
  const [selectedAccount, setSelectedAccount] = useState<Account | null>(null)

  const load = useCallback(() => {
    getTransactions({
      year: month.year(),
      month: month.month() + 1,
      search: search || undefined,
      categoryIds: selectedCategories.map(c => c.id),
      accountId: selectedAccount?.id
    }).then(setTransactions).catch(console.error)
  }, [month, search, selectedCategories, selectedAccount])

  useEffect(() => {
    getExpenseCategories().then(setCategories).catch(console.error)
    getAccounts().then(setAccounts).catch(console.error)
  }, [])

  useEffect(() => { load() }, [load])

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this transaction?')) return
    await deleteTransaction(id)
    load()
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>History</Typography>
        <DatePicker
          label="Month"
          value={month}
          onChange={m => m && setMonth(m.startOf('month'))}
          views={['year', 'month']}
        />
      </Box>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mb: 2 }}>
        <TextField
          label="Search"
          value={search}
          onChange={e => setSearch(e.target.value)}
          size="small"
        />
        <Autocomplete
          multiple
          options={categories}
          getOptionLabel={c => c.name || ''}
          value={selectedCategories}
          onChange={(_, v) => setSelectedCategories(v)}
          renderTags={(value, getTagProps) =>
            value.map((c, i) => <Chip label={c.name} {...getTagProps({ index: i })} key={c.id} />)
          }
          renderInput={params => <TextField {...params} label="Categories" size="small" sx={{ minWidth: 200 }} />}
          sx={{ minWidth: 200 }}
        />
        <Autocomplete
          options={accounts}
          getOptionLabel={a => a.name || ''}
          value={selectedAccount}
          onChange={(_, v) => setSelectedAccount(v)}
          renderInput={params => <TextField {...params} label="Account" size="small" />}
          sx={{ minWidth: 160 }}
        />
      </Box>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Date</TableCell>
            <TableCell>Description</TableCell>
            <TableCell>Categories</TableCell>
            <TableCell align="right">Amount</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {transactions.map(t => (
            <TableRow key={t.id}>
              <TableCell>{new Date(t.date).toLocaleDateString()}</TableCell>
              <TableCell>{t.description}</TableCell>
              <TableCell>
                {t.details.map(d => (
                  <Chip key={d.id} label={`${d.categoryName}: ${formatCurrency(d.amount)}`} size="small" sx={{ mr: 0.5 }} />
                ))}
              </TableCell>
              <TableCell align="right">
                {formatCurrency(t.details.reduce((s, d) => s + d.amount, 0))}
              </TableCell>
              <TableCell>
                <IconButton size="small" onClick={() => handleDelete(t.id)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
          {transactions.length === 0 && (
            <TableRow>
              <TableCell colSpan={5} align="center">No transactions</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Box>
  )
}
