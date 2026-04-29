import { useState } from 'react'
import {
  Box, Typography, Button, Table, TableHead, TableRow,
  TableCell, TableBody, Autocomplete, TextField, Alert, CircularProgress, Chip
} from '@mui/material'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import CheckIcon from '@mui/icons-material/Check'
import { parseImportFile, confirmImport, getExpenseCategories } from '../services/api'
import type { ImportRow, ExpenseCategory } from '../types/api'
import { useEffect } from 'react'

function formatCurrency(amount: number) {
  return `$${Math.abs(amount).toFixed(2)}`
}

export default function ImportPage() {
  const [rows, setRows] = useState<ImportRow[]>([])
  const [categories, setCategories] = useState<ExpenseCategory[]>([])
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<{ imported: number; skipped: number } | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getExpenseCategories().then(setCategories).catch(console.error)
  }, [])

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setError(null)
    setResult(null)
    setLoading(true)
    try {
      const parsed = await parseImportFile(file)
      setRows(parsed)
    } catch (err) {
      setError(String(err))
    } finally {
      setLoading(false)
    }
    e.target.value = '' // reset input
  }

  const handleCategoryChange = (index: number, cat: ExpenseCategory | null) => {
    setRows(prev => prev.map((r, i) => i === index ? { ...r, categoryId: cat?.id ?? null } : r))
  }

  const handleConfirm = async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await confirmImport(rows)
      setResult(res)
      setRows([])
    } catch (err) {
      setError(String(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>Import Transactions</Typography>

      <Box sx={{ mb: 2 }}>
        <Button
          variant="contained"
          component="label"
          startIcon={<UploadFileIcon />}
          disabled={loading}
        >
          Upload CSV
          <input type="file" accept=".csv" hidden onChange={handleFileChange} />
        </Button>
      </Box>

      {loading && <CircularProgress size={24} />}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {result && (
        <Alert severity="success" icon={<CheckIcon />} sx={{ mb: 2 }}>
          Imported {result.imported} transactions. Skipped {result.skipped} (no category match).
        </Alert>
      )}

      {rows.length > 0 && (
        <>
          <Typography variant="body2" sx={{ mb: 1 }}>
            {rows.length} rows found. Assign categories and click Confirm.
          </Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Date</TableCell>
                <TableCell>Description</TableCell>
                <TableCell align="right">Amount</TableCell>
                <TableCell>Category</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r, i) => (
                <TableRow key={i}>
                  <TableCell>{new Date(r.date).toLocaleDateString()}</TableCell>
                  <TableCell>{r.description}</TableCell>
                  <TableCell align="right">
                    <Chip
                      label={formatCurrency(r.amount)}
                      color={r.amount < 0 ? 'error' : 'success'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Autocomplete
                      options={categories}
                      getOptionLabel={c => c.name || ''}
                      value={categories.find(c => c.id === r.categoryId) ?? null}
                      onChange={(_, v) => handleCategoryChange(i, v)}
                      renderInput={params => <TextField {...params} size="small" placeholder="Select category" />}
                      sx={{ minWidth: 200 }}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <Box sx={{ mt: 2 }}>
            <Button
              variant="contained"
              color="primary"
              onClick={handleConfirm}
              disabled={loading || rows.every(r => r.categoryId === null)}
            >
              Confirm Import
            </Button>
          </Box>
        </>
      )}
    </Box>
  )
}
