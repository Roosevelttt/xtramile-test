<script setup lang="ts">
import { computed } from 'vue'
import { Moon, Sun } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/composables/useTheme'

const { theme, toggleTheme } = useTheme()
const isDark = computed(() => theme.value === 'dark')
const label = computed(() => (isDark.value ? 'Switch to light mode' : 'Switch to dark mode'))
</script>

<template>
  <Button
    type="button"
    variant="ghost"
    class="group flex items-center gap-2 rounded-full border border-slate-300/80 bg-white/80 px-4 py-2 text-sm font-medium text-slate-900 shadow-[0_8px_24px_rgba(15,23,42,0.08)] transition dark:border-white/20 dark:bg-white/10 dark:text-white dark:shadow-[0_8px_24px_rgba(0,0,0,0.45)]"
    :aria-pressed="isDark"
    :title="label"
    @click="toggleTheme"
  >
    <Sun v-if="!isDark" class="h-4 w-4" />
    <Moon v-else class="h-4 w-4" />
    <span>{{ isDark ? 'Dark mode' : 'Light mode' }}</span>
  </Button>
</template>
