import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, TextField, FormControlLabel, Switch,
  Table, TableHead, TableRow, TableCell, TableBody,
  IconButton, Button, Tooltip, LinearProgress, Chip
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff'
import AddIcon from '@mui/icons-material/Add'
import { DatePicker } from '@mui/x-date-pickers'
import { useMonth } from '../contexts/MonthContext'
import { getBudget, hideExpenseCategory, deleteExpenseCategory } from '../services/api'
import type { BudgetOverview, BudgetCategory } from '../types/api'
import CategoryDialog from '../components/CategoryDialog'

function formatCurrency(cents: number) {
  return `$${(Math.abs(cents) / 100).toFixed(2)}`
}

export default function BudgetPage() {
  const { month, setMonth } = useMonth()
  const [overview, setOverview] = useState<BudgetOverview | null>(null)
  const [search, setSearch] = useState('')
  const [showHidden, setShowHidden] = useState(false)
  const [editCategory, setEditCategory] = useState<BudgetCategory | null>(null)
  const [createOpen, setCreateOpen] = useState(false)

  const load = useCallback(() => {
    getBudget(month.year(), month.month() + 1)
      .then(setOverview)
      .catch(console.error)
  }, [month])

  useEffect(() => { load() }, [load])

  const filtered = overview?.categories.filter(c => {
    if (!showHidden && c.name === undefined) return false
    const q = search.toLowerCase()
    return !q || c.name?.toLowerCase().includes(q) || c.categoryName?.toLowerCase().includes(q)
  }) ?? []

  // Group by categoryName
  const groups = filtered.reduce<Record<string, BudgetCategory[]>>((acc, c) => {
    const key = c.categoryName ?? '(none)'
    if (!acc[key]) acc[key] = []
    acc[key].push(c)
    return acc
  }, {})

  const handleHideToggle = async (c: BudgetCategory) => {
    // We don't have isHidden on BudgetCategory — toggle via expense category endpoints
    try {
      await hideExpenseCategory(c.id)
      load()
    } catch (e) { console.error(e) }
  }

  const handleDelete = async (c: BudgetCategory) => {
    if (!window.confirm(`Delete "${c.name}"?`)) return
    try {
      await deleteExpenseCategory(c.id)
      load()
    } catch (e) { console.error(e) }
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>Budget</Typography>
        <DatePicker
          label="Month"
          value={month}
          onChange={m => m && setMonth(m.startOf('month'))}
          views={['year', 'month']}
        />
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          New Category
        </Button>
      </Box>

      <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
        <TextField
          label="Search"
          value={search}
          onChange={e => setSearch(e.target.value)}
          size="small"
        />
        <FormControlLabel
          control={<Switch checked={showHidden} onChange={e => setShowHidden(e.target.checked)} />}
          label="Show hidden"
        />
      </Box>

      {overview && (
        <Typography variant="subtitle1" sx={{ mb: 1 }}>
          Total Budget: <strong>{formatCurrency(overview.totalBudget)}</strong>
        </Typography>
      )}

      {Object.entries(groups).map(([group, cats]) => (
        <Box key={group} sx={{ mb: 3 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>{group}</Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell align="right">Budget</TableCell>
                <TableCell align="right">Spent</TableCell>
                <TableCell align="right">Remaining</TableCell>
                <TableCell align="right">Balance</TableCell>
                <TableCell>Progress</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {cats.map(c => {
                const pct = c.effectiveBudgetAmount > 0
                  ? Math.min(100, (c.spentThisMonth / c.effectiveBudgetAmount) * 100)
                  : 0
                const overBudget = c.spentThisMonth > c.effectiveBudgetAmount && c.effectiveBudgetAmount > 0
                return (
                  <TableRow key={c.id}>
                    <TableCell>
                      {c.name}
                      {c.budgetedPercentage > 0 && (
                        <Chip label={`${c.budgetedPercentage}%`} size="small" sx={{ ml: 1 }} />
                      )}
                    </TableCell>
                    <TableCell align="right">{formatCurrency(c.effectiveBudgetAmount)}</TableCell>
                    <TableCell align="right">{formatCurrency(c.spentThisMonth)}</TableCell>
                    <TableCell align="right" sx={{ color: overBudget ? 'error.main' : 'inherit' }}>
                      {formatCurrency(c.remainingThisMonth)}
                    </TableCell>
                    <TableCell align="right">{formatCurrency(c.currentBalance)}</TableCell>
                    <TableCell sx={{ minWidth: 100 }}>
                      <LinearProgress
                        variant="determinate"
                        value={pct}
                        color={overBudget ? 'error' : 'primary'}
                      />
                    </TableCell>
                    <TableCell>
                      <Tooltip title="Edit">
                        <IconButton size="small" onClick={() => setEditCategory(c)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Hide">
                        <IconButton size="small" onClick={() => handleHideToggle(c)}>
                          <VisibilityOffIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete">
                        <IconButton size="small" onClick={() => handleDelete(c)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </Box>
      ))}

      <CategoryDialog
        open={createOpen || editCategory !== null}
        categoryId={editCategory?.id ?? null}
        onClose={() => { setCreateOpen(false); setEditCategory(null) }}
        onSaved={load}
      />
    </Box>
  )
}
