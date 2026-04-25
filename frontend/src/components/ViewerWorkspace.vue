<script setup lang="ts">
import Button from 'primevue/button'
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import InputNumber from 'primevue/inputnumber'
import Message from 'primevue/message'
import ProgressBar from 'primevue/progressbar'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import type { MatrixEaseColumnValue, MatrixEasePayload } from '@/services/api'
import type { ViewerBucketForm, ViewerColumnEntry, ViewerSettings } from '@/utils/viewer'
import DependencyMatrix from '@/components/DependencyMatrix.vue'
import QuickChartCanvas from '@/components/QuickChartCanvas.vue'
import { formatNumber } from '@/utils/format'
import { percentageLabel } from '@/utils/viewer'

defineProps<{
  busyMessage: string
  errorMessage: string
  successMessage: string
  payload: MatrixEasePayload | null
  selectedColumn: ViewerColumnEntry | null
  selectedNode: MatrixEaseColumnValue | null
  selectedColumnName: string | null
  selectedValueKey: string | null
  selectionSummary: string
  pendingExpression: string
  settings: ViewerSettings
  bucketForm: ViewerBucketForm
  showingSettings: boolean
  columnEntries: ViewerColumnEntry[]
  visibleColumns: ViewerColumnEntry[]
  renderedColumns: Array<ViewerColumnEntry & { renderValues: MatrixEaseColumnValue[] }>
  measureColumns: Array<{ name: string; index: number }>
  dimensionColumns: Array<{ name: string; index: number }>
  columnStats: Record<string, unknown> | null
  rowReport: { title: string; columns: string[]; data: Array<Array<string | number | boolean | null>> } | null
  duplicateEntries: string[] | null
  dependencyDiagram: Record<string, unknown> | null
  measureStats: Record<string, Record<string | number, string | number | null>> | null
  quickChartData: { labels: string[]; datasets: Array<{ label: string; data: number[] }> } | null
  quickReport: { columns: string[]; data: Array<Array<string | number | boolean | null>> } | null
  applyingFilter: boolean
  refreshingSettings: boolean
  runningColumnAction: boolean
  measuresFiltered: boolean
  measureSelectedIndexes: number[]
  chartFiltered: boolean
  chartType: string
  chartDimensionIndex: number | null
  chartTotalIndexes: number[]
  chartAverageIndexes: number[]
  chartCountIndexes: number[]
}>()

const emit = defineEmits<{
  updatePendingExpression: [value: string]
  updateShowingSettings: [value: boolean]
  updateSetting: [field: keyof ViewerSettings, value: ViewerSettings[keyof ViewerSettings]]
  updateBucketSetting: [field: keyof ViewerBucketForm, value: ViewerBucketForm[keyof ViewerBucketForm]]
  toggleColumnVisibility: [index: number, visible: boolean]
  selectValue: [columnName: string, columnValue: string]
  appendSelection: [mode?: string]
  clearExpression: []
  reload: []
  applyFilter: []
  saveSettings: []
  loadColumnStats: []
  openColumnReport: []
  applyBucketSettings: []
  loadNodeRows: []
  loadDuplicateEntries: []
  loadDependencyDiagram: []
  updateMeasuresFiltered: [value: boolean]
  toggleMeasureIndex: [index: number]
  loadMeasures: []
  updateChartFiltered: [value: boolean]
  updateChartType: [value: string]
  updateChartDimension: [value: number | null]
  toggleChartTotalIndex: [index: number]
  toggleChartAverageIndex: [index: number]
  toggleChartCountIndex: [index: number]
  loadQuickChartOrReport: []
  closeColumnStats: []
  closeRowReport: []
  closeDuplicateEntries: []
  closeDependencyDiagram: []
  closeMeasureStats: []
  closeQuickChartData: []
  closeQuickReport: []
}>()

const chartModes = [
  { label: 'Report', value: 'report' },
  { label: 'Bar', value: 'bar' },
  { label: 'Line', value: 'line' },
  { label: 'Radar', value: 'radar' },
  { label: 'Polar Area', value: 'polarArea' }
]
</script>

