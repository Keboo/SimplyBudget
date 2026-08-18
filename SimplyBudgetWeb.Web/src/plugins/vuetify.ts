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
          // Off-white so surfaces (cards, app bar, etc.) stand out against the page.
          background: '#f5f5f5',
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
          // Off-black background with a lighter surface color so cards, the
          // app bar, and other surfaces are visually distinct from the page.
          background: '#161616',
          surface: '#242424',
        },
      },
    },
  },
})
