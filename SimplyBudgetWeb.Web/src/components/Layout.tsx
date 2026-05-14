import { Outlet, useNavigate } from 'react-router-dom'
import { AppBar, Toolbar, Typography, Button, Box, IconButton, Container } from '@mui/material'
import { Brightness4, Brightness7 } from '@mui/icons-material'
import { useAuth } from '@/contexts/AuthContext'
import { useTheme } from '@/contexts/ThemeContext'

export default function Layout() {
  const navigate = useNavigate()
  const { account, isAuthenticated, login, logout } = useAuth()
  const { mode, toggleTheme } = useTheme()

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography
            variant="h6"
            component="div"
            sx={{ cursor: 'pointer', mr: 3 }}
            onClick={() => navigate('/budget')}
          >
            Simply Budget
          </Typography>

          {isAuthenticated && (
            <Box sx={{ display: 'flex', gap: 1, flexGrow: 1 }}>
              <Button color="inherit" onClick={() => navigate('/budget')}>Budget</Button>
              <Button color="inherit" onClick={() => navigate('/history')}>History</Button>
              <Button color="inherit" onClick={() => navigate('/accounts')}>Accounts</Button>
              <Button color="inherit" onClick={() => navigate('/settings')}>Settings</Button>
              <Button color="inherit" onClick={() => navigate('/import')}>Import</Button>
            </Box>
          )}

          {!isAuthenticated && <Box sx={{ flexGrow: 1 }} />}

          <IconButton
            sx={{ ml: 1 }}
            onClick={toggleTheme}
            color="inherit"
            aria-label={mode === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {mode === 'dark' ? <Brightness7 /> : <Brightness4 />}
          </IconButton>

          {isAuthenticated ? (
            <>
              {account?.name && (
                <Typography variant="body2" sx={{ mx: 1 }}>{account.name}</Typography>
              )}
              <Button color="inherit" onClick={() => logout()}>Sign out</Button>
            </>
          ) : (
            <Button color="inherit" onClick={() => login()}>Sign in</Button>
          )}
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flex: 1, py: 3 }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 2, px: 2, mt: 'auto', backgroundColor: 'background.paper' }}>
        <Typography variant="body2" color="text.secondary" align="center">
          © {new Date().getFullYear()} Simply Budget
        </Typography>
      </Box>
    </Box>
  )
}