<template>
  <div class="viewer-workspace">
    <aside class="viewer-sidebar">
      <Card class="surface-card">
        <template #title>
          <div class="section-header">
            <div>
              <h2>Inspector</h2>
              <p>{{ selectionSummary }}</p>
            </div>
            <Button
              :label="showingSettings ? 'Hide Settings' : 'Settings'"
              severity="secondary"
              text
              @click="emit('updateShowingSettings', !showingSettings)"
            />
          </div>
        </template>

        <div class="stack-sm">
          <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>
          <Message v-if="successMessage" severity="success" :closable="false">{{ successMessage }}</Message>
          <Message v-if="busyMessage" severity="info" :closable="false">{{ busyMessage }}</Message>
        </div>

        <div class="viewer-form">
          <label class="field">
            <span>Selection expression</span>
            <Textarea
              :model-value="pendingExpression"
              auto-resize
              rows="4"
              @update:model-value="emit('updatePendingExpression', String($event))"
            />
          </label>

          <div class="button-grid">
            <Button label="Set" @click="emit('appendSelection', 'overwrite_selection')" />
            <Button label="Clear" severity="secondary" text @click="emit('clearExpression')" />
            <Button label="And" severity="secondary" outlined @click="emit('appendSelection', 'and_selections')" />
            <Button label="Or" severity="secondary" outlined @click="emit('appendSelection', 'or_selection')" />
          </div>

          <div class="button-grid">
            <Button label="Apply Filter" :loading="applyingFilter" @click="emit('applyFilter')" />
            <Button label="Reload" severity="secondary" outlined @click="emit('reload')" />
          </div>
        </div>
      </Card>

      <Card v-if="showingSettings" class="surface-card">
        <template #title><h2>Viewer Settings</h2></template>
        <div class="viewer-form">
          <div class="form-grid">
            <label class="field">
              <span>Low bound</span>
              <InputNumber :model-value="settings.showLowBound" fluid @update:model-value="emit('updateSetting', 'showLowBound', Number($event ?? 0))" />
            </label>
            <label class="field">
              <span>High bound</span>
              <InputNumber :model-value="settings.showHighBound" fluid @update:model-value="emit('updateSetting', 'showHighBound', Number($event ?? 100))" />
            </label>
            <label class="field">
              <span>Percent mode</span>
              <Select
                :model-value="settings.showPercentage"
                :options="[
                  { label: 'Selected total %', value: 'pct_tot_sel' },
                  { label: 'Selected only %', value: 'pct_of_sel' }
                ]"
                option-label="label"
                option-value="value"
                fluid
                @update:model-value="emit('updateSetting', 'showPercentage', String($event))"
              />
            </label>
            <label class="field">
              <span>Selection mode</span>
              <Select
                :model-value="settings.selectOperation"
                :options="[
                  { label: 'Overwrite', value: 'overwrite_selection' },
                  { label: 'And', value: 'and_selections' },
                  { label: 'Or', value: 'or_selection' }
                ]"
                option-label="label"
                option-value="value"
                fluid
                @update:model-value="emit('updateSetting', 'selectOperation', String($event))"
              />
            </label>
          </div>

          <div class="choice-grid">
            <label class="checkbox"><Checkbox binary :model-value="settings.showLowEqual" @update:model-value="emit('updateSetting', 'showLowEqual', Boolean($event))" /><span>Include low bound</span></label>
            <label class="checkbox"><Checkbox binary :model-value="settings.showHighEqual" @update:model-value="emit('updateSetting', 'showHighEqual', Boolean($event))" /><span>Include high bound</span></label>
            <label class="checkbox"><Checkbox binary :model-value="settings.colAscending" @update:model-value="emit('updateSetting', 'colAscending', Boolean($event))" /><span>Sort ascending by count</span></label>
          </div>

          <div class="stack-sm">
            <p class="viewer-section-label">Visible columns</p>
            <label v-for="column in columnEntries" :key="column.name" class="checkbox">
              <Checkbox
                binary
                :model-value="settings.hideColumns[column.Index] !== true"
                @update:model-value="emit('toggleColumnVisibility', column.Index, Boolean($event))"
              />
              <span>{{ column.name }}</span>
            </label>
          </div>

          <Button label="Save Settings" :loading="refreshingSettings" @click="emit('saveSettings')" />
        </div>
      </Card>

      <Card v-if="selectedColumn" class="surface-card">
        <template #title><h2>Column Actions</h2></template>
        <div class="viewer-form">
          <p><strong>{{ selectedColumn.name }}</strong></p>
          <p>{{ selectedColumn.ColType }} • {{ selectedColumn.DataType }}</p>
          <div class="button-grid">
            <Button label="Stats" severity="secondary" outlined :loading="runningColumnAction" @click="emit('loadColumnStats')" />
            <Button label="Report" severity="secondary" outlined @click="emit('openColumnReport')" />
          </div>
          <label class="checkbox">
            <Checkbox binary :model-value="bucketForm.bucketized" @update:model-value="emit('updateBucketSetting', 'bucketized', Boolean($event))" />
            <span>Bucketized</span>
          </label>
          <label class="field">
            <span>Bucket size</span>
            <InputNumber :model-value="bucketForm.bucketSize" fluid @update:model-value="emit('updateBucketSetting', 'bucketSize', Number($event ?? 0))" />
          </label>
          <label class="field">
            <span>Bucket modifier</span>
            <InputNumber :model-value="bucketForm.bucketMod" fluid @update:model-value="emit('updateBucketSetting', 'bucketMod', Number($event ?? 0))" />
          </label>
          <Button label="Apply Bucket Settings" :loading="runningColumnAction" @click="emit('applyBucketSettings')" />
        </div>
      </Card>

      <Card v-if="selectedNode" class="surface-card">
        <template #title><h2>Value Actions</h2></template>
        <div class="stack-sm">
          <p><strong>{{ selectedNode.ColumnValue || '(empty)' }}</strong></p>
          <p>{{ percentageLabel(selectedNode, settings.showPercentage) }}</p>
          <div class="button-grid">
            <Button label="Rows" severity="secondary" outlined :loading="runningColumnAction" @click="emit('loadNodeRows')" />
            <Button label="Duplicates" severity="secondary" outlined :loading="runningColumnAction" @click="emit('loadDuplicateEntries')" />
            <Button label="Dependency" severity="secondary" outlined :loading="runningColumnAction" @click="emit('loadDependencyDiagram')" />
          </div>
        </div>
      </Card>

      <Card v-if="measureColumns.length > 0" class="surface-card">
        <template #title><h2>Measures</h2></template>
        <div class="viewer-form">
          <label class="checkbox"><Checkbox binary :model-value="measuresFiltered" @update:model-value="emit('updateMeasuresFiltered', Boolean($event))" /><span>Use filtered results</span></label>
          <label v-for="column in measureColumns" :key="column.index" class="checkbox">
            <Checkbox binary :model-value="measureSelectedIndexes.includes(column.index)" @update:model-value="emit('toggleMeasureIndex', column.index)" />
            <span>{{ column.name }}</span>
          </label>
          <Button label="Load Measures" :loading="runningColumnAction" @click="emit('loadMeasures')" />
        </div>
      </Card>

      <Card v-if="dimensionColumns.length > 0" class="surface-card">
        <template #title><h2>Quick Charts</h2></template>
        <div class="viewer-form">
          <label class="field">
            <span>Dimension</span>
            <Select
              :model-value="chartDimensionIndex"
              :options="dimensionColumns.map(column => ({ label: column.name, value: column.index }))"
              option-label="label"
              option-value="value"
              show-clear
              fluid
              @update:model-value="emit('updateChartDimension', $event === null ? null : Number($event))"
            />
          </label>
          <label class="field">
            <span>Mode</span>
            <Select
              :model-value="chartType"
              :options="chartModes"
              option-label="label"
              option-value="value"
              fluid
              @update:model-value="emit('updateChartType', String($event))"
            />
          </label>
          <label class="checkbox"><Checkbox binary :model-value="chartFiltered" @update:model-value="emit('updateChartFiltered', Boolean($event))" /><span>Use filtered results</span></label>

          <div class="stack-sm">
            <p class="viewer-section-label">Totals</p>
            <label v-for="column in measureColumns" :key="`tot-${column.index}`" class="checkbox">
              <Checkbox binary :model-value="chartTotalIndexes.includes(column.index)" @update:model-value="emit('toggleChartTotalIndex', column.index)" />
              <span>{{ column.name }}</span>
            </label>
          </div>

          <div class="stack-sm">
            <p class="viewer-section-label">Averages</p>
            <label v-for="column in measureColumns" :key="`avg-${column.index}`" class="checkbox">
              <Checkbox binary :model-value="chartAverageIndexes.includes(column.index)" @update:model-value="emit('toggleChartAverageIndex', column.index)" />
              <span>{{ column.name }}</span>
            </label>
          </div>

          <div class="stack-sm">
            <p class="viewer-section-label">Counts</p>
            <label v-for="column in dimensionColumns" :key="`cnt-${column.index}`" class="checkbox">
              <Checkbox binary :model-value="chartCountIndexes.includes(column.index)" @update:model-value="emit('toggleChartCountIndex', column.index)" />
              <span>{{ column.name }}</span>
            </label>
          </div>

          <Button label="Load Chart / Report" :loading="runningColumnAction" @click="emit('loadQuickChartOrReport')" />
        </div>
      </Card>
    </aside>

    <section class="viewer-main">
      <Card v-if="payload" class="surface-card">
        <template #title>
          <div class="section-header">
            <div>
              <h2>Matrix Columns</h2>
              <p>Select a value to build a filter or inspect the related data.</p>
            </div>
            <div class="viewer-chip-row">
              <Tag :value="`${formatNumber(payload.TotalRows)} rows`" severity="secondary" />
              <Tag :value="`${formatNumber(payload.SelectedRows)} selected`" severity="secondary" />
              <Tag :value="`${visibleColumns.length} visible columns`" severity="secondary" />
            </div>
          </div>
        </template>

        <div class="viewer-columns">
          <article v-for="column in renderedColumns" :key="column.name" class="viewer-column-card">
            <div class="stack-sm">
              <div class="catalog-row__title">
                <h3>{{ column.name }}</h3>
                <Tag :value="column.ColType" severity="contrast" />
              </div>
              <p>{{ column.DataType }} • {{ column.DistinctValues }} distinct values</p>
            </div>

            <div v-if="column.renderValues.length === 0" class="empty-state">
              <p>No values match the current threshold settings.</p>
            </div>

            <button
              v-for="value in column.renderValues"
              :key="`${column.name}-${value.ColumnValue}`"
              class="viewer-value-row"
              :class="{ 'viewer-value-row--active': selectedColumnName === column.name && selectedValueKey === value.ColumnValue }"
              @click="emit('selectValue', column.name, value.ColumnValue)"
            >
              <div>
                <strong>{{ value.ColumnValue || '(empty)' }}</strong>
                <p>{{ percentageLabel(value, settings.showPercentage) }}</p>
                <ProgressBar :value="settings.showPercentage === 'pct_of_sel' ? value.SelectRelPct : value.SelectAllPct" />
              </div>
              <span>{{ formatNumber(value.SelectedValues) }}</span>
            </button>
          </article>
        </div>
      </Card>

      <div class="viewer-panels">
        <Card v-if="columnStats" class="surface-card">
          <template #title>
            <div class="section-header"><h2>Column Statistics</h2><Button icon="pi pi-times" text rounded @click="emit('closeColumnStats')" /></div>
          </template>
          <pre class="viewer-pre">{{ JSON.stringify(columnStats, null, 2) }}</pre>
        </Card>

        <Card v-if="dependencyDiagram" class="surface-card">
          <template #title>
            <div class="section-header"><h2>Dependency Data</h2><Button icon="pi pi-times" text rounded @click="emit('closeDependencyDiagram')" /></div>
          </template>
          <DependencyMatrix :dependency-diagram="dependencyDiagram" />
        </Card>

        <Card v-if="duplicateEntries" class="surface-card">
          <template #title>
            <div class="section-header"><h2>Duplicate Entries</h2><Button icon="pi pi-times" text rounded @click="emit('closeDuplicateEntries')" /></div>
          </template>
          <ul class="viewer-list">
            <li v-for="entry in duplicateEntries" :key="entry">{{ entry }}</li>
          </ul>
        </Card>

        <Card v-if="measureStats" class="surface-card viewer-panel-span">
          <template #title>
            <div class="section-header"><h2>Measure Statistics</h2><Button icon="pi pi-times" text rounded @click="emit('closeMeasureStats')" /></div>
          </template>
          <div class="viewer-table-wrap">
            <table class="viewer-table">
              <thead>
                <tr>
                  <th>Measure</th>
                  <th>Count</th>
                  <th>Total</th>
                  <th>Average</th>
                  <th>Min</th>
                  <th>Max</th>
                  <th>Range</th>
                  <th>Std Dev</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(stats, name) in measureStats" :key="name">
                  <td>{{ name }}</td>
                  <td>{{ stats.Count }}</td>
                  <td>{{ stats.Total }}</td>
                  <td>{{ stats.Average }}</td>
                  <td>{{ stats.Min }}</td>
                  <td>{{ stats.Max }}</td>
                  <td>{{ stats.Range }}</td>
                  <td>{{ stats.StandardDeviation }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </Card>

        <Card v-if="rowReport" class="surface-card viewer-panel-span">
          <template #title>
            <div class="section-header"><h2>{{ rowReport.title }}</h2><Button icon="pi pi-times" text rounded @click="emit('closeRowReport')" /></div>
          </template>
          <div class="viewer-table-wrap">
            <table class="viewer-table">
              <thead><tr><th v-for="column in rowReport.columns" :key="column">{{ column }}</th></tr></thead>
              <tbody>
                <tr v-for="(row, rowIndex) in rowReport.data" :key="rowIndex">
                  <td v-for="(value, valueIndex) in row" :key="valueIndex">{{ value }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </Card>

        <Card v-if="quickReport" class="surface-card viewer-panel-span">
          <template #title>
            <div class="section-header"><h2>Quick Report</h2><Button icon="pi pi-times" text rounded @click="emit('closeQuickReport')" /></div>
          </template>
          <div class="viewer-table-wrap">
            <table class="viewer-table">
              <thead><tr><th v-for="column in quickReport.columns" :key="column">{{ column }}</th></tr></thead>
              <tbody>
                <tr v-for="(row, rowIndex) in quickReport.data" :key="rowIndex">
                  <td v-for="(value, valueIndex) in row" :key="valueIndex">{{ value }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </Card>

        <Card v-if="quickChartData" class="surface-card">
          <template #title>
            <div class="section-header"><h2>Quick Chart Data</h2><Button icon="pi pi-times" text rounded @click="emit('closeQuickChartData')" /></div>
          </template>
          <div class="stack-sm">
            <QuickChartCanvas v-if="chartType !== 'report'" :chart-type="chartType" :chart-data="quickChartData" />
            <div v-for="dataset in quickChartData.datasets" :key="dataset.label" class="stack-sm">
              <p class="viewer-section-label">{{ dataset.label }}</p>
              <div v-for="(label, index) in quickChartData.labels" :key="`${dataset.label}-${label}`" class="viewer-bar-row">
                <span>{{ label }}</span>
                <div class="viewer-bar-track">
                  <div class="viewer-bar-fill" :style="{ width: `${Math.min(100, Math.max(2, Number(dataset.data[index] || 0)))}%` }" />
                </div>
                <strong>{{ dataset.data[index] }}</strong>
              </div>
            </div>
          </div>
        </Card>
      </div>
    </section>
  </div>
</template>
