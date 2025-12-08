<template>
  <div class="relative min-h-screen overflow-hidden bg-[#03050b] text-white">
    <div class="pointer-events-none absolute inset-0">
      <div class="absolute inset-x-0 top-0 h-40 bg-linear-to-b from-white/10 to-transparent"></div>
      <div class="absolute left-1/2 top-12 h-64 w-64 -translate-x-1/2 rounded-[120px] bg-indigo-500/20 blur-[200px]"></div>
      <div class="absolute bottom-0 right-0 h-72 w-72 rounded-[140px] bg-emerald-400/15 blur-[200px]"></div>
    </div>
    <div class="relative z-10">
      <RouterView />
    </div>
    <Toaster
      position="top-right"
      closeButton
      :toastOptions="{
        class: 'group-[.toaster]:border-white/15 group-[.toaster]:bg-[#050912]/95 group-[.toaster]:backdrop-blur-2xl group-[.toaster]:text-white/90',
        closeButton: true
      }"
      class="[--toaster-success-bg:rgba(16,185,129,0.15)] [--toaster-error-bg:rgba(248,113,113,0.18)]"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { Toaster } from '@/components/ui/sonner'

const appTitle = import.meta.env.VITE_APP_TITLE

onMounted(() => {
  document.title = appTitle
})
</script>