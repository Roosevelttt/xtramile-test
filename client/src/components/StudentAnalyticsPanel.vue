<script setup lang="ts">
import { Button } from '@/components/ui/button'
import type { AgeHighlight, AnalyticsItem } from '@/types/student'
import type { AnalyticsSection } from '@/composables/useStudentAnalytics'

const props = defineProps<{
  showAnalytics: boolean
  studentsCount: number
  averageAge: number
  leadingProgram?: AnalyticsItem
  topAgeSegment?: AgeHighlight
  prominentAgeSegments: AgeHighlight[]
  analyticsSections: AnalyticsSection[]
  analyticsToggleLabel: string
  analyticsToggleHint: string
}>()

const emit = defineEmits<{ 'toggle-analytics': [] }>()

const statsCardClass = 'rounded-xl border border-white/15 bg-[#070d1b]/90 p-5 shadow-[0_35px_80px_rgba(0,0,0,0.45)] backdrop-blur-xl'
</script>

<template>
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
        :aria-pressed="props.showAnalytics"
        :title="props.analyticsToggleHint"
        @click="emit('toggle-analytics')"
      >
        {{ props.analyticsToggleLabel }}
      </Button>
    </div>
  </section>

  <div v-if="props.showAnalytics" class="space-y-6">
    <section class="grid gap-4 lg:grid-cols-4">
      <div :class="statsCardClass">
        <p class="text-sm font-semibold text-white/65">Total students</p>
        <p class="mt-2 text-4xl font-semibold text-white">{{ props.studentsCount }}</p>
        <p class="text-xs text-white/55">All records</p>
      </div>
      <div :class="statsCardClass">
        <p class="text-sm font-semibold text-white/65">Average age</p>
        <p class="mt-2 text-4xl font-semibold text-white">{{ props.averageAge }} yrs</p>
        <p class="text-xs text-white/55">Rounded to one decimal</p>
      </div>
      <div :class="statsCardClass">
        <p class="text-sm font-semibold text-white/65">Top program</p>
        <p class="mt-2 text-2xl font-semibold text-white">
          {{ props.leadingProgram ? props.leadingProgram.label : 'Awaiting data' }}
        </p>
        <p class="text-xs text-white/55">
          {{ props.leadingProgram ? `${props.leadingProgram.count} students` : '' }}
        </p>
      </div>
      <div :class="statsCardClass">
        <p class="text-sm font-semibold text-white/65">Age balance</p>
        <p class="mt-2 text-3xl font-semibold text-white">
          {{ props.topAgeSegment ? `${props.topAgeSegment.percentage}%` : '—' }}
        </p>
        <p class="text-xs text-white/55">
          {{ props.topAgeSegment ? `${props.topAgeSegment.label} cohort` : 'No students yet' }}
        </p>
        <div v-if="props.prominentAgeSegments.length" class="mt-4 space-y-2">
          <div
            v-for="segment in props.prominentAgeSegments"
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
        v-for="section in props.analyticsSections"
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
</template>
