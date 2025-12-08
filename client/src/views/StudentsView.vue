<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch, nextTick } from 'vue'
import { useForm } from '@tanstack/vue-form'
import { z } from 'zod'
import StudentTable from '../components/StudentTable.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Calendar } from '@/components/ui/calendar'
import { parseDate, today, getLocalTimeZone } from '@internationalized/date'
import { toast } from '@/components/ui/sonner'
import type { AcceptableValue, DateRange, DateValue } from 'reka-ui'

interface StudentDto {
  id: string
  nomorIndukMahasiswa: string
  namaLengkap: string
  usia: number
  facultyCode?: string
  jenjangCode?: string
  prodiCode?: string
  angkatan?: string
}

interface CreateStudentRequest {
  firstName: string
  lastName: string
  dateOfBirth: string
  facultyCode: string
  jenjangCode: string
  prodiCode: string
  angkatan: string
}

// --- Data Structure for Dropdowns ---
const faculties = [
  { code: 'A', name: 'Humaniora dan Industri Kreatif' },
  { code: 'B', name: 'Teknik Sipil & Perencanaan' },
  { code: 'C', name: 'Teknologi Industri' },
  { code: 'D', name: 'School of Business and Management' },
  { code: 'E', name: 'Seni & Desain' },
  { code: 'F', name: 'Ilmu Komunikasi' },
  { code: 'G', name: 'Keguruan dan Ilmu Pendidikan' },
  { code: 'H', name: 'Humaniora dan Industri Kreatif (2023+)' }
]

const jenjangs = [
  { code: '1', name: 'Strata 1 (S1)' },
  { code: '2', name: 'Strata 2 (S2)' },
  { code: '3', name: 'Strata 3 (S3)' }
]

const facultyNameByCode = faculties.reduce<Record<string, string>>((acc, faculty) => {
  acc[faculty.code] = faculty.name
  return acc
}, {})

const HUMANIORA_AUTO_VALUE = 'auto-humaniora'
const humanioraDisplayName = 'Humaniora dan Industri Kreatif'
const facultySelectOptions = [
  { value: HUMANIORA_AUTO_VALUE, name: humanioraDisplayName },
  ...faculties
    .filter((faculty) => !['A', 'H'].includes(faculty.code))
    .map((faculty) => ({ value: faculty.code, name: faculty.name }))
]

// Map: FacultyCode -> Jenjang -> List of Prodi
const prodiMap: Record<string, Record<string, { code: string; name: string }[]>> = {
  'A': {
    '1': [{ code: '1', name: 'Sastra Inggris' }, { code: '2', name: 'Bahasa Mandarin' }],
    '2': [{ code: '1', name: 'Magister Sastra' }]
  },
  'B': {
    '1': [{ code: '1', name: 'Teknik Sipil' }, { code: '2', name: 'Arsitektur' }],
    '2': [{ code: '1', name: 'Magister Teknik Sipil' }, { code: '2', name: 'Magister Arsitektur' }],
    '3': [{ code: '1', name: 'Doktor Teknik Sipil' }]
  },
  'C': {
    '1': [{ code: '1', name: 'Teknik Elektro' }, { code: '2', name: 'Teknik Mesin' }, { code: '3', name: 'Teknik Industri' }, { code: '4', name: 'Informatika' }],
    '2': [{ code: '1', name: 'Magister Teknik Industri' }]
  },
  'D': {
    '1': [{ code: '1', name: 'Manajemen' }, { code: '2', name: 'Akuntansi' }],
    '2': [{ code: '1', name: 'Magister Manajemen' }]
  },
  'E': {
    '1': [{ code: '1', name: 'Desain Interior' }, { code: '2', name: 'Desain Komunikasi Visual' }]
  },
  'F': {
    '1': [{ code: '1', name: 'Ilmu Komunikasi' }]
  },
  'G': {
    '1': [{ code: '1', name: 'PGSD' }, { code: '2', name: 'PGPAUD' }]
  },
  'H': {
    '1': [{ code: '1', name: 'Sastra Inggris' }, { code: '2', name: 'Bahasa Mandarin' }, { code: '3', name: 'Desain Interior' }, { code: '4', name: 'DKV' }, { code: '5', name: 'Ilmu Komunikasi' }],
    '2': [{ code: '1', name: 'Magister Sastra' }]
  }
}

