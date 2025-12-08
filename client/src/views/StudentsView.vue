<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch, nextTick } from 'vue'
import { useForm } from '@tanstack/vue-form'
import { z } from 'zod'
import StudentAnalyticsPanel from '@/components/StudentAnalyticsPanel.vue'
import StudentFiltersBar from '@/components/StudentFiltersBar.vue'
import StudentTable from '@/components/StudentTable.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Calendar } from '@/components/ui/calendar'
import { parseDate, today, getLocalTimeZone } from '@internationalized/date'
import { toast } from '@/components/ui/sonner'
import { useStudentAnalytics } from '@/composables/useStudentAnalytics'
import {
  faculties,
  jenjangs,
  prodiMap,
  HUMANIORA_AUTO_VALUE,
  facultySelectOptions,
  formatOptionLabel,
  getFacultyDisplayLabel,
  getFacultySelectValue,
  getProgramLabel,
  resolveHumanioraFacultyCode,
  isHumanioraCode,
  formatAngkatanLabel,
  decodeNimMetadata
} from '@/utils/studentData'
import type { CreateStudentRequest, StudentDto, StudentTableRow } from '@/types/student'
import type { AcceptableValue, DateRange, DateValue } from 'reka-ui'

// State
const students = ref<StudentDto[]>([])
const isEditing = ref(false)
const editingId = ref('')
const loading = ref(false)
const tableLoading = ref(false)
const dobPickerOpen = ref(false)
type CalendarSelection = DateValue | DateValue[] | DateRange | undefined
const dobCalendarValue = ref<CalendarSelection>(undefined)
const dobDisplayFormatter = new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
const dobMaxValue = today(getLocalTimeZone()).subtract({ years: 17 })
const dobMinValue = today(getLocalTimeZone()).subtract({ years: 80 })

const isDateRangeSelection = (value: CalendarSelection): value is DateRange => {
  if (!value || typeof value !== 'object') return false
  return 'start' in value || 'end' in value
}

const extractFirstDateValue = (value: CalendarSelection): DateValue | undefined => {
  if (!value) return undefined
  if (Array.isArray(value)) return value[0]
  if (isDateRangeSelection(value)) return value.start ?? value.end ?? undefined
  return value
}

const dobCalendarBinding = computed({
  get: () => dobCalendarValue.value as DateValue | DateValue[] | undefined,
  set: (value: DateValue | DateValue[] | undefined) => {
    dobCalendarValue.value = value
  }
})

const studentSchema = z
  .object({
    firstName: z
      .string()
      .trim()
      .min(1, 'First Name is required')
      .max(50, 'First Name cannot exceed 50 characters'),
    lastName: z
      .string()
      .max(50, 'Last Name cannot exceed 50 characters'),
    dateOfBirth: z
      .string()
      .min(1, 'Date of Birth is required')
      .refine((value) => {
        const dob = new Date(value)
        if (Number.isNaN(dob.getTime())) return false
        const today = new Date()
        let age = today.getFullYear() - dob.getFullYear()
        const monthDiff = today.getMonth() - dob.getMonth()
        if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) age--
        return age >= 17
      }, 'Student must be at least 17 years old'),
    facultyCode: z.string().optional(),
    jenjangCode: z.string().optional(),
    prodiCode: z.string().optional(),
    angkatan: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    if (isEditing.value) return
    if (!data.facultyCode) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['facultyCode'], message: 'Faculty is required' })
    }
    if (!data.jenjangCode) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['jenjangCode'], message: 'Jenjang is required' })
    }
    if (!data.prodiCode) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['prodiCode'], message: 'Prodi is required' })
    }
    if (!data.angkatan) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['angkatan'], message: 'Angkatan is required' })
    } else if (!/^\d{2}$/.test(data.angkatan)) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['angkatan'], message: 'Angkatan must be 2 digits (e.g., 23)' })
    }
  })

// TanStack Form
const getDefaultFormValues = (): CreateStudentRequest => ({
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  facultyCode: '',
  jenjangCode: '',
  prodiCode: '',
  angkatan: new Date().getFullYear().toString().slice(-2)
})

const form = useForm<CreateStudentRequest>({
  defaultValues: getDefaultFormValues()
})

