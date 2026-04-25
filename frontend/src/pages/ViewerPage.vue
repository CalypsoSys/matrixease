<script setup lang="ts">
import ViewerSummary from '@/components/ViewerSummary.vue'
import ViewerWorkspace from '@/components/ViewerWorkspace.vue'
import { useViewer } from '@/composables/useViewer'
import AppShell from '@/layouts/AppShell.vue'
import type { ViewerSettings } from '@/utils/viewer'
import type { ViewerBucketForm } from '@/utils/viewer'

const props = defineProps<{
  mxesId: string
}>()

const viewer = useViewer(props.mxesId)
const {
  loading,
  errorMessage,
  mangaName,
  payload,
  busyMessage,
  successMessage,
  selectedColumn,
  selectedNode,
  selectedColumnName,
  selectedValueKey,
  selectionSummary,
  pendingExpression,
  settings,
  bucketForm,
  showingSettings,
  columnEntries,
  visibleColumns,
  renderedColumns,
  measureColumns,
  dimensionColumns,
  columnStats,
  rowReport,
  duplicateEntries,
  dependencyDiagram,
  measureStats,
  quickChartData,
  quickReport,
  exportingCsv,
  deletingManga,
  applyingFilter,
  refreshingSettings,
  runningColumnAction,
  measuresPanel,
  chartPanel
} = viewer

function updateSetting(field: keyof ViewerSettings, value: ViewerSettings[keyof ViewerSettings])
{
  switch (field) {
    case 'showLowEqual':
      settings.showLowEqual = Boolean(value)
      break
    case 'showLowBound':
      settings.showLowBound = Number(value)
      break
    case 'showHighEqual':
      settings.showHighEqual = Boolean(value)
      break
    case 'showHighBound':
      settings.showHighBound = Number(value)
      break
    case 'showPercentage':
      settings.showPercentage = String(value)
      break
    case 'selectOperation':
      settings.selectOperation = String(value)
      break
    case 'colAscending':
      settings.colAscending = Boolean(value)
      break
    case 'hideColumns':
      settings.hideColumns = Array.isArray(value) ? [...value] : settings.hideColumns
      break
  }
}

function updateBucketSetting(field: keyof ViewerBucketForm, value: ViewerBucketForm[keyof ViewerBucketForm])
{
  switch (field) {
    case 'bucketized':
      bucketForm.bucketized = Boolean(value)
      break
    case 'bucketSize':
      bucketForm.bucketSize = Number(value)
      break
    case 'bucketMod':
      bucketForm.bucketMod = Number(value)
      break
  }
}
</script>

<template>
  <AppShell
    title="Viewer"
    subtitle="Inspect the fetched matrix payload with a simpler routed page while the rest of the legacy viewer interactions are ported into reusable components."
  >
    <ViewerSummary
      :busy="loading"
      :error-message="errorMessage"
      :manga-name="mangaName"
      :payload="payload"
      :export-busy="exportingCsv"
      :delete-busy="deletingManga"
      @export-csv="viewer.exportSelectedAsCsv"
      @delete-project="viewer.deleteManga"
    />
    <ViewerWorkspace
      v-if="!loading && payload"
      :busy-message="busyMessage"
      :error-message="errorMessage"
      :success-message="successMessage"
      :payload="payload"
      :selected-column="selectedColumn"
      :selected-node="selectedNode"
      :selected-column-name="selectedColumnName"
      :selected-value-key="selectedValueKey"
      :selection-summary="selectionSummary"
      :pending-expression="pendingExpression"
      :settings="settings"
      :bucket-form="bucketForm"
      :showing-settings="showingSettings"
      :column-entries="columnEntries"
      :visible-columns="visibleColumns"
      :rendered-columns="renderedColumns"
      :measure-columns="measureColumns"
      :dimension-columns="dimensionColumns"
      :column-stats="columnStats"
      :row-report="rowReport"
      :duplicate-entries="duplicateEntries"
      :dependency-diagram="dependencyDiagram"
      :measure-stats="measureStats"
      :quick-chart-data="quickChartData"
      :quick-report="quickReport"
      :applying-filter="applyingFilter"
      :refreshing-settings="refreshingSettings"
      :running-column-action="runningColumnAction"
      :measures-filtered="measuresPanel.filtered"
      :measure-selected-indexes="measuresPanel.selectedIndexes"
      :chart-filtered="chartPanel.filtered"
      :chart-type="chartPanel.chartType"
      :chart-dimension-index="chartPanel.dimensionIndex"
      :chart-total-indexes="chartPanel.totalIndexes"
      :chart-average-indexes="chartPanel.averageIndexes"
      :chart-count-indexes="chartPanel.countIndexes"
      @update-pending-expression="pendingExpression = $event"
      @update-showing-settings="showingSettings = $event"
      @update-setting="updateSetting"
      @update-bucket-setting="updateBucketSetting"
      @toggle-column-visibility="(index, visible) => { settings.hideColumns[index] = !visible }"
      @select-value="viewer.selectValue"
      @append-selection="viewer.appendSelection"
      @clear-expression="pendingExpression = ''"
      @reload="viewer.loadViewer"
      @apply-filter="viewer.applyCurrentFilter"
      @save-settings="viewer.saveSettings"
      @load-column-stats="viewer.loadColumnStats"
      @open-column-report="viewer.openColumnReport"
      @apply-bucket-settings="viewer.applyBucketSettings"
      @load-node-rows="viewer.loadNodeRows"
      @load-duplicate-entries="viewer.loadDuplicateEntries"
      @load-dependency-diagram="viewer.loadDependencyDiagram"
      @update-measures-filtered="measuresPanel.filtered = $event"
      @toggle-measure-index="viewer.toggleIndexSelection(measuresPanel.selectedIndexes, $event)"
      @load-measures="viewer.loadMeasures"
      @update-chart-filtered="chartPanel.filtered = $event"
      @update-chart-type="chartPanel.chartType = $event"
      @update-chart-dimension="chartPanel.dimensionIndex = $event"
      @toggle-chart-total-index="viewer.toggleIndexSelection(chartPanel.totalIndexes, $event)"
      @toggle-chart-average-index="viewer.toggleIndexSelection(chartPanel.averageIndexes, $event)"
      @toggle-chart-count-index="viewer.toggleIndexSelection(chartPanel.countIndexes, $event)"
      @load-quick-chart-or-report="viewer.loadQuickChartOrReport"
      @close-column-stats="columnStats = null"
      @close-row-report="rowReport = null"
      @close-duplicate-entries="duplicateEntries = null"
      @close-dependency-diagram="dependencyDiagram = null"
      @close-measure-stats="measureStats = null"
      @close-quick-chart-data="quickChartData = null"
      @close-quick-report="quickReport = null"
    />
  </AppShell>
</template>