const formatOptionLabel = (options: { code: string; name: string }[], code?: string) => {
  if (!code) return ''
  const match = options.find((option) => option.code === code)
  return match ? match.name : ''
}

const isHumanioraCode = (code?: string) => code === 'A' || code === 'H'

const resolveHumanioraFacultyCode = (angkatan?: string) => {
  const fallbackYear = Number(new Date().getFullYear().toString().slice(-2))
  const numericYear = Number.parseInt(angkatan ?? '', 10)
  const year = Number.isNaN(numericYear) ? fallbackYear : numericYear
  return year >= 23 ? 'H' : 'A'
}

const getFacultySelectValue = (code?: string) => {
  if (!code) return ''
  return isHumanioraCode(code) ? HUMANIORA_AUTO_VALUE : code
}

const getFacultyDisplayLabel = (code?: string) => {
  if (!code) return ''
  return isHumanioraCode(code) ? humanioraDisplayName : facultyNameByCode[code] ?? ''
}

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

const averageAge = computed(() => {
  if (!filteredStudents.value.length) return 0
  const total = filteredStudents.value.reduce((sum, student) => sum + (student.usia ?? 0), 0)
  return Math.round((total / filteredStudents.value.length) * 10) / 10
})

type AnalyticsItem = {
  label: string
  count: number
}

type NimMetadata = Pick<StudentDto, 'facultyCode' | 'jenjangCode' | 'prodiCode' | 'angkatan'>

type StudentTableRow = StudentDto & {
  facultyLabel: string
  levelLabel: string
  programLabel: string
  yearLabel: string
}

const decodeNimMetadata = (nim?: string): NimMetadata => {
  if (!nim) return {}
  const match = nim.match(/^([A-Za-z])(\d)(\d)(\d{2})/)
  if (!match) return {}
  const [, faculty = '', jenjang = '', prodi = '', angkatan = ''] = match
  return {
    facultyCode: faculty.toUpperCase(),
    jenjangCode: jenjang,
    prodiCode: prodi,
    angkatan
  }
}

const toAnalyticsItems = (counts: Record<string, number>): AnalyticsItem[] =>
  Object.entries(counts)
    .map(([label, count]) => ({ label, count }))
    .sort((a, b) => b.count - a.count)

const getProgramLabel = (student: StudentDto) => {
  if (!student.facultyCode || !student.jenjangCode || !student.prodiCode) return ''
  const program = prodiMap[student.facultyCode]?.[student.jenjangCode]?.find((option) => option.code === student.prodiCode)
  return program?.name ?? ''
}

const formatAngkatanLabel = (angkatan?: string) => {
  if (!angkatan) return ''
  return `20${angkatan}`
}

const facultyDistribution = computed<AnalyticsItem[]>(() => {
  const counts: Record<string, number> = {}
  filteredStudents.value.forEach((student) => {
    if (!student.facultyCode) return
    const label = getFacultyDisplayLabel(student.facultyCode) || `Faculty ${student.facultyCode}`
    counts[label] = (counts[label] ?? 0) + 1
  })
  return toAnalyticsItems(counts)
})

const levelDistribution = computed<AnalyticsItem[]>(() => {
  const counts: Record<string, number> = {}
  filteredStudents.value.forEach((student) => {
    if (!student.jenjangCode) return
    const label = formatOptionLabel(jenjangs, student.jenjangCode) || `Level ${student.jenjangCode}`
    counts[label] = (counts[label] ?? 0) + 1
  })
  return toAnalyticsItems(counts)
})

const yearDistribution = computed<AnalyticsItem[]>(() => {
  const counts: Record<string, number> = {}
  filteredStudents.value.forEach((student) => {
    const label = formatAngkatanLabel(student.angkatan)
    if (!label) return
    counts[label] = (counts[label] ?? 0) + 1
  })
  return toAnalyticsItems(counts)
})

const programDistribution = computed<AnalyticsItem[]>(() => {
  const counts: Record<string, number> = {}
  filteredStudents.value.forEach((student) => {
    const label = getProgramLabel(student)
    if (!label) return
    counts[label] = (counts[label] ?? 0) + 1
  })
  return toAnalyticsItems(counts).slice(0, 5)
})

