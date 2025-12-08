export interface StudentDto {
  id: string
  nomorIndukMahasiswa: string
  namaLengkap: string
  usia: number
  facultyCode?: string
  jenjangCode?: string
  prodiCode?: string
  angkatan?: string
}

export interface CreateStudentRequest {
  firstName: string
  lastName: string
  dateOfBirth: string
  facultyCode: string
  jenjangCode: string
  prodiCode: string
  angkatan: string
}

export type NimMetadata = Pick<StudentDto, 'facultyCode' | 'jenjangCode' | 'prodiCode' | 'angkatan'>

export interface AnalyticsItem {
  label: string
  count: number
}

export interface AgeHighlight extends AnalyticsItem {
  percentage: number
}

export type StudentTableRow = StudentDto & {
  facultyLabel: string
  levelLabel: string
  programLabel: string
  yearLabel: string
}
