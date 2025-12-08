import type { NimMetadata, StudentDto } from '@/types/student'

export const faculties = [
  { code: 'A', name: 'Humaniora dan Industri Kreatif' },
  { code: 'B', name: 'Teknik Sipil & Perencanaan' },
  { code: 'C', name: 'Teknologi Industri' },
  { code: 'D', name: 'School of Business and Management' },
  { code: 'E', name: 'Seni & Desain' },
  { code: 'F', name: 'Ilmu Komunikasi' },
  { code: 'G', name: 'Keguruan dan Ilmu Pendidikan' },
  { code: 'H', name: 'Humaniora dan Industri Kreatif (2023+)' }
]

export const jenjangs = [
  { code: '1', name: 'Strata 1 (S1)' },
  { code: '2', name: 'Strata 2 (S2)' },
  { code: '3', name: 'Strata 3 (S3)' }
]

export const prodiMap: Record<string, Record<string, { code: string; name: string }[]>> = {
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
    '1': [
      { code: '1', name: 'Teknik Elektro' },
      { code: '2', name: 'Teknik Mesin' },
      { code: '3', name: 'Teknik Industri' },
      { code: '4', name: 'Informatika' }
    ],
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
    '1': [
      { code: '1', name: 'Sastra Inggris' },
      { code: '2', name: 'Bahasa Mandarin' },
      { code: '3', name: 'Desain Interior' },
      { code: '4', name: 'DKV' },
      { code: '5', name: 'Ilmu Komunikasi' }
    ],
    '2': [{ code: '1', name: 'Magister Sastra' }]
  }
}

export const facultyNameByCode = faculties.reduce<Record<string, string>>((acc, faculty) => {
  acc[faculty.code] = faculty.name
  return acc
}, {})

export const HUMANIORA_AUTO_VALUE = 'auto-humaniora'
export const humanioraDisplayName = 'Humaniora dan Industri Kreatif'

export const facultySelectOptions = [
  { value: HUMANIORA_AUTO_VALUE, name: humanioraDisplayName },
  ...faculties
    .filter((faculty) => !['A', 'H'].includes(faculty.code))
    .map((faculty) => ({ value: faculty.code, name: faculty.name }))
]

export const formatOptionLabel = (options: { code: string; name: string }[], code?: string) => {
  if (!code) return ''
  const match = options.find((option) => option.code === code)
  return match ? match.name : ''
}

export const isHumanioraCode = (code?: string) => code === 'A' || code === 'H'

export const resolveHumanioraFacultyCode = (angkatan?: string) => {
  const fallbackYear = Number(new Date().getFullYear().toString().slice(-2))
  const numericYear = Number.parseInt(angkatan ?? '', 10)
  const year = Number.isNaN(numericYear) ? fallbackYear : numericYear
  return year >= 23 ? 'H' : 'A'
}

export const getFacultySelectValue = (code?: string) => {
  if (!code) return ''
  return isHumanioraCode(code) ? HUMANIORA_AUTO_VALUE : code
}

export const getFacultyDisplayLabel = (code?: string) => {
  if (!code) return ''
  return isHumanioraCode(code) ? humanioraDisplayName : facultyNameByCode[code] ?? ''
}

export const getProgramLabel = (student: StudentDto) => {
  if (!student.facultyCode || !student.jenjangCode || !student.prodiCode) return ''
  const program = prodiMap[student.facultyCode]?.[student.jenjangCode]?.find((option) => option.code === student.prodiCode)
  return program?.name ?? ''
}

export const formatAngkatanLabel = (angkatan?: string) => {
  if (!angkatan) return ''
  return `20${angkatan}`
}

export const decodeNimMetadata = (nim?: string): NimMetadata => {
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