const ageSegments = computed<AnalyticsItem[]>(() => {
  if (!filteredStudents.value.length) return []
  const buckets = [
    { label: '≤ 20 years', test: (age: number) => age <= 20 },
    { label: '21 - 23 years', test: (age: number) => age >= 21 && age <= 23 },
    { label: '24 - 26 years', test: (age: number) => age >= 24 && age <= 26 },
    { label: '27+ years', test: (age: number) => age >= 27 }
  ]

  const results = buckets.map((bucket) => ({
    label: bucket.label,
    count: filteredStudents.value.reduce((sum, student) => (bucket.test(student.usia ?? 0) ? sum + 1 : sum), 0)
  }))

  return results.filter((bucket) => bucket.count > 0)
})

const ageHighlights = computed(() => {
  const segments = ageSegments.value
  const total = segments.reduce((sum, segment) => sum + segment.count, 0)
  if (!total) return []
  return segments.map((segment) => ({
    ...segment,
    percentage: Math.round((segment.count / total) * 100)
  }))
})

const prominentAgeSegments = computed(() => ageHighlights.value.slice(0, 3))

const leadingProgram = computed(() => programDistribution.value[0])
const topAgeSegment = computed(() => prominentAgeSegments.value[0])

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

type AnalyticsSection = {
  key: string
  title: string
  description: string
  data: AnalyticsItem[]
  empty: string
  maxCount: number
}

