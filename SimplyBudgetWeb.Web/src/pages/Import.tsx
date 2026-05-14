import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Button, TextField, MenuItem, Select, FormControl,
  Paper, CircularProgress, Table, TableHead, TableBody,
  TableRow, TableCell, Checkbox,
} from '@mui/material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { ImportItemDto, ExpenseCategoryDto } from '@/types'
import { formatCents } from '@/utils/currency'

export default function Import() {
  const [csvText, setCsvText] = useState('')
  const [items, setItems] = useState<ImportItemDto[]>([])
  const [categories, setCategories] = useState<ExpenseCategoryDto[]>([])
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const { enqueueSnackbar } = useSnackbar()

  const fetchCategories = useCallback(async () => {
    try {
      const data = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
      setCategories(data ?? [])
    } catch { /* ignore */ }
  }, [])

  useEffect(() => { fetchCategories() }, [fetchCategories])

  const handleParse = async () => {
    if (!csvText.trim()) return
    setLoading(true)
    try {
      const data = await apiClient.post<ImportItemDto[]>('/api/import/parse', { csv: csvText })
      setItems(data ?? [])
      enqueueSnackbar(`Parsed ${data?.length ?? 0} items`, { variant: 'success' })
    } catch {
      enqueueSnackbar('Failed to parse CSV', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }

  const updateItem = (index: number, updates: Partial<ImportItemDto>) => {
    setItems(prev => prev.map((item, i) => i === index ? { ...item, ...updates } : item))
  }

  const handleImport = async () => {
    setSubmitting(true)
    try {
      await apiClient.post('/api/import/save', items)
      enqueueSnackbar('Import saved', { variant: 'success' })
      setItems([])
      setCsvText('')
    } catch {
      enqueueSnackbar('Failed to save import', { variant: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom>Import Transactions</Typography>

      <Paper sx={{ p: 2, mb: 2 }}>
        <TextField
          label="Paste CSV data here"
          multiline
          rows={8}
          fullWidth
          value={csvText}
          onChange={e => setCsvText(e.target.value)}
          placeholder="date,description,amount..."
          sx={{ mb: 2 }}
        />
        <Button variant="contained" onClick={handleParse} disabled={loading || !csvText.trim()}>
          {loading ? <CircularProgress size={20} /> : 'Parse'}
        </Button>
      </Paper>

      {items.length > 0 && (
        <>
          <Paper sx={{ overflow: 'auto', mb: 2 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Done</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell align="right">Amount</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Category</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((item, index) => (
                  <TableRow key={index} sx={{ opacity: item.isDone ? 0.5 : 1 }}>
                    <TableCell>
                      <Checkbox
                        checked={item.isDone}
                        onChange={e => updateItem(index, { isDone: e.target.checked })}
                      />
                    </TableCell>
                    <TableCell>{new Date(item.date).toLocaleDateString()}</TableCell>
                    <TableCell>{item.description}</TableCell>
                    <TableCell align="right">{formatCents(item.amount)}</TableCell>
                    <TableCell>{item.isDebit ? 'Debit' : 'Credit'}</TableCell>
                    <TableCell>
                      <FormControl size="small" sx={{ minWidth: 150 }}>
                        <Select
                          value={item.suggestedCategoryId ?? -1}
                          onChange={e => {
                            const val = e.target.value as number
                            const numVal = val === -1 ? null : val
                            const cat = numVal !== null ? categories.find(c => c.id === numVal) : undefined
                            updateItem(index, {
                              suggestedCategoryId: numVal,
                              suggestedCategoryName: cat?.name ?? null,
                            })
                          }}
                          displayEmpty
                        >
                          <MenuItem value={-1}><em>None</em></MenuItem>
                          {categories.map(c => (
                            <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
          <Button variant="contained" onClick={handleImport} disabled={submitting}>
            {submitting ? <CircularProgress size={20} /> : 'Save Import'}
          </Button>
        </>
      )}
    </Box>
  )
}
