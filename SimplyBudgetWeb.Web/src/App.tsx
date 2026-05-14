import { Routes, Route, Navigate } from 'react-router-dom'
import { CssBaseline } from '@mui/material'
import { SnackbarProvider } from 'notistack'
import { AuthProvider } from './contexts/AuthContext'
import { ThemeProvider } from './contexts/ThemeContext'
import Layout from './components/Layout'

// Pages
import Budget from './pages/Budget'
import History from './pages/History'
import Accounts from './pages/Accounts'
import Settings from './pages/Settings'
import Import from './pages/Import'

function App() {
  return (
    <ThemeProvider>
      <CssBaseline />
      <SnackbarProvider maxSnack={3}>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<Navigate to="/budget" replace />} />
              <Route path="budget" element={<Budget />} />
              <Route path="history" element={<History />} />
              <Route path="accounts" element={<Accounts />} />
              <Route path="settings" element={<Settings />} />
              <Route path="import" element={<Import />} />
              <Route path="*" element={<Navigate to="/budget" replace />} />
            </Route>
          </Routes>
        </AuthProvider>
      </SnackbarProvider>
    </ThemeProvider>
  )
}

export default App
