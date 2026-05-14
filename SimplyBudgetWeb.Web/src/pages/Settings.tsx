import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Button, TextField, MenuItem, Select, InputLabel,
  FormControl, Paper, CircularProgress, List, ListItem, ListItemText,
  IconButton, Dialog, DialogTitle, DialogContent, DialogActions,
  DialogContentText,
} from '@mui/material'
import { Edit, Delete } from '@mui/icons-material'
import { useSnackbar } from 'notistack'
import { apiClient } from '@/services/apiClient'
import { RuleDto, ExpenseCategoryDto } from '@/types'

export default function Settings() {
  const [rules, setRules] = useState<RuleDto[]>([])
  const [categories, setCategories] = useState<ExpenseCategoryDto[]>([])
  const [loading, setLoading] = useState(false)
  const [addOpen, setAddOpen] = useState(false)
  const [deleteRule, setDeleteRule] = useState<RuleDto | null>(null)
  const [editRule, setEditRule] = useState<RuleDto | null>(null)
  const [form, setForm] = useState({ name: '', ruleRegex: '', expenseCategoryId: '' })
  const { enqueueSnackbar } = useSnackbar()

  const fetchRules = useCallback(async () => {
    setLoading(true)
    try {
      const data = await apiClient.get<RuleDto[]>('/api/rules')
      setRules(data ?? [])
    } catch {
      enqueueSnackbar('Failed to load rules', { variant: 'error' })
    } finally {
      setLoading(false)
    }
  }, [enqueueSnackbar])

  const fetchCategories = useCallback(async () => {
    try {
      const data = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
      setCategories(data ?? [])
    } catch { /* ignore */ }
  }, [])

  useEffect(() => { fetchRules(); fetchCategories() }, [fetchRules, fetchCategories])

  const resetForm = () => setForm({ name: '', ruleRegex: '', expenseCategoryId: '' })

  const handleAdd = async () => {
    try {
      await apiClient.post('/api/rules', {
        name: form.name,
        ruleRegex: form.ruleRegex,
        expenseCategoryId: form.expenseCategoryId ? Number(form.expenseCategoryId) : null,
      })
      enqueueSnackbar('Rule added', { variant: 'success' })
      setAddOpen(false)
      resetForm()
      fetchRules()
    } catch {
      enqueueSnackbar('Failed to add rule', { variant: 'error' })
    }
  }

  const handleEdit = async () => {
    if (!editRule) return
    try {
      await apiClient.put(`/api/rules/${editRule.id}`, {
        name: form.name,
        ruleRegex: form.ruleRegex,
        expenseCategoryId: form.expenseCategoryId ? Number(form.expenseCategoryId) : null,
      })
      enqueueSnackbar('Rule updated', { variant: 'success' })
      setEditRule(null)
      resetForm()
      fetchRules()
    } catch {
      enqueueSnackbar('Failed to update rule', { variant: 'error' })
    }
  }

  const handleDelete = async () => {
    if (!deleteRule) return
    try {
      await apiClient.delete(`/api/rules/${deleteRule.id}`)
      enqueueSnackbar('Rule deleted', { variant: 'success' })
      setDeleteRule(null)
      fetchRules()
    } catch {
      enqueueSnackbar('Failed to delete rule', { variant: 'error' })
    }
  }

  const openEdit = (rule: RuleDto) => {
    setEditRule(rule)
    setForm({
      name: rule.name ?? '',
      ruleRegex: rule.ruleRegex ?? '',
      expenseCategoryId: rule.expenseCategoryId ? String(rule.expenseCategoryId) : '',
    })
  }

  const RuleForm = () => (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
      <TextField label="Name" fullWidth value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
      <TextField label="Regex Pattern" fullWidth value={form.ruleRegex} onChange={e => setForm(f => ({ ...f, ruleRegex: e.target.value }))} />
      <FormControl fullWidth>
        <InputLabel>Category</InputLabel>
        <Select
          label="Category"
          value={form.expenseCategoryId}
          onChange={e => setForm(f => ({ ...f, expenseCategoryId: e.target.value as string }))}
        >
          <MenuItem value="">None</MenuItem>
          {categories.map(c => <MenuItem key={c.id} value={String(c.id)}>{c.name}</MenuItem>)}
        </Select>
      </FormControl>
    </Box>
  )

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5">Import Rules</Typography>
        <Button variant="contained" onClick={() => { resetForm(); setAddOpen(true) }}>Add Rule</Button>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box>
      ) : (
        <List>
          {rules.length === 0 && <ListItem><ListItemText primary="No rules defined." /></ListItem>}
          {rules.map(rule => (
            <Paper key={rule.id} sx={{ mb: 1 }}>
              <ListItem
                secondaryAction={
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <IconButton onClick={() => openEdit(rule)}><Edit /></IconButton>
                    <IconButton color="error" onClick={() => setDeleteRule(rule)}><Delete /></IconButton>
                  </Box>
                }
              >
                <ListItemText
                  primary={rule.name}
                  secondary={`Pattern: ${rule.ruleRegex ?? '—'} · Category: ${rule.categoryName ?? 'None'}`}
                />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}

      {/* Add Dialog */}
      <Dialog open={addOpen} onClose={() => setAddOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Rule</DialogTitle>
        <DialogContent><RuleForm /></DialogContent>
        <DialogActions>
          <Button onClick={() => setAddOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAdd}>Add</Button>
        </DialogActions>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editRule} onClose={() => setEditRule(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Rule</DialogTitle>
        <DialogContent><RuleForm /></DialogContent>
        <DialogActions>
          <Button onClick={() => setEditRule(null)}>Cancel</Button>
          <Button variant="contained" onClick={handleEdit}>Save</Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirm */}
      <Dialog open={!!deleteRule} onClose={() => setDeleteRule(null)}>
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>Delete rule "{deleteRule?.name}"?</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteRule(null)}>Cancel</Button>
          <Button color="error" onClick={handleDelete}>Delete</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
