import { computed, type ComputedRef } from 'vue'
import type { AgeHighlight, AnalyticsItem, StudentDto } from '@/types/student'
import {
  formatAngkatanLabel,
  formatOptionLabel,
  getFacultyDisplayLabel,
  getProgramLabel,
  jenjangs
} from '@/utils/studentData'

export type AnalyticsSection = {
  key: string
  title: string
  description: string
  data: AnalyticsItem[]
  empty: string
  maxCount: number
}

export const useStudentAnalytics = (filteredStudents: ComputedRef<StudentDto[]>) => {
  const averageAge = computed(() => {
    if (!filteredStudents.value.length) return 0
    const total = filteredStudents.value.reduce((sum, student) => sum + (student.usia ?? 0), 0)
    return Math.round((total / filteredStudents.value.length) * 10) / 10
  })

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

    return buckets
      .map((bucket) => ({
        label: bucket.label,
        count: filteredStudents.value.reduce((sum, student) => (bucket.test(student.usia ?? 0) ? sum + 1 : sum), 0)
      }))
      .filter((bucket) => bucket.count > 0)
  })

  const ageHighlights = computed<AgeHighlight[]>(() => {
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

  return {
    averageAge,
    prominentAgeSegments,
    leadingProgram,
    topAgeSegment,
    analyticsSections
  }
}

const toAnalyticsItems = (counts: Record<string, number>): AnalyticsItem[] =>
  Object.entries(counts)
    .map(([label, count]) => ({ label, count }))
    .sort((a, b) => b.count - a.count)
