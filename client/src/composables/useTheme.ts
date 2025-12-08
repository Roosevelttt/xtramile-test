import { computed, ref } from 'vue'

type Theme = 'light' | 'dark'

const STORAGE_KEY = 'sms-theme'
const COLOR_SCHEME_QUERY = '(prefers-color-scheme: dark)'
const theme = ref<Theme>('dark')
let preferenceListenerBound = false

const applyThemeClass = (value: Theme) => {
  if (typeof document === 'undefined') return
  const root = document.documentElement
  if (value === 'dark') {
    root.classList.add('dark')
    root.classList.remove('light')
  } else {
    root.classList.remove('dark')
    root.classList.add('light')
  }
}

const persistTheme = (value: Theme) => {
  if (typeof window === 'undefined') return
  window.localStorage.setItem(STORAGE_KEY, value)
}

const resolveInitialTheme = (): Theme => {
  if (typeof window === 'undefined') return 'dark'
  const stored = window.localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') return stored
  return window.matchMedia(COLOR_SCHEME_QUERY).matches ? 'dark' : 'light'
}

const syncTheme = (value: Theme, persist = false) => {
  theme.value = value
  applyThemeClass(value)
  if (persist) persistTheme(value)
}

const bindPreferenceListener = () => {
  if (typeof window === 'undefined' || preferenceListenerBound) return
  const mediaQueryList = window.matchMedia(COLOR_SCHEME_QUERY)
  const handleChange = (event: MediaQueryListEvent) => {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    if (stored === 'light' || stored === 'dark') return
    syncTheme(event.matches ? 'dark' : 'light', false)
  }
  mediaQueryList.addEventListener('change', handleChange)
  preferenceListenerBound = true
}

if (typeof window !== 'undefined') {
  syncTheme(resolveInitialTheme(), false)
  bindPreferenceListener()
}

const setTheme = (value: Theme) => {
  syncTheme(value, true)
}

const toggleTheme = () => {
  setTheme(theme.value === 'dark' ? 'light' : 'dark')
}

export const useTheme = () => {
  const isDark = computed(() => theme.value === 'dark')
  return {
    theme,
    isDark,
    setTheme,
    toggleTheme
  }
}
