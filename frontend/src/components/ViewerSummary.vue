<script setup lang="ts">
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import { computed } from 'vue'
import type { MatrixEasePayload } from '@/services/api'
import { formatNumber } from '@/utils/format'
import { summarizePayload } from '@/utils/matrix'
import StatSurface from '@/components/StatSurface.vue'

const props = defineProps<{
  busy: boolean
  errorMessage: string
  mangaName: string
  payload: MatrixEasePayload | null
  exportBusy?: boolean
  deleteBusy?: boolean
}>()

const emit = defineEmits<{
  exportCsv: []
  deleteProject: []
}>()

const summary = computed(() => props.payload ? summarizePayload(props.payload) : null)
</script>

<template>
  <Card class="surface-card">
    <template #title>
      <div class="section-header">
        <div>
          <h2>{{ mangaName || 'Viewer Summary' }}</h2>
          <p>Shared routed page backed by the MatrixEase viewer API.</p>
        </div>
        <div class="actions-row">
          <Button label="Export CSV" severity="secondary" outlined :loading="exportBusy" @click="emit('exportCsv')" />
          <Button label="Delete Project" severity="danger" outlined :loading="deleteBusy" @click="emit('deleteProject')" />
        </div>
      </div>
    </template>

    <div v-if="busy" class="empty-state">
      <p>Loading matrix payload...</p>
    </div>

    <div v-else-if="errorMessage" class="stack-sm">
      <Message severity="error" :closable="false">{{ errorMessage }}</Message>
    </div>

    <div v-else-if="!payload || !summary" class="empty-state">
      <p>No matrix payload is available for this project.</p>
    </div>

    <div v-else class="viewer-grid">
      <StatSurface label="Rows" :value="formatNumber(payload.TotalRows)" caption="Rows in the current matrix payload." />
      <StatSurface label="Selected" :value="formatNumber(payload.SelectedRows)" caption="Rows that match the current selection state." />
      <StatSurface label="Columns" :value="String(summary.columnCount)" caption="Columns returned by the API." />
      <StatSurface label="Measures" :value="String(summary.measureCount)" caption="Columns marked as measures." />
    </div>
  </Card>
</template>