const analyticsSections = computed<AnalyticsSection[]>(() => {
  const sections: Omit<AnalyticsSection, 'maxCount'>[] = [
    {
      key: 'faculty',
      title: 'Faculty mix',
      description: 'Where each cohort sits',
      data: facultyDistribution.value,
      empty: 'No students yet'
    },
    {
      key: 'level',
      title: 'Level breakdown',
      description: 'S1 vs S2 vs S3 load',
      data: levelDistribution.value,
      empty: 'No students yet'
    },
    {
      key: 'year',
      title: 'Entry year trend',
      description: 'How batches spread over time',
      data: yearDistribution.value,
      empty: 'No students yet'
    },
    {
      key: 'program',
      title: 'Program spotlight',
      description: 'Top selected study programs',
      data: programDistribution.value,
      empty: 'No students yet'
    }
  ]

  return sections.map((section) => ({
    ...section,
    maxCount: section.data.reduce((max, item) => Math.max(max, item.count), 0)
  }))
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
const statsCardClass = 'rounded-xl border border-white/15 bg-[#070d1b]/90 p-5 shadow-[0_35px_80px_rgba(0,0,0,0.45)] backdrop-blur-xl'

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

        <section class="rounded-xl border border-white/10 bg-[#03060f]/70 p-5 shadow-[0_25px_60px_rgba(0,0,0,0.45)] backdrop-blur-xl">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p class="text-sm font-semibold tracking-wide text-white/70">Overview</p>
              <p class="text-xs text-white/55">Toggle to focus on records.</p>
            </div>
            <Button
              type="button"
              variant="ghost"
              class="border border-white/10 text-white/80 hover:bg-white/10 hover:text-white"
              :aria-pressed="showAnalytics"
              :title="analyticsToggleHint"
              @click="toggleAnalyticsVisibility"
            >
              {{ analyticsToggleLabel }}
            </Button>
          </div>
        </section>

        <div v-if="showAnalytics" class="space-y-6">
          <section class="grid gap-4 lg:grid-cols-4">
            <div :class="statsCardClass">
              <p class="text-sm font-semibold text-white/65">Total students</p>
              <p class="mt-2 text-4xl font-semibold text-white">{{ students.length }}</p>
              <p class="text-xs text-white/55">All records</p>
            </div>
            <div :class="statsCardClass">
              <p class="text-sm font-semibold text-white/65">Average age</p>
              <p class="mt-2 text-4xl font-semibold text-white">{{ averageAge }} yrs</p>
              <p class="text-xs text-white/55">Rounded to one decimal</p>
            </div>
            <div :class="statsCardClass">
              <p class="text-sm font-semibold text-white/65">Top program</p>
              <p class="mt-2 text-2xl font-semibold text-white">
                {{ leadingProgram ? leadingProgram.label : 'Awaiting data' }}
              </p>
              <p class="text-xs text-white/55">
                {{ leadingProgram ? `${leadingProgram.count} students` : '' }}
              </p>
            </div>
            <div :class="statsCardClass">
              <p class="text-sm font-semibold text-white/65">Age balance</p>
              <p class="mt-2 text-3xl font-semibold text-white">
                {{ topAgeSegment ? `${topAgeSegment.percentage}%` : '—' }}
              </p>
              <p class="text-xs text-white/55">
                {{ topAgeSegment ? `${topAgeSegment.label} cohort` : 'No students yet' }}
              </p>
              <div v-if="prominentAgeSegments.length" class="mt-4 space-y-2">
                <div
                  v-for="segment in prominentAgeSegments"
                  :key="segment.label"
                  class="space-y-1"
                >
                  <div class="flex items-center justify-between text-xs text-white/70">
                    <span>{{ segment.label }}</span>
                    <span>{{ segment.percentage }}%</span>
                  </div>
                  <div class="h-1.5 rounded-md bg-white/10">
                    <div
                      class="h-full rounded-md bg-cyan-400"
                      :style="{ width: `${segment.percentage}%` }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section class="grid gap-4 lg:grid-cols-2">
            <div
              v-for="section in analyticsSections"
              :key="section.key"
              :class="statsCardClass"
            >
              <div class="flex flex-col gap-1 text-white/70 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p class="text-sm font-semibold text-white/70">{{ section.title }}</p>
                  <p class="text-xs text-white/60">{{ section.description }}</p>
                </div>
                <p v-if="section.data.length" class="text-xs text-white/55">
                  {{ section.data.reduce((sum, item) => sum + item.count, 0) }} students
                </p>
              </div>
              <div v-if="section.data.length" class="mt-5 space-y-3">
                <div
                  v-for="item in section.data"
                  :key="item.label"
                  class="flex items-center gap-3 text-sm text-white/70"
                >
                  <span class="w-32 shrink-0 truncate font-medium text-white">{{ item.label }}</span>
                  <div class="h-2 flex-1 rounded-md bg-white/10">
                    <div
                      class="h-full rounded-md bg-emerald-400"
                      :style="{
                        width: section.maxCount
                          ? `${Math.max((item.count / section.maxCount) * 100, 12)}%`
                          : '12%'
                      }"
                    ></div>
                  </div>
                  <span class="w-10 text-right text-white">{{ item.count }}</span>
                </div>
              </div>
              <p v-else class="mt-5 text-sm text-white/60">{{ section.empty }}</p>
            </div>
          </section>
        </div>

        <section class="rounded-xl border border-white/10 bg-[#03060f]/80 p-5 shadow-[0_25px_60px_rgba(0,0,0,0.45)] backdrop-blur-xl">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-end">
            <div class="flex-1">
              <label class="text-xs font-semibold text-white/65">Search</label>
              <Input
                type="text"
                placeholder="Search by name, ID, or program"
                :modelValue="searchQuery"
                :class="inputSurfaceClass"
                @update:modelValue="updateSearchQuery"
              />
            </div>
            <div class="flex flex-1 flex-wrap gap-4">
              <div class="min-w-40 flex-1">
                <label class="text-xs font-semibold text-white/65">Faculty</label>
                <Select :modelValue="filterFaculty" @update:modelValue="handleFacultyFilterChange">
                  <SelectTrigger :class="selectTriggerClass">
                    <SelectValue placeholder="All faculties" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem :value="FILTER_ALL_VALUE">All faculties</SelectItem>
                    <SelectItem v-for="faculty in faculties" :key="faculty.code" :value="faculty.code">
                      {{ faculty.name }}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div class="min-w-36 flex-1">
                <label class="text-xs font-semibold text-white/65">Level</label>
                <Select :modelValue="filterLevel" @update:modelValue="handleLevelFilterChange">
                  <SelectTrigger :class="selectTriggerClass">
                    <SelectValue placeholder="All levels" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem :value="FILTER_ALL_VALUE">All levels</SelectItem>
                    <SelectItem v-for="option in jenjangs" :key="option.code" :value="option.code">
                      {{ option.name }}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div class="min-w-36 flex-1">
                <label class="text-xs font-semibold text-white/65">Year</label>
                <Select :modelValue="filterYear" @update:modelValue="handleYearFilterChange">
                  <SelectTrigger :class="selectTriggerClass">
                    <SelectValue placeholder="All years" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem :value="FILTER_ALL_VALUE">All years</SelectItem>
                    <SelectItem
                      v-for="year in availableYearFilters"
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
          <div v-if="hasActiveFilters" class="mt-4 flex justify-end">
            <Button
              type="button"
              variant="ghost"
              class="border border-white/10 text-white/80 hover:bg-white/10"
              @click="clearFilters"
            >
              Clear filters
            </Button>
          </div>
        </section>

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