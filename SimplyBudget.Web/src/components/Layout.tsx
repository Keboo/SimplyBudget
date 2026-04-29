import { useState } from 'react'
import { Outlet, NavLink } from 'react-router'
import {
  AppBar, Toolbar, Typography, IconButton, Drawer,
  List, ListItemButton, ListItemIcon, ListItemText,
  Box, Tooltip
} from '@mui/material'
import AccountBalanceWalletIcon from '@mui/icons-material/AccountBalanceWallet'
import HistoryIcon from '@mui/icons-material/History'
import AccountBalanceIcon from '@mui/icons-material/AccountBalance'
import SettingsIcon from '@mui/icons-material/Settings'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline'
import { MonthProvider } from '../contexts/MonthContext'
import AddItemDialog from './AddItemDialog'

const NAV = [
  { to: '/', label: 'Budget', icon: <AccountBalanceWalletIcon /> },
  { to: '/history', label: 'History', icon: <HistoryIcon /> },
  { to: '/accounts', label: 'Accounts', icon: <AccountBalanceIcon /> },
  { to: '/settings', label: 'Settings', icon: <SettingsIcon /> },
  { to: '/import', label: 'Import', icon: <UploadFileIcon /> },
]

const DRAWER_WIDTH = 200

export default function Layout() {
  const [addOpen, setAddOpen] = useState(false)

  return (
    <MonthProvider>
      <Box sx={{ display: 'flex' }}>
        <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
          <Toolbar>
            <Typography variant="h6" sx={{ flexGrow: 1 }}>
              SimplyBudget
            </Typography>
            <Tooltip title="Add transaction">
              <IconButton color="inherit" onClick={() => setAddOpen(true)}>
                <AddCircleOutlineIcon />
              </IconButton>
            </Tooltip>
          </Toolbar>
        </AppBar>

        <Drawer
          variant="permanent"
          sx={{
            width: DRAWER_WIDTH,
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' }
          }}
        >
          <Toolbar />
          <List>
            {NAV.map(({ to, label, icon }) => (
              <ListItemButton
                key={to}
                component={NavLink}
                to={to}
                end={to === '/'}
              >
                <ListItemIcon>{icon}</ListItemIcon>
                <ListItemText primary={label} />
              </ListItemButton>
            ))}
          </List>
        </Drawer>

        <Box component="main" sx={{ flexGrow: 1, p: 3, mt: 8, ml: `${DRAWER_WIDTH}px` }}>
          <Outlet />
        </Box>
      </Box>

      <AddItemDialog open={addOpen} onClose={() => setAddOpen(false)} />
    </MonthProvider>
  )
}
