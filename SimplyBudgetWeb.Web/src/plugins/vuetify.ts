import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

const THEME_STORAGE_KEY = 'SimplyBudgetWebTheme'

function getInitialTheme(): 'light' | 'dark' {
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') {
    return stored
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export const vuetify = createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
  theme: {
    defaultTheme: getInitialTheme(),
    themes: {
      light: {
        dark: false,
        colors: {
          background: '#ffffff',
          surface: '#ffffff',
        },
        variables: {
          // Bumped from the Vuetify default of 0.60 to meet WCAG AA contrast
          // requirements for medium-emphasis text (e.g. field labels).
          'medium-emphasis-opacity': 0.74,
        },
      },
      dark: {
        dark: true,
        colors: {
          background: '#121212',
          surface: '#121212',
        },
      },
    },
  },
})
