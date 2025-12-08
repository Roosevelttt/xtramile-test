<script setup lang="ts">
import type { AcceptableValue } from 'reka-ui'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

const props = defineProps<{
  searchQuery: string
  filterFaculty: string
  filterLevel: string
  filterYear: string
  faculties: { code: string; name: string }[]
  jenjangs: { code: string; name: string }[]
  availableYearFilters: { value: string; label: string }[]
  hasActiveFilters: boolean
  inputSurfaceClass: string
  selectTriggerClass: string
  filterAllValue: string
}>()

const emit = defineEmits<{
  'update:search': [value: string]
  'update:faculty': [value: AcceptableValue]
  'update:level': [value: AcceptableValue]
  'update:year': [value: AcceptableValue]
  'clear-filters': []
}>()

const handleSearchUpdate = (value: string | number) => {
  emit('update:search', typeof value === 'string' ? value : String(value ?? ''))
}
</script>

<template>
  <section class="rounded-xl border border-slate-200/70 bg-white/90 p-5 text-slate-900 shadow-[0_25px_60px_rgba(15,23,42,0.08)] backdrop-blur-xl dark:border-white/10 dark:bg-[#03060f]/80 dark:text-white dark:shadow-[0_25px_60px_rgba(0,0,0,0.45)]">
    <div class="flex flex-col gap-4 lg:flex-row lg:items-end">
      <div class="flex-1">
        <label class="text-xs font-semibold text-slate-600 dark:text-white/65">Search</label>
        <Input
          type="text"
          placeholder="Search by name, ID, or program"
          :modelValue="props.searchQuery"
          :class="props.inputSurfaceClass"
          @update:modelValue="handleSearchUpdate"
        />
      </div>
      <div class="flex flex-1 flex-wrap gap-4">
        <div class="min-w-40 flex-1">
          <label class="text-xs font-semibold text-slate-600 dark:text-white/65">Faculty</label>
          <Select :modelValue="props.filterFaculty" @update:modelValue="value => emit('update:faculty', value)">
            <SelectTrigger :class="props.selectTriggerClass">
              <SelectValue placeholder="All faculties" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem :value="props.filterAllValue">All faculties</SelectItem>
              <SelectItem v-for="faculty in props.faculties" :key="faculty.code" :value="faculty.code">
                {{ faculty.name }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div class="min-w-36 flex-1">
          <label class="text-xs font-semibold text-slate-600 dark:text-white/65">Level</label>
          <Select :modelValue="props.filterLevel" @update:modelValue="value => emit('update:level', value)">
            <SelectTrigger :class="props.selectTriggerClass">
              <SelectValue placeholder="All levels" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem :value="props.filterAllValue">All levels</SelectItem>
              <SelectItem v-for="option in props.jenjangs" :key="option.code" :value="option.code">
                {{ option.name }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div class="min-w-36 flex-1">
          <label class="text-xs font-semibold text-slate-600 dark:text-white/65">Year</label>
          <Select :modelValue="props.filterYear" @update:modelValue="value => emit('update:year', value)">
            <SelectTrigger :class="props.selectTriggerClass">
              <SelectValue placeholder="All years" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem :value="props.filterAllValue">All years</SelectItem>
              <SelectItem
                v-for="year in props.availableYearFilters"
                :key="year.value"
                :value="year.value"
              >
                {{ year.label }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>
    </div>
    <div v-if="props.hasActiveFilters" class="mt-4 flex justify-end">
      <Button
        type="button"
        variant="ghost"
        class="border border-slate-300/70 text-slate-700 hover:bg-slate-100 hover:text-slate-900 dark:border-white/10 dark:text-white/80 dark:hover:bg-white/10"
        @click="emit('clear-filters')"
      >
        Clear filters
      </Button>
    </div>
  </section>
</template>