const formValues = reactive<CreateStudentRequest>({ ...form.state.values })
const createTouchedState = () => ({
  firstName: false,
  lastName: false,
  dateOfBirth: false,
  facultyCode: false,
  jenjangCode: false,
  prodiCode: false,
  angkatan: false
})
const touchedFields = reactive(createTouchedState())
const submitAttempted = ref(false)
type FieldErrors = Partial<Record<keyof CreateStudentRequest, string[]>>
const fieldErrors = ref<FieldErrors>({})

const applySchemaValidation = (value: CreateStudentRequest) => {
  const validation = studentSchema.safeParse(value)
  if (validation.success) {
    fieldErrors.value = {}
    return validation.data as CreateStudentRequest
  }

  fieldErrors.value = validation.error.formErrors.fieldErrors as FieldErrors
  return null
}

const handleSubmit = async () => {
  submitAttempted.value = true
  await nextTick()
  const parsed = applySchemaValidation({ ...formValues })
  if (!parsed) {
    showError('Please fix the highlighted fields.')
    return
  }

  await saveStudentWith(parsed)
}

const shouldShowError = (field: keyof CreateStudentRequest) => submitAttempted.value || touchedFields[field]
const getFieldError = (field: keyof CreateStudentRequest) => {
  if (!shouldShowError(field)) return ''
  return fieldErrors.value[field]?.[0] ?? ''
}
const searchQuery = ref('')
const filterFaculty = ref('')
const filterLevel = ref('')
const filterYear = ref('')
const FILTER_ALL_VALUE = '__all__'
const showAnalytics = ref(true)
const importInputRef = ref<HTMLInputElement | null>(null)
const importLoading = ref(false)
const exportLoading = ref(false)
const pageSizeOptions = [5, 10, 20, 50]
const defaultPageSize = pageSizeOptions[1] ?? pageSizeOptions[0] ?? 10
const pageSize = ref<number>(defaultPageSize)
const currentPage = ref(1)

const analyticsToggleLabel = computed(() => (showAnalytics.value ? 'Hide analytics' : 'Show analytics'))
const analyticsToggleHint = computed(() => (showAnalytics.value ? 'Collapse cohort insights' : 'Reveal cohort insights'))
const toggleAnalyticsVisibility = () => {
  showAnalytics.value = !showAnalytics.value
}

const getApiErrorMessage = async (response: Response) => {
  try {
    const payload = await response.clone().json()
    if (payload) {
      if (typeof payload.message === 'string') return payload.message
      if (typeof payload.Message === 'string') return payload.Message
    }
  } catch {
    /* swallow */
  }

  try {
    const text = await response.text()
    if (text) return text
  } catch {
    /* swallow */
  }

  return 'Request failed. Please try again.'
}

const openImportPicker = () => {
  importInputRef.value?.click()
}

const handleImportFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement | null
  const file = target?.files?.[0]
  if (!file) return

  importLoading.value = true
  try {
    const formData = new FormData()
    formData.append('file', file)
    const response = await fetch(`${apiUrl}/import`, { method: 'POST', body: formData })
    if (!response.ok) {
      throw new Error(await getApiErrorMessage(response))
    }

    const summary = await response.json().catch(() => null)
    const importedCount = summary?.imported ?? summary?.Imported ?? 0
    const duplicates = summary?.duplicates ?? summary?.Duplicates ?? 0
    const invalid = summary?.invalid ?? summary?.Invalid ?? 0

    const parts: string[] = []
    if (importedCount) parts.push(`${importedCount} imported`)
    if (duplicates) parts.push(`${duplicates} duplicate${duplicates === 1 ? '' : 's'} skipped`)
    if (invalid) parts.push(`${invalid} invalid`)

    showSuccess(parts.length ? `Import result: ${parts.join(', ')}` : 'Import completed.')
    await fetchStudents()
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to import students.'
    showError(message)
  } finally {
    importLoading.value = false
    if (importInputRef.value) importInputRef.value.value = ''
  }
}

const downloadBlob = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  window.URL.revokeObjectURL(url)
}

const handleExportStudents = async () => {
  exportLoading.value = true
  try {
    const response = await fetch(`${apiUrl}/export`)
    if (!response.ok) {
      throw new Error(await getApiErrorMessage(response))
    }

    const blob = await response.blob()
    const stamp = new Date().toISOString().split('T')[0]
    downloadBlob(blob, `students-${stamp}.csv`)
    showSuccess('Export ready. Check your downloads.')
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to export students.'
    showError(message)
  } finally {
    exportLoading.value = false
  }
}

