<script setup lang="ts">
import Select from 'primevue/select'
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  dependencyDiagram: Record<string, unknown>
}>()

type DependencyColumn = {
  keys: string[]
  matrix: number[][]
}

const columnOptions = computed(() => {
  const columns = props.dependencyDiagram.columns as Record<string, DependencyColumn> | undefined
  return Object.entries(columns ?? {}).map(([label, value]) => ({ label, value }))
})

const selectedColumn = ref<DependencyColumn | null>(null)

watch(columnOptions, options => {
  selectedColumn.value = options[0]?.value ?? null
}, { immediate: true })
</script>

<template>
  <div class="stack-sm">
    <label class="field">
      <span>Dependency column</span>
      <Select
        :model-value="selectedColumn"
        :options="columnOptions"
        option-label="label"
        option-value="value"
        fluid
        @update:model-value="selectedColumn = $event as DependencyColumn | null"
      />
    </label>

    <div v-if="selectedColumn" class="viewer-table-wrap">
      <table class="viewer-table viewer-table--matrix">
        <thead>
          <tr>
            <th>Key</th>
            <th v-for="label in selectedColumn.keys" :key="`head-${label}`">{{ label }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, rowIndex) in selectedColumn.matrix" :key="rowIndex">
            <th>{{ selectedColumn.keys[rowIndex] }}</th>
            <td v-for="(value, valueIndex) in row" :key="valueIndex">{{ value }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
