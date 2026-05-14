import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Button, List, ListItem, ListItemText,
  Paper, CircularProgress, Accordion, AccordionSummary, AccordionDetails,
  Chip, Fab, Tooltip,
} from '@mui/material'
import { ChevronLeft, ChevronRight, ExpandMore, Add } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { BudgetResponse, BudgetCategoryDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import AddTransactionDialog from '@/components/AddTransactionDialog'

export default function Budget() {
  const [currentMonth, setCurrentMonth] = useState<Date>(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  })
  const [budget, setBudget] = useState<BudgetResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [categories, setCategories] = useState<ExpenseCategoryDto[]>([])
  const { enqueueSnackbar } = useSnackbar()

  const fetchBudget = useCallback(async () => {
    setLoading(true)
    try {
      const month = formatMonth(currentMonth)
      const data = await apiClient.get<BudgetResponse>(`/api/budget?month=${month}-01`)
      setBudget(data)
    } catch (e) {
      enqueueSnackbar('Failed to load budget', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }, [currentMonth, enqueueSnackbar])

  const fetchCategories = useCallback(async () => {
    try {
      const data = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
      setCategories(data)
    } catch { /* ignore */ }
  }, [])

  useEffect(() => { fetchBudget() }, [fetchBudget])
  useEffect(() => { fetchCategories() }, [fetchCategories])

  const prevMonth = () => {
    setCurrentMonth(d => new Date(d.getFullYear(), d.getMonth() - 1, 1))
  }
  const nextMonth = () => {
    setCurrentMonth(d => new Date(d.getFullYear(), d.getMonth() + 1, 1))
  }

  const grouped = groupByCategory(budget?.categories ?? [])

  return (
    <Box>
      {/* Month Navigator */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, gap: 1 }}>
        <Button variant="outlined" size="small" onClick={prevMonth} startIcon={<ChevronLeft />}>Prev</Button>
        <Typography variant="h6" sx={{ minWidth: 160, textAlign: 'center' }}>
          {currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' })}
        </Typography>
        <Button variant="outlined" size="small" onClick={nextMonth} endIcon={<ChevronRight />}>Next</Button>
      </Box>

      {/* Total Budget */}
      {budget && (
        <Paper sx={{ p: 2, mb: 2 }}>
          <Typography variant="h5">
            Total Budget: <strong>{formatCents(budget.totalBudget)}</strong>
          </Typography>
        </Paper>
      )}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Box>
          {grouped.map(({ groupName, items }) => (
            <Accordion key={groupName} defaultExpanded>
              <AccordionSummary expandIcon={<ExpandMore />}>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>{groupName}</Typography>
              </AccordionSummary>
              <AccordionDetails sx={{ p: 0 }}>
                <List dense disablePadding>
                  {items.map(cat => (
                    <ListItem key={cat.id} divider>
                      <ListItemText
                        primary={cat.name ?? '(unnamed)'}
                        secondary={
                          <Box component="span" sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 0.5 }}>
                            <Chip size="small" label={`Budget: ${cat.usePercentage ? `${cat.budgetedPercentage}%` : formatCents(cat.budgetedAmount)}`} />
                            <Chip size="small" label={`Spent: ${formatCents(cat.monthlyExpenses)}`} color="error" variant="outlined" />
                            <Chip size="small" label={`Balance: ${formatCents(cat.currentBalance)}`} color={cat.currentBalance >= 0 ? 'success' : 'error'} />
                            <Chip size="small" label={`3mo avg: ${formatCents(cat.threeMonthAverage)}`} variant="outlined" />
                            <Chip size="small" label={`6mo avg: ${formatCents(cat.sixMonthAverage)}`} variant="outlined" />
                            <Chip size="small" label={`12mo avg: ${formatCents(cat.twelveMonthAverage)}`} variant="outlined" />
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </AccordionDetails>
            </Accordion>
          ))}
        </Box>
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
        onSuccess={() => { fetchBudget(); setDialogOpen(false) }}
      />
    </Box>
  )
}

function groupByCategory(categories: BudgetCategoryDto[]) {
  const map = new Map<string, BudgetCategoryDto[]>()
  for (const cat of categories) {
    const key = cat.categoryName?.trim() || 'Uncategorized'
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(cat)
  }
  return Array.from(map.entries()).map(([groupName, items]) => ({ groupName, items }))
}
