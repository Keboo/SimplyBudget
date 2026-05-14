import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Button, TextField, MenuItem, Select, InputLabel,
  FormControl, Paper, CircularProgress, List, ListItem, ListItemText,
  IconButton, Chip, Fab, Tooltip, Dialog, DialogTitle, DialogContent,
  DialogContentText, DialogActions,
} from '@mui/material'
import { ChevronLeft, ChevronRight, Delete, Add } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { HistoryItemDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import AddTransactionDialog from '@/components/AddTransactionDialog'

export default function History() {
  const [currentMonth, setCurrentMonth] = useState<Date>(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  })
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState<number | ''>('')
  const [items, setItems] = useState<HistoryItemDto[]>([])
  const [categories, setCategories] = useState<ExpenseCategoryDto[]>([])
  const [loading, setLoading] = useState(false)
  const [deleteItem, setDeleteItem] = useState<HistoryItemDto | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const { enqueueSnackbar } = useSnackbar()

  const fetchCategories = useCallback(async () => {
    try {
      const data = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
      setCategories(data)
    } catch { /* ignore */ }
  }, [])

  const fetchHistory = useCallback(async () => {
    setLoading(true)
    try {
      const month = `${formatMonth(currentMonth)}-01`
      const params = new URLSearchParams({ month })
      if (search) params.set('search', search)
      if (categoryId !== '') params.set('categoryId', String(categoryId))
      const data = await apiClient.get<HistoryItemDto[]>(`/api/history?${params}`)
      setItems(data ?? [])
    } catch {
      enqueueSnackbar('Failed to load history', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }, [currentMonth, search, categoryId, enqueueSnackbar])

  useEffect(() => { fetchCategories() }, [fetchCategories])
  useEffect(() => { fetchHistory() }, [fetchHistory])

  const handleDelete = async () => {
    if (!deleteItem) return
    try {
      await apiClient.delete(`/api/history/${deleteItem.id}`)
      enqueueSnackbar('Transaction deleted', { variant: 'success' })
      setDeleteItem(null)
      fetchHistory()
    } catch {
      enqueueSnackbar('Failed to delete transaction', { variant: 'error' })
    }
  }

  const totalForItem = (item: HistoryItemDto) =>
    item.details.reduce((sum, d) => sum + d.amount, 0)

  return (
    <Box>
      <Typography variant="h5" gutterBottom>History</Typography>

      {/* Filters */}
      <Paper sx={{ p: 2, mb: 2, display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
        <Button variant="outlined" size="small" onClick={() => setCurrentMonth(d => new Date(d.getFullYear(), d.getMonth() - 1, 1))} startIcon={<ChevronLeft />}>Prev</Button>
        <Typography sx={{ minWidth: 140, textAlign: 'center' }}>
          {currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' })}
        </Typography>
        <Button variant="outlined" size="small" onClick={() => setCurrentMonth(d => new Date(d.getFullYear(), d.getMonth() + 1, 1))} endIcon={<ChevronRight />}>Next</Button>

        <TextField
          label="Search"
          size="small"
          value={search}
          onChange={e => setSearch(e.target.value)}
          sx={{ minWidth: 200 }}
        />

        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel>Category</InputLabel>
          <Select
            label="Category"
            value={categoryId}
            onChange={e => setCategoryId(e.target.value as number | '')}
          >
            <MenuItem value="">All</MenuItem>
            {categories.map(c => (
              <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Paper>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>
      ) : (
        <List>
          {items.length === 0 && (
            <ListItem><ListItemText primary="No transactions found." /></ListItem>
          )}
          {items.map(item => (
            <Paper key={item.id} sx={{ mb: 1 }}>
              <ListItem
                secondaryAction={
                  <IconButton edge="end" color="error" onClick={() => setDeleteItem(item)}>
                    <Delete />
                  </IconButton>
                }
              >
                <ListItemText
                  primary={
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography>
                        {new Date(item.date).toLocaleDateString()} — {item.description}
                        {item.isTransfer && <Chip size="small" label="Transfer" sx={{ ml: 1 }} />}
                      </Typography>
                      <Typography sx={{ fontWeight: 'bold' }}>{formatCents(totalForItem(item))}</Typography>
                    </Box>
                  }
                  secondary={
                    <Box component="span" sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 0.5 }}>
                      {item.details.map(d => (
                        <Chip key={d.id} size="small" label={`${d.categoryName}: ${formatCents(d.amount)}`} variant="outlined" />
                      ))}
                    </Box>
                  }
                />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}

      <Tooltip title="Add Transaction">
        <Fab color="primary" sx={{ position: 'fixed', bottom: 32, right: 32 }} onClick={() => setDialogOpen(true)}>
          <Add />
        </Fab>
      </Tooltip>

      <AddTransactionDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        categories={categories}
        onSuccess={() => { fetchHistory(); setDialogOpen(false) }}
      />

      {/* Delete Confirm Dialog */}
      <Dialog open={!!deleteItem} onClose={() => setDeleteItem(null)}>
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Delete transaction "{deleteItem?.description}"?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteItem(null)}>Cancel</Button>
          <Button color="error" onClick={handleDelete}>Delete</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