const filteredStudents = computed<StudentDto[]>(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return students.value.filter((student) => {
    const matchesSearch = !query
      || [student.namaLengkap, student.nomorIndukMahasiswa, getProgramLabel(student)]
        .join(' ')
        .toLowerCase()
        .includes(query)

    const matchesFaculty = !filterFaculty.value || student.facultyCode === filterFaculty.value
    const matchesLevel = !filterLevel.value || student.jenjangCode === filterLevel.value
    const matchesYear = !filterYear.value || student.angkatan === filterYear.value

    return matchesSearch && matchesFaculty && matchesLevel && matchesYear
  })
})

const totalFilteredStudents = computed(() => filteredStudents.value.length)
const totalPages = computed(() => {
  const size = Math.max(1, pageSize.value || 1)
  const total = totalFilteredStudents.value
  const pages = total ? Math.ceil(total / size) : 1
  return Math.max(1, pages)
})

const { averageAge, prominentAgeSegments, leadingProgram, topAgeSegment, analyticsSections } = useStudentAnalytics(filteredStudents)

const studentTableRows = computed<StudentTableRow[]>(() =>
  filteredStudents.value.map((student) => {
    const facultyLabel = getFacultyDisplayLabel(student.facultyCode) || '—'
    const levelLabel = formatOptionLabel(jenjangs, student.jenjangCode) || '—'
    const programLabel = getProgramLabel(student) || '—'
    const yearLabel = formatAngkatanLabel(student.angkatan) || '—'

    return {
      ...student,
      facultyLabel,
      levelLabel,
      programLabel,
      yearLabel
    }
  })
)

const paginatedStudentTableRows = computed<StudentTableRow[]>(() => {
  if (!studentTableRows.value.length) return []
  const size = Math.max(1, pageSize.value || 1)
  const startIndex = Math.max(0, (currentPage.value - 1) * size)
  return studentTableRows.value.slice(startIndex, startIndex + size)
})

const availableYearFilters = computed(() => {
  const unique = new Map<string, string>()
  students.value.forEach((student) => {
    if (!student.angkatan) return
    if (!unique.has(student.angkatan)) {
      unique.set(student.angkatan, formatAngkatanLabel(student.angkatan))
    }
  })
  return Array.from(unique.entries())
    .map(([value, label]) => ({ value, label }))
    .sort((a, b) => b.value.localeCompare(a.value))
})

const hasActiveFilters = computed(() =>
  Boolean(
    searchQuery.value.trim()
    || filterFaculty.value
    || filterLevel.value
    || filterYear.value
  )
)

const clearFilters = () => {
  searchQuery.value = ''
  filterFaculty.value = ''
  filterLevel.value = ''
  filterYear.value = ''
}

const updateSearchQuery = (value: string | number) => {
  searchQuery.value = typeof value === 'string' ? value : String(value ?? '')
}

const normalizeFilterValue = (value: AcceptableValue) => {
  if (typeof value === 'number') return String(value)
  if (typeof value !== 'string') return ''
  return value === FILTER_ALL_VALUE ? '' : value
}

const setFilterValue = (target: typeof filterFaculty, value: AcceptableValue) => {
  target.value = normalizeFilterValue(value)
}

const handleFacultyFilterChange = (value: AcceptableValue) => setFilterValue(filterFaculty, value)
const handleLevelFilterChange = (value: AcceptableValue) => setFilterValue(filterLevel, value)
const handleYearFilterChange = (value: AcceptableValue) => setFilterValue(filterYear, value)

const handlePageChange = (page: number) => {
  const nextPage = Number.isFinite(page) ? Math.floor(page) : 1
  const clamped = Math.min(Math.max(nextPage, 1), totalPages.value || 1)
  currentPage.value = clamped
}

const handlePageSizeChange = (size: number) => {
  if (!Number.isFinite(size) || size <= 0) return
  pageSize.value = Math.floor(size)
}

watch(filteredStudents, () => {
  currentPage.value = 1
})

watch(pageSize, () => {
  currentPage.value = 1
})

watch(totalPages, (pages) => {
  if (currentPage.value > pages) {
    currentPage.value = pages
  }
})

// Alerts via toasts
const showSuccess = (message: string) => toast.success(message)
const showError = (message: string) => toast.error(message)

