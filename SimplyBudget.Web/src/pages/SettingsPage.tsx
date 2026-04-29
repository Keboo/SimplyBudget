import { useState, useEffect, useCallback } from 'react'
import {
  Box, Typography, Table, TableHead, TableRow, TableCell,
  TableBody, IconButton, Button, Dialog, DialogTitle,
  DialogContent, DialogActions, TextField, Autocomplete
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import AddIcon from '@mui/icons-material/Add'
import {
  getCategoryRules, createCategoryRule, updateCategoryRule,
  deleteCategoryRule, getExpenseCategories
} from '../services/api'
import type { CategoryRule, ExpenseCategory } from '../types/api'

interface RuleFormProps {
  open: boolean
  rule: CategoryRule | null
  categories: ExpenseCategory[]
  onClose: () => void
  onSaved: () => void
}

function RuleForm({ open, rule, categories, onClose, onSaved }: RuleFormProps) {
  const [name, setName] = useState(rule?.name ?? '')
  const [regex, setRegex] = useState(rule?.ruleRegex ?? '')
  const [category, setCategory] = useState<ExpenseCategory | null>(
    categories.find(c => c.id === rule?.expenseCategoryId) ?? null
  )

  useEffect(() => {
    setName(rule?.name ?? '')
    setRegex(rule?.ruleRegex ?? '')
    setCategory(categories.find(c => c.id === rule?.expenseCategoryId) ?? null)
  }, [rule, categories])

  const handleSave = async () => {
    try {
      const data = { name, ruleRegex: regex, expenseCategoryId: category?.id ?? null }
      if (rule) {
        await updateCategoryRule(rule.id, data)
      } else {
        await createCategoryRule(data)
      }
      onSaved()
      onClose()
    } catch (e) { console.error(e) }
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{rule ? 'Edit Rule' : 'New Rule'}</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        <TextField label="Name" value={name} onChange={e => setName(e.target.value)} fullWidth />
        <TextField
          label="Regex Pattern"
          value={regex}
          onChange={e => setRegex(e.target.value)}
          fullWidth
          helperText="Regular expression to match transaction descriptions"
        />
        <Autocomplete
          options={categories}
          getOptionLabel={c => c.name || ''}
          value={category}
          onChange={(_, v) => setCategory(v)}
          renderInput={params => <TextField {...params} label="Category" />}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave}>Save</Button>
      </DialogActions>
    </Dialog>
  )
}

export default function SettingsPage() {
  const [rules, setRules] = useState<CategoryRule[]>([])
  const [categories, setCategories] = useState<ExpenseCategory[]>([])
  const [editRule, setEditRule] = useState<CategoryRule | null>(null)
  const [createOpen, setCreateOpen] = useState(false)

  const load = useCallback(() => {
    getCategoryRules().then(setRules).catch(console.error)
  }, [])

  useEffect(() => {
    load()
    getExpenseCategories().then(setCategories).catch(console.error)
  }, [load])

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this rule?')) return
    await deleteCategoryRule(id)
    load()
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>Settings — Category Rules</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          New Rule
        </Button>
      </Box>

      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Pattern</TableCell>
            <TableCell>Category</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {rules.map(r => (
            <TableRow key={r.id}>
              <TableCell>{r.name}</TableCell>
              <TableCell><code>{r.ruleRegex}</code></TableCell>
              <TableCell>{categories.find(c => c.id === r.expenseCategoryId)?.name ?? '-'}</TableCell>
              <TableCell>
                <IconButton size="small" onClick={() => setEditRule(r)}>
                  <EditIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" onClick={() => handleDelete(r.id)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
          {rules.length === 0 && (
            <TableRow>
              <TableCell colSpan={4} align="center">No rules configured</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>

      <RuleForm
        open={createOpen || editRule !== null}
        rule={editRule}
        categories={categories}
        onClose={() => { setCreateOpen(false); setEditRule(null) }}
        onSaved={load}
      />
    </Box>
  )
}
