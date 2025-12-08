<script setup lang="ts">
import { computed } from 'vue'
import type { AcceptableValue } from 'reka-ui'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

interface StudentDto {
  id: string
  nomorIndukMahasiswa: string
  namaLengkap: string
  usia: number
  facultyLabel?: string
  levelLabel?: string
  programLabel?: string
  yearLabel?: string
}

type MetadataKey = 'facultyLabel' | 'levelLabel' | 'programLabel' | 'yearLabel'

interface Props {
  students: StudentDto[]
  loading?: boolean
  page?: number
  pageSize?: number
  total?: number
  pageSizeOptions?: number[]
}

const metadataFields: ReadonlyArray<{ key: MetadataKey; label: string }> = [
  { key: 'facultyLabel', label: 'Faculty' },
  { key: 'levelLabel', label: 'Level' },
  { key: 'programLabel', label: 'Program' },
  { key: 'yearLabel', label: 'Year' }
]

const getMetadataValue = (student: StudentDto, key: MetadataKey) => {
  const value = student[key]
  if (typeof value !== 'string') return '—'
  return value.trim() === '' ? '—' : value
}

const formatAge = (age?: number) => {
  if (typeof age !== 'number' || Number.isNaN(age) || age <= 0) return '—'
  return `${age} yrs`
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  page: 1,
  pageSize: 10,
  pageSizeOptions: () => [5, 10, 20, 50]
})

const students = computed(() => props.students)
const loading = computed(() => props.loading)
const page = computed(() => (props.page ?? 1) < 1 ? 1 : props.page ?? 1)
const pageSize = computed(() => (props.pageSize ?? 10) < 1 ? 1 : props.pageSize ?? 10)
const pageSizeOptions = computed(() => (props.pageSizeOptions?.length ? props.pageSizeOptions : [5, 10, 20, 50]))
const total = computed(() => (typeof props.total === 'number' ? props.total : students.value.length))
const totalPages = computed(() => {
  if (total.value === 0) return 1
  return Math.max(1, Math.ceil(total.value / pageSize.value))
})
const pageStart = computed(() => {
  if (!total.value) return 0
  return (page.value - 1) * pageSize.value + 1
})
const pageEnd = computed(() => {
  if (!total.value) return 0
  return Math.min(total.value, page.value * pageSize.value)
})

const skeletonRows = [0, 1, 2, 3]

const emit = defineEmits<{
  edit: [id: string]
  delete: [id: string]
  create: []
  'change-page': [page: number]
  'change-page-size': [size: number]
}>()

const handlePageSizeSelect = (value: AcceptableValue) => {
  const parsed = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(parsed) || parsed <= 0) return
  emit('change-page-size', Math.floor(parsed))
}
</script>

<template>
  <section class="rounded-xl border border-white/10 bg-[#03060f]/85 p-6 shadow-[0_45px_120px_rgba(0,0,0,0.65)] backdrop-blur-2xl">
    <header class="flex flex-col gap-4 border-b border-white/10 pb-6 md:flex-row md:items-center md:justify-between">
      <div>
        <h2 class="mt-1 text-2xl font-semibold text-white">Students</h2>
        <p class="text-sm text-white/65">{{ total }} total records</p>
      </div>
    </header>

    <div class="mt-6 space-y-4">
      <template v-if="loading">
        <div
          v-for="row in skeletonRows"
          :key="row"
          class="flex items-center gap-4 rounded-lg border border-white/10 bg-white/5 p-5 text-white/50"
        >
          <div class="h-14 w-14 rounded-lg bg-white/15"></div>
          <div class="flex-1 space-y-3">
            <div class="h-3 w-1/3 rounded-sm bg-white/15"></div>
            <div class="h-3 w-1/4 rounded-sm bg-white/15"></div>
          </div>
          <div class="h-3 w-16 rounded-sm bg-white/15"></div>
        </div>
      </template>

      <template v-else-if="students.length === 0">
        <div class="flex flex-col items-center gap-4 rounded-lg border border-dashed border-white/15 bg-white/5 px-8 py-12 text-center">
          <Button
            type="button"
            class="bg-emerald-400 text-black shadow-[0_0_30px_rgba(16,185,129,0.35)] hover:bg-emerald-300"
            @click="emit('create')"
          >
            Add Student
          </Button>
          <div>
            <p class="text-base font-semibold text-white">No students yet</p>
            <p class="text-sm text-white/65">Use the Add Student button above.</p>
          </div>
        </div>
      </template>

      <template v-else>
        <article
          v-for="student in students"
          :key="student.id"
          class="group rounded-xl border border-white/10 bg-linear-to-br from-white/5 via-white/0 to-transparent p-5 transition-all duration-300 hover:border-white/30 hover:bg-white/10"
        >
          <div class="flex flex-col gap-6 xl:flex-row xl:items-center xl:justify-between">
            <div>
              <p class="text-base font-semibold text-white">{{ student.namaLengkap }}</p>
              <p class="text-xs font-mono text-white/60">{{ student.nomorIndukMahasiswa }}</p>
            </div>

            <div class="flex flex-1 flex-col gap-6 xl:flex-row xl:items-center xl:justify-end">
              <div class="grid flex-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <div
                  v-for="field in metadataFields"
                  :key="field.key"
                  class="min-w-[140px]"
                >
                  <p class="text-xs text-white/55">{{ field.label }}</p>
                  <p class="text-sm font-semibold text-white">{{ getMetadataValue(student, field.key) }}</p>
                </div>
              </div>

              <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:gap-6">
                <div>
                  <p class="text-xs text-white/55">Age</p>
                  <p class="text-base font-semibold text-white">{{ formatAge(student.usia) }}</p>
                </div>
                <div class="flex items-center gap-2">
                  <Button
                    size="sm"
                    variant="ghost"
                    class="text-emerald-200 hover:bg-emerald-400/10 hover:text-emerald-100"
                    @click="emit('edit', student.id)"
                  >
                    Edit
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    class="text-red-300 hover:bg-red-500/10 hover:text-red-100"
                    @click="emit('delete', student.id)"
                  >
                    Delete
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </article>
      </template>
    </div>
  </section>
  <footer class="mt-8 flex flex-col gap-4 border-t border-white/10 pt-6 text-white/70 md:flex-row md:items-center md:justify-between">
    <p class="text-sm">
      <template v-if="total">
        Showing {{ pageStart }}
        <span v-if="pageEnd > pageStart">– {{ pageEnd }}</span>
        of {{ total }} students
      </template>
      <template v-else>
        No students found
      </template>
    </p>
    <div class="flex flex-wrap items-center gap-4">
      <div class="flex items-center gap-2 text-sm">
        <span>Rows per page</span>
        <Select :modelValue="String(pageSize)" @update:modelValue="handlePageSizeSelect">
          <SelectTrigger class="h-9 w-20 border-white/20 bg-white/5 text-white">
            <SelectValue />
          </SelectTrigger>
          <SelectContent class="border-white/15 bg-[#050912]/95 text-white">
            <SelectItem
              v-for="option in pageSizeOptions"
              :key="option"
              :value="String(option)"
            >
              {{ option }}
            </SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div class="flex items-center gap-2 text-sm">
        <Button
          variant="ghost"
          class="border border-white/10 px-3 text-white/80 hover:bg-white/10 hover:text-white"
          :disabled="page <= 1"
          @click="emit('change-page', page - 1)"
        >
          Previous
        </Button>
        <span>Page {{ Math.min(page, totalPages) }} / {{ totalPages }}</span>
        <Button
          variant="ghost"
          class="border border-white/10 px-3 text-white/80 hover:bg-white/10 hover:text-white"
          :disabled="page >= totalPages"
          @click="emit('change-page', page + 1)"
        >
          Next
        </Button>
      </div>
    </div>
  </footer>
</template>