// Delete confirmation modal
const deleteModal = ref({
  show: false,
  studentId: ''
})

const formDialogOpen = ref(false)

const apiUrl = `${import.meta.env.VITE_API_URL}/api/students`

const availableProdis = computed(() => {
  if (!formValues.facultyCode || !formValues.jenjangCode) return []
  const facultyProdis = prodiMap[formValues.facultyCode]
  if (!facultyProdis) return []
  return facultyProdis[formValues.jenjangCode] || []
})

const syncFieldValue = <K extends keyof CreateStudentRequest>(key: K, value: CreateStudentRequest[K]) => {
  form.setFieldValue(key, value)
  formValues[key] = value
}

const markFieldTouched = (key: keyof CreateStudentRequest) => {
  if (!touchedFields[key]) touchedFields[key] = true
}

watch(() => formValues.facultyCode, () => {
  if (!isEditing.value && formValues.prodiCode) {
    syncFieldValue('prodiCode', '')
    touchedFields.prodiCode = false
  }
})

watch(() => formValues.jenjangCode, () => {
  if (!isEditing.value && formValues.prodiCode) {
    syncFieldValue('prodiCode', '')
    touchedFields.prodiCode = false
  }
})

watch(() => formValues.angkatan, () => {
  if (!isEditing.value && isHumanioraCode(formValues.facultyCode)) {
    const resolved = resolveHumanioraFacultyCode(formValues.angkatan)
    if (resolved !== formValues.facultyCode) {
      syncFieldValue('facultyCode', resolved)
      void revalidateForm()
    }
  }
})

const revalidateForm = async () => {
  await nextTick()
  applySchemaValidation({ ...formValues })
}

type FieldSlotContext<TValue = unknown> = {
  name: string
  handleChange: (value: TValue) => void
  handleBlur: (event?: FocusEvent) => void
}

const handleFieldChange = <TValue>(field: FieldSlotContext<TValue>, value: TValue) => {
  field.handleChange(value)
  const key = field.name as keyof CreateStudentRequest
  formValues[key] = value as CreateStudentRequest[keyof CreateStudentRequest]
  const eagerFields: Array<keyof CreateStudentRequest> = ['facultyCode', 'jenjangCode', 'prodiCode']
  if (touchedFields[key] || eagerFields.includes(key)) {
    if (eagerFields.includes(key)) markFieldTouched(key)
    void revalidateForm()
  }
}

const handleFacultySelect = (field: FieldSlotContext<string>, value: AcceptableValue) => {
  if (typeof value !== 'string' || !value) return
  const resolvedValue = value === HUMANIORA_AUTO_VALUE ? resolveHumanioraFacultyCode(formValues.angkatan) : value
  handleFieldChange(field, resolvedValue)
}

const handleFieldBlur = <TValue>(field: FieldSlotContext<TValue>, event: FocusEvent) => {
  field.handleBlur(event)
  const key = field.name as keyof CreateStudentRequest
  markFieldTouched(key)
  void revalidateForm()
}

const parseDobToDateValue = (value?: string): DateValue | undefined => {
  if (!value) return undefined
  try {
    return parseDate(value)
  } catch {
    return undefined
  }
}

const formatDobLabel = (value?: string) => {
  if (!value) return ''
  const [year, month, day] = value.split('-').map((segment) => Number(segment))
  if (
    year === undefined || month === undefined || day === undefined ||
    [year, month, day].some((segment) => Number.isNaN(segment))
  ) return ''
  const dateObj = new Date(year, month - 1, day)
  if (Number.isNaN(dateObj.getTime())) return ''
  return dobDisplayFormatter.format(dateObj)
}

const dobDisplayLabel = computed(() => formatDobLabel(formValues.dateOfBirth))

watch(() => formValues.dateOfBirth, (value) => {
  const parsed = parseDobToDateValue(value)
  if (!parsed) {
    dobCalendarValue.value = undefined
    return
  }
  const currentValue = extractFirstDateValue(dobCalendarValue.value as CalendarSelection)
  if (currentValue?.toString() === parsed.toString()) return
  dobCalendarValue.value = parsed
}, { immediate: true })

