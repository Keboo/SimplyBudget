import { Routes, Route, Navigate } from 'react-router'
import Layout from './components/Layout'
import BudgetPage from './pages/BudgetPage'
import HistoryPage from './pages/HistoryPage'
import AccountsPage from './pages/AccountsPage'
import SettingsPage from './pages/SettingsPage'
import ImportPage from './pages/ImportPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<BudgetPage />} />
        <Route path="history" element={<HistoryPage />} />
        <Route path="accounts" element={<AccountsPage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route path="import" element={<ImportPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
