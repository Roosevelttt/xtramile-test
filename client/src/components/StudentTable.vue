<script setup lang="ts">
import { computed } from 'vue'
import { Button } from '@/components/ui/button'

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

const props = defineProps<Props>()

const students = computed(() => props.students)
const loading = computed(() => props.loading ?? false)

const skeletonRows = [0, 1, 2, 3]

const emit = defineEmits<{
  edit: [id: string]
  delete: [id: string]
  create: []
}>()
</script>

<template>
  <section class="rounded-xl border border-white/10 bg-[#03060f]/85 p-6 shadow-[0_45px_120px_rgba(0,0,0,0.65)] backdrop-blur-2xl">
    <header class="flex flex-col gap-4 border-b border-white/10 pb-6 md:flex-row md:items-center md:justify-between">
      <div>
        <h2 class="mt-1 text-2xl font-semibold text-white">Students</h2>
        <p class="text-sm text-white/65">{{ students.length }} total records</p>
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
</template>