const handleDobCalendarSelect = (field: FieldSlotContext<string>, value?: CalendarSelection) => {
  const selectedDate = extractFirstDateValue(value)
  if (!selectedDate) return
  markFieldTouched('dateOfBirth')
  handleFieldChange(field, selectedDate.toString())
  dobPickerOpen.value = false
}

const inputSurfaceClass = 'border-white/20 bg-[#060b16] text-white placeholder:text-white/50 focus-visible:border-emerald-300/60 focus-visible:ring-emerald-400/30'
const selectTriggerClass = 'w-full border-white/20 bg-[#060b16] text-white data-[placeholder]:text-white/50 focus-visible:border-emerald-300/60 focus-visible:ring-emerald-400/30'

const fetchStudents = async () => {
  tableLoading.value = true
  try {
    const res = await fetch(apiUrl)
    if (!res.ok) throw new Error('Failed to fetch students')
    const data: StudentDto[] = await res.json()
    students.value = data.map((student) => ({
      ...student,
      ...decodeNimMetadata(student.nomorIndukMahasiswa)
    }))
  } catch (err) {
    console.error('Error fetching students:', err)
    showError('Failed to load students. Please try again.')
  } finally {
    tableLoading.value = false
  }
}


const saveStudentWith = async (value: CreateStudentRequest) => {
  loading.value = true
  try {
    if (isEditing.value) {
      const res = await fetch(`${apiUrl}/${editingId.value}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          firstName: value.firstName,
          lastName: value.lastName,
          dateOfBirth: value.dateOfBirth
        })
      })
      if (!res.ok) throw new Error('Failed to update')
      showSuccess('Student updated successfully!')
    } else {
      const res = await fetch(apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(value)
      })
      if (!res.ok) throw new Error('Failed to create')
      showSuccess('Student added successfully!')
    }

    resetForm()
    formDialogOpen.value = false
    await fetchStudents()
  } catch (err) {
    console.error('Error saving student:', err)
    showError('Failed to save student. Please check your input.')
  } finally {
    loading.value = false
  }
}

const editStudent = async (id: string) => {
  try {
    const res = await fetch(`${apiUrl}/${id}`)
    if (!res.ok) throw new Error('Failed to fetch student')
    const student = await res.json()

    syncFieldValue('firstName', student.firstName)
    syncFieldValue('lastName', student.lastName)
    syncFieldValue('dateOfBirth', student.dateOfBirth.split('T')[0])
    syncFieldValue('facultyCode', '')
    syncFieldValue('jenjangCode', '')
    syncFieldValue('prodiCode', '')
    syncFieldValue('angkatan', '')
    editingId.value = id
    isEditing.value = true
    formDialogOpen.value = true
  } catch (err) {
    console.error('Error fetching student details:', err)
    showError('Failed to load student details.')
  }
}

const confirmDelete = (id: string) => {
  deleteModal.value = { show: true, studentId: id }
}

const deleteStudent = async () => {
  const id = deleteModal.value.studentId
  deleteModal.value.show = false

  try {
    const res = await fetch(`${apiUrl}/${id}`, { method: 'DELETE' })
    if (!res.ok) throw new Error('Failed to delete')
    showSuccess('Student deleted successfully!')
    await fetchStudents()
  } catch (err) {
    console.error('Error deleting student:', err)
    showError('Failed to delete student.')
  }
}

const resetForm = () => {
  const defaults = getDefaultFormValues()
  form.reset(defaults)
  Object.assign(formValues, defaults)
  Object.assign(touchedFields, createTouchedState())
  submitAttempted.value = false
  isEditing.value = false
  editingId.value = ''
  fieldErrors.value = {}
}

const openCreateDialog = () => {
  resetForm()
  formDialogOpen.value = true
}

watch(formDialogOpen, (open) => {
  if (!open) resetForm()
})

onMounted(() => {
  fetchStudents()
})
</script>

<template>
  <TooltipProvider :delayDuration="120">
    <div class="relative min-h-[calc(100vh-64px)] overflow-hidden bg-[#05070d] text-white">
    <div class="pointer-events-none absolute inset-0">
      <div class="absolute inset-x-0 top-0 h-36 bg-linear-to-b from-white/10 to-transparent"></div>
      <div class="absolute -left-24 top-16 h-72 w-72 rounded-[120px] bg-indigo-500/25 blur-[180px]"></div>
      <div class="absolute bottom-10 right-[-60px] h-80 w-80 rounded-[140px] bg-emerald-400/25 blur-[180px]"></div>
    </div>

    <Dialog v-model:open="formDialogOpen">
      <main class="relative z-10 mx-auto max-w-6xl space-y-10 px-4 py-10 sm:px-6 lg:px-10">
        <input
          ref="importInputRef"
          type="file"
          accept=".csv,text/csv"
          class="hidden"
          @change="handleImportFileChange"
        />
        <section class="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 class="text-3xl font-semibold tracking-tight">Student Management</h1>
            <p class="mt-2 text-sm text-white/60">Manage student records</p>
          </div>
          <div class="flex flex-wrap items-center gap-3">
            <Button
              type="button"
              variant="outline"
              class="border-white/20 bg-white/5 text-white/80 shadow-[0_10px_25px_rgba(0,0,0,0.45)] hover:bg-white/10 hover:text-white"
              :disabled="importLoading"
              @click="openImportPicker"
            >
              {{ importLoading ? 'Importing...' : 'Import CSV' }}
            </Button>
            <Button
              type="button"
              variant="outline"
              class="border-white/20 bg-white/5 text-white/80 shadow-[0_10px_25px_rgba(0,0,0,0.45)] hover:bg-white/10 hover:text-white"
              :disabled="exportLoading"
              @click="handleExportStudents"
            >
              {{ exportLoading ? 'Preparing...' : 'Export CSV' }}
            </Button>
            <DialogTrigger asChild>
              <Button
                type="button"
                class="bg-emerald-400 text-black shadow-[0_0_45px_rgba(16,185,129,0.65)] hover:bg-emerald-300"
                @click="openCreateDialog"
              >
                Add Student
              </Button>
            </DialogTrigger>
          </div>
        </section>

        <StudentAnalyticsPanel
          :show-analytics="showAnalytics"
          :students-count="students.length"
          :average-age="averageAge"
          :leading-program="leadingProgram"
          :top-age-segment="topAgeSegment"
          :prominent-age-segments="prominentAgeSegments"
          :analytics-sections="analyticsSections"
          :analytics-toggle-label="analyticsToggleLabel"
          :analytics-toggle-hint="analyticsToggleHint"
          @toggle-analytics="toggleAnalyticsVisibility"
        />

        <StudentFiltersBar
          :search-query="searchQuery"
          :filter-faculty="filterFaculty"
          :filter-level="filterLevel"
          :filter-year="filterYear"
          :faculties="faculties"
          :jenjangs="jenjangs"
          :available-year-filters="availableYearFilters"
          :has-active-filters="hasActiveFilters"
          :input-surface-class="inputSurfaceClass"
          :select-trigger-class="selectTriggerClass"
          :filter-all-value="FILTER_ALL_VALUE"
          @update:search="updateSearchQuery"
          @update:faculty="handleFacultyFilterChange"
          @update:level="handleLevelFilterChange"
          @update:year="handleYearFilterChange"
          @clear-filters="clearFilters"
        />

        <StudentTable
          :students="paginatedStudentTableRows"
          :loading="tableLoading"
          :page="currentPage"
          :page-size="pageSize"
          :total="totalFilteredStudents"
          :page-size-options="pageSizeOptions"
          @edit="editStudent"
          @delete="confirmDelete"
          @create="openCreateDialog"
          @change-page="handlePageChange"
          @change-page-size="handlePageSizeChange"
        />
      </main>

      <DialogContent class="sm:max-w-3xl border border-white/15 bg-[#050912]/95 text-white shadow-[0_40px_120px_rgba(0,0,0,0.65)] backdrop-blur-2xl">
        <DialogHeader>
          <DialogTitle>{{ isEditing ? 'Edit Student' : 'Add New Student' }}</DialogTitle>
        </DialogHeader>
        <form id="student-form" class="space-y-6" @submit.prevent="handleSubmit">
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <form.Field name="firstName" v-slot="{ field }">
              <div class="space-y-2">
                <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">First Name</label>
                <Input
                  :id="field.name"
                  :modelValue="field.state.value"
                  :class="inputSurfaceClass"
                  placeholder="John"
                  :aria-invalid="getFieldError('firstName') ? 'true' : 'false'"
                  @update:modelValue="value => handleFieldChange(field, value)"
                  @blur="(event: FocusEvent) => handleFieldBlur(field, event)"
                />
                <p v-if="getFieldError('firstName')" class="text-xs text-red-300">{{ getFieldError('firstName') }}</p>
              </div>
            </form.Field>

            <form.Field name="lastName" v-slot="{ field }">
              <div class="space-y-2">
                <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Last Name</label>
                <Input
                  :id="field.name"
                  :modelValue="field.state.value"
                  :class="inputSurfaceClass"
                  placeholder="Doe"
                  :aria-invalid="getFieldError('lastName') ? 'true' : 'false'"
                  @update:modelValue="value => handleFieldChange(field, value)"
                  @blur="(event: FocusEvent) => handleFieldBlur(field, event)"
                />
                <p v-if="getFieldError('lastName')" class="text-xs text-red-300">{{ getFieldError('lastName') }}</p>
              </div>
            </form.Field>

            <form.Field name="dateOfBirth" v-slot="{ field }">
              <div class="space-y-2">
                <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Date of Birth</label>
                <Popover v-model:open="dobPickerOpen">
                  <PopoverTrigger asChild>
                    <div class="relative w-full">
                      <Input
                        :id="field.name"
                        type="text"
                        readonly
                        :modelValue="formValues.dateOfBirth ? dobDisplayLabel : ''"
                        placeholder="Select date"
                        :aria-invalid="getFieldError('dateOfBirth') ? 'true' : 'false'"
                        :class="[
                          inputSurfaceClass,
                          'cursor-pointer pr-12',
                          formValues.dateOfBirth ? 'text-white' : 'text-white/70',
                          getFieldError('dateOfBirth') ? 'ring-1 ring-red-400/80' : ''
                        ]"
                      />
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.5"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        class="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/70"
                      >
                        <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                        <line x1="16" y1="2" x2="16" y2="6" />
                        <line x1="8" y1="2" x2="8" y2="6" />
                        <line x1="3" y1="10" x2="21" y2="10" />
                      </svg>
                    </div>
                  </PopoverTrigger>
                  <PopoverContent
                    align="start"
                    class="w-auto border border-white/15 bg-[#050912]/95 p-0 text-white shadow-[0_25px_70px_rgba(0,0,0,0.65)]"
                  >
                    <Calendar
                      v-model="dobCalendarBinding"
                      :maxValue="dobMaxValue"
                      :minValue="dobMinValue"
                      layout="month-and-year"
                      @update:modelValue="value => handleDobCalendarSelect(field, value)"
                    />
                  </PopoverContent>
                </Popover>
                <p v-if="getFieldError('dateOfBirth')" class="text-xs text-red-300">{{ getFieldError('dateOfBirth') }}</p>
              </div>
            </form.Field>

            <template v-if="!isEditing">
              <form.Field name="facultyCode" v-slot="{ field }">
                <div class="space-y-2">
                  <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Faculty</label>
                  <Tooltip :disabled="!field.state.value">
                    <TooltipTrigger asChild>
                      <div class="block">
                        <Select
                          :modelValue="getFacultySelectValue(field.state.value)"
                          :aria-invalid="getFieldError('facultyCode') ? 'true' : 'false'"
                          @update:modelValue="value => handleFacultySelect(field, value)"
                        >
                          <SelectTrigger
                            :id="field.name"
                            :class="[selectTriggerClass, 'overflow-hidden text-ellipsis whitespace-nowrap']"
                          >
                            <SelectValue placeholder="Select faculty" class="block truncate" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem v-for="option in facultySelectOptions" :key="option.value" :value="option.value">
                              {{ option.name }}
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </TooltipTrigger>
                    <TooltipContent v-if="field.state.value" class="max-w-xs text-xs text-white/80">
                      {{ getFacultyDisplayLabel(field.state.value) }}
                    </TooltipContent>
                  </Tooltip>
                  <p v-if="getFieldError('facultyCode')" class="text-xs text-red-300">{{ getFieldError('facultyCode') }}</p>
                </div>
              </form.Field>

              <form.Field name="jenjangCode" v-slot="{ field }">
                <div class="space-y-2">
                  <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Level</label>
                  <Tooltip :disabled="!field.state.value">
                    <TooltipTrigger asChild>
                      <div class="block">
                        <Select
                          :modelValue="field.state.value"
                          :aria-invalid="getFieldError('jenjangCode') ? 'true' : 'false'"
                          @update:modelValue="value => handleFieldChange(field, value)"
                        >
                          <SelectTrigger
                            :id="field.name"
                            :class="[selectTriggerClass, 'overflow-hidden text-ellipsis whitespace-nowrap']"
                          >
                            <SelectValue placeholder="Select level" class="block truncate" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem v-for="j in jenjangs" :key="j.code" :value="j.code">
                              {{ j.name }}
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </TooltipTrigger>
                    <TooltipContent v-if="field.state.value" class="max-w-xs text-xs text-white/80">
                      {{ formatOptionLabel(jenjangs, field.state.value) }}
                    </TooltipContent>
                  </Tooltip>
                  <p v-if="getFieldError('jenjangCode')" class="text-xs text-red-300">{{ getFieldError('jenjangCode') }}</p>
                </div>
              </form.Field>

              <form.Field name="angkatan" v-slot="{ field }">
                <div class="space-y-2">
                  <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Year</label>
                  <Input
                    :id="field.name"
                    maxlength="2"
                    placeholder="e.g., 23"
                    :modelValue="field.state.value"
                    :class="inputSurfaceClass"
                    :aria-invalid="getFieldError('angkatan') ? 'true' : 'false'"
                    @update:modelValue="value => handleFieldChange(field, value)"
                    @blur="(event: FocusEvent) => handleFieldBlur(field, event)"
                  />
                  <p v-if="getFieldError('angkatan')" class="text-xs text-red-300">{{ getFieldError('angkatan') }}</p>
                </div>
              </form.Field>

              <form.Field name="prodiCode" v-slot="{ field }">
                <div class="space-y-2">
                  <label class="text-xs font-semibold tracking-wide text-white/60" :for="field.name">Program</label>
                  <Tooltip :disabled="!field.state.value">
                    <TooltipTrigger asChild>
                      <div class="block">
                        <Select
                          :disabled="availableProdis.length === 0"
                          :modelValue="field.state.value"
                          :aria-invalid="getFieldError('prodiCode') ? 'true' : 'false'"
                          @update:modelValue="value => handleFieldChange(field, value)"
                        >
                          <SelectTrigger
                            :id="field.name"
                            :class="[selectTriggerClass, 'disabled:opacity-40 overflow-hidden text-ellipsis whitespace-nowrap']"
                          >
                            <SelectValue placeholder="Select program" class="block truncate" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem v-for="p in availableProdis" :key="p.code" :value="p.code">
                              {{ p.name }}
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </TooltipTrigger>
                    <TooltipContent v-if="field.state.value" class="max-w-xs text-xs text-white/80">
                      {{ formatOptionLabel(availableProdis, field.state.value) }}
                    </TooltipContent>
                  </Tooltip>
                  <p v-if="getFieldError('prodiCode')" class="text-xs text-red-300">{{ getFieldError('prodiCode') }}</p>
                </div>
              </form.Field>
            </template>
          </div>

          <DialogFooter class="gap-2">
            <Button
              v-if="isEditing"
              type="button"
              variant="ghost"
              class="border border-white/15 bg-white/10 text-white/90 hover:bg-white/15 hover:text-white"
              :disabled="loading"
              @click="() => { resetForm(); formDialogOpen = false }"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              class="bg-emerald-400 text-black hover:bg-emerald-300"
              :disabled="loading"
            >
              {{ isEditing ? (loading ? 'Saving...' : 'Save Changes') : loading ? 'Adding...' : 'Add Student' }}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

    <Dialog v-model:open="deleteModal.show">
      <DialogContent class="border border-white/15 bg-[#050912]/95 text-white shadow-[0_35px_100px_rgba(0,0,0,0.6)] backdrop-blur-2xl">
        <DialogHeader>
          <DialogTitle>Delete Student</DialogTitle>
        </DialogHeader>
        <p class="text-sm text-white/70">Are you sure you want to delete this student? This action cannot be undone.</p>
        <DialogFooter>
          <Button variant="ghost" class="border border-white/15 bg-white/10 text-white/90 hover:bg-white/15" @click="deleteModal.show = false">Cancel</Button>
          <Button variant="destructive" class="bg-red-500 text-white shadow-[0_10px_30px_rgba(239,68,68,0.45)] hover:bg-red-500/90" @click="deleteStudent">Delete</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
    </div>
  </TooltipProvider>
</template>