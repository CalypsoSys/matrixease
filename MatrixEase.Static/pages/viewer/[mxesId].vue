<script setup lang="ts">
const route = useRoute()
const api = useMatrixEaseApi()
const session = useMatrixEaseSession()

const loading = ref(true)
const errorMessage = ref('')
const successMessage = ref('')
const busyMessage = ref('')
const mangaName = ref('')
const payload = ref<MatrixEasePayload | null>(null)
const showingSettings = ref(false)
const selectedColumnName = ref<string | null>(null)
const selectedValueKey = ref<string | null>(null)
const pendingExpression = ref('')
const applyingFilter = ref(false)
const refreshingSettings = ref(false)
const runningColumnAction = ref(false)
const columnStats = ref<Record<string, any> | null>(null)
const rowReport = ref<{ title: string, columns: string[], data: Array<Array<any>> } | null>(null)
const duplicateEntries = ref<string[] | null>(null)
const dependencyDiagram = ref<Record<string, any> | null>(null)
const measureStats = ref<Record<string, Record<string, string | number | null>> | null>(null)
const quickChartData = ref<{ labels: string[], datasets: Array<{ label: string, data: number[] }> } | null>(null)
const quickReport = ref<{ columns: string[], data: Array<Array<any>> } | null>(null)

const settings = reactive({
  showLowEqual: true,
  showLowBound: 0,
  showHighEqual: true,
  showHighBound: 100,
  showPercentage: 'pct_tot_sel',
  selectOperation: 'overwrite_selection',
  colAscending: false,
  hideColumns: [] as boolean[]
})

const bucketForm = reactive({
  bucketized: false,
  bucketSize: 0,
  bucketMod: 0
})

const measuresPanel = reactive({
  filtered: true,
  selectedIndexes: [] as number[]
})

const chartPanel = reactive({
  filtered: true,
  chartType: 'bar',
  dimensionIndex: null as number | null,
  totalIndexes: [] as number[],
  averageIndexes: [] as number[],
  countIndexes: [] as number[]
})

const svgConfig = {
  columnWidth: 280,
  columnGap: 20,
  headerHeight: 42,
  nodeHeight: 68,
  nodeGap: 10
}

const columnEntries = computed(() =>
{
  if (!payload.value) {
    return []
  }

  return Object.entries(payload.value.Columns)
    .map(([name, column]) => ({
      name,
      ...column
    }))
    .sort((left, right) => left.Index - right.Index)
})

const visibleColumns = computed(() =>
{
  if (!payload.value) {
    return columnEntries.value
  }

  return columnEntries.value.filter(column => payload.value?.HideColumns[column.Index] !== true)
})

const selectedColumn = computed(() =>
{
  if (!selectedColumnName.value) {
    return null
  }

  return visibleColumns.value.find(column => column.name === selectedColumnName.value) ?? null
})

const selectedNode = computed(() =>
{
  if (!selectedColumn.value || !selectedValueKey.value) {
    return null
  }

  return selectedColumn.value.Values.find(value => value.ColumnValue === selectedValueKey.value) ?? null
})

const selectedNodeToken = computed(() =>
{
  if (!selectedColumn.value || !selectedNode.value) {
    return ''
  }

  return `${selectedNode.value.ColumnValue}@${selectedColumn.value.name}:${selectedColumn.value.Index}`
})

const selectionSummary = computed(() =>
{
  if (!selectedColumn.value || !selectedNode.value) {
    return 'Select a value to inspect it and build filters.'
  }

  return `${selectedNode.value.ColumnValue || '(empty)'} @ ${selectedColumn.value.name}`
})

const measureColumns = computed(() =>
{
  return visibleColumns.value
    .filter(column => column.ColType === 'Measure')
    .map(column => ({ name: column.name, index: column.Index }))
})

const dimensionColumns = computed(() =>
{
  return visibleColumns.value
    .filter(column => column.ColType !== 'Measure' || column.DistinctValues <= 100)
    .map(column => ({ name: column.name, index: column.Index }))
})

const showValue = (selectRelPct: number) =>
{
  if (selectRelPct > settings.showLowBound && selectRelPct <= settings.showHighBound) {
    return true
  }

  if (settings.showLowEqual && selectRelPct === settings.showLowBound) {
    return true
  }

  if (settings.showHighEqual && selectRelPct === settings.showHighBound) {
    return true
  }

  return false
}

const renderedValuesForColumn = (column: MatrixEaseColumn) =>
{
  return column.Values
    .filter(value => showValue(value.SelectRelPct))
    .slice(0, 24)
}

const svgColumns = computed(() =>
{
  return visibleColumns.value.map((column, visualIndex) => ({
    ...column,
    visualIndex,
    renderValues: renderedValuesForColumn(column)
  }))
})

const maxRenderedRows = computed(() =>
{
  return Math.max(1, ...svgColumns.value.map(column => column.renderValues.length))
})

const svgWidth = computed(() =>
{
  const count = Math.max(svgColumns.value.length, 1)
  return count * svgConfig.columnWidth + Math.max(count - 1, 0) * svgConfig.columnGap + 24
})

const svgHeight = computed(() =>
{
  return svgConfig.headerHeight + 24 + maxRenderedRows.value * (svgConfig.nodeHeight + svgConfig.nodeGap) + 24
})

const svgXForColumn = (visualIndex: number) => visualIndex * (svgConfig.columnWidth + svgConfig.columnGap)
const svgYForValue = (valueIndex: number) => svgConfig.headerHeight + 20 + valueIndex * (svgConfig.nodeHeight + svgConfig.nodeGap)

const percentageLabel = (value: MatrixEaseColumnValue) =>
{
  if (settings.showPercentage === 'pct_of_sel') {
    return `Selected ${value.SelectRelPct.toFixed(2)}%`
  }

  return `Selected total ${value.SelectAllPct.toFixed(2)}%`
}

const svgBarWidth = (value: MatrixEaseColumnValue) =>
{
  const metric = settings.showPercentage === 'pct_of_sel' ? value.SelectRelPct : value.SelectAllPct
  return Math.max(10, Math.min(svgConfig.columnWidth - 20, ((svgConfig.columnWidth - 20) * metric) / 100))
}

const svgTotalBarWidth = (value: MatrixEaseColumnValue) =>
{
  return Math.max(10, Math.min(svgConfig.columnWidth - 20, ((svgConfig.columnWidth - 20) * value.TotalPct) / 100))
}

const trimSvgText = (value: string, maxLength: number) =>
{
  if (value.length <= maxLength) {
    return value
  }

  return `${value.slice(0, Math.max(0, maxLength - 1))}…`
}

const syncSettingsFromPayload = () =>
{
  if (!payload.value) {
    return
  }

  settings.showLowEqual = payload.value.ShowLowEqual
  settings.showLowBound = payload.value.ShowLowBound
  settings.showHighEqual = payload.value.ShowHighEqual
  settings.showHighBound = payload.value.ShowHighBound
  settings.showPercentage = payload.value.ShowPercentage
  settings.selectOperation = payload.value.SelectOperation
  settings.colAscending = payload.value.ColAscending
  settings.hideColumns = [...payload.value.HideColumns]
  pendingExpression.value = payload.value.SelectionExpression ?? ''

  if (selectedColumn.value) {
    bucketForm.bucketized = selectedColumn.value.Bucketized
    bucketForm.bucketSize = selectedColumn.value.CurBucketSize
    bucketForm.bucketMod = selectedColumn.value.CurBucketMod
  }
}

const loadViewer = async () =>
{
  loading.value = true
  errorMessage.value = ''

  try {
    if (!session.matrixEaseId.value) {
      await session.refreshBootstrap()
    }

    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getManga(session.matrixEaseId.value, mxesId)
    mangaName.value = response.MangaName
    payload.value = response.MangaData
    syncSettingsFromPayload()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load MatrixEase viewer.'
  } finally {
    loading.value = false
  }
}

const selectValue = (columnName: string, value: MatrixEaseColumnValue) =>
{
  selectedColumnName.value = columnName
  selectedValueKey.value = value.ColumnValue

  const column = visibleColumns.value.find(item => item.name === columnName)
  if (column) {
    bucketForm.bucketized = column.Bucketized
    bucketForm.bucketSize = column.CurBucketSize
    bucketForm.bucketMod = column.CurBucketMod
  }
}

const appendSelection = (mode?: string) =>
{
  if (!selectedColumn.value || !selectedNode.value) {
    errorMessage.value = 'Select a value before building a filter.'
    return
  }

  const expression = `"${selectedNode.value.ColumnValue}@${selectedColumn.value.name}:${selectedColumn.value.Index}"`
  const operation = mode ?? settings.selectOperation

  if (pendingExpression.value && (operation === 'or_selection' || operation === 'and_selections')) {
    pendingExpression.value = `${pendingExpression.value}${operation === 'or_selection' ? ' OR ' : ' AND '}${expression}`
  } else {
    pendingExpression.value = expression
  }
}

const clearExpression = () =>
{
  pendingExpression.value = ''
}

const refreshMatrixFromPickup = async (pickupKey: string) =>
{
  if (!session.matrixEaseId.value) {
    return
  }

  const mxesId = String(route.params.mxesId ?? '')
  for (let attempt = 0; attempt < 120; attempt += 1) {
    const pickup = await api.getPickupStatus(session.matrixEaseId.value, mxesId, pickupKey)
    if (!pickup.Success) {
      throw new Error(pickup.Message ?? 'Failed while waiting for matrix update.')
    }

    if (pickup.Complete && pickup.Results) {
      payload.value = pickup.Results
      syncSettingsFromPayload()
      return
    }

    await new Promise(resolve => setTimeout(resolve, 1000))
  }

  throw new Error('Timed out waiting for updated matrix results.')
}

const applyCurrentFilter = async () =>
{
  if (!session.matrixEaseId.value) {
    return
  }

  applyingFilter.value = true
  errorMessage.value = ''
  successMessage.value = ''
  busyMessage.value = 'Applying filter and waiting for the matrix to refresh.'

  try {
    const mxesId = String(route.params.mxesId ?? '')
    const result = await api.applyFilter(session.matrixEaseId.value, mxesId, pendingExpression.value)
    if (!result.Success || !result.PickupKey) {
      throw new Error(result.Error ?? 'Failed to start filter update.')
    }

    await refreshMatrixFromPickup(result.PickupKey)
    successMessage.value = 'Matrix filter updated.'
    busyMessage.value = ''
  } catch (error) {
    busyMessage.value = ''
    errorMessage.value = error instanceof Error ? error.message : 'Failed to apply filter.'
  } finally {
    applyingFilter.value = false
  }
}

const saveSettings = async () =>
{
  if (!session.matrixEaseId.value) {
    return
  }

  refreshingSettings.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.updateSettings(session.matrixEaseId.value, mxesId, settings)
    if (!response.Success) {
      throw new Error('Failed to save viewer settings.')
    }

    await loadViewer()
    successMessage.value = 'Viewer settings updated.'
    showingSettings.value = false
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to save settings.'
  } finally {
    refreshingSettings.value = false
  }
}

const toggleIndexSelection = (list: number[], index: number) =>
{
  const existingIndex = list.indexOf(index)
  if (existingIndex >= 0) {
    list.splice(existingIndex, 1)
  } else {
    list.push(index)
  }
}

const runColumnAction = async (action: () => Promise<void>) =>
{
  runningColumnAction.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await action()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Viewer action failed.'
  } finally {
    runningColumnAction.value = false
  }
}

const applyBucketSettings = async () =>
{
  if (!selectedColumn.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    const mxesId = String(route.params.mxesId ?? '')
    const result = await api.bucketizeColumn(session.matrixEaseId.value, mxesId, {
      columnName: selectedColumn.value.name,
      columnIndex: selectedColumn.value.Index,
      bucketized: bucketForm.bucketized,
      bucketSize: bucketForm.bucketSize,
      bucketMod: bucketForm.bucketMod
    })

    if (!result.Success || !result.PickupKey) {
      throw new Error(result.Error ?? 'Bucketize request failed.')
    }

    busyMessage.value = 'Rebucketing column and waiting for matrix refresh.'
    await refreshMatrixFromPickup(result.PickupKey)
    busyMessage.value = ''
    successMessage.value = `Updated bucket settings for ${selectedColumn.value?.name}.`
  })
}

const loadColumnStats = async () =>
{
  if (!selectedColumn.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getDetailedColumnStats(session.matrixEaseId.value, mxesId, selectedColumn.value.name, selectedColumn.value.Index)
    if (!response.Success || !response.ColStats) {
      throw new Error('Failed to load column statistics.')
    }

    columnStats.value = response.ColStats
  })
}

const openColumnReport = () =>
{
  if (!selectedColumn.value) {
    return
  }

  rowReport.value = {
    title: `Column ${selectedColumn.value.name} report`,
    columns: ['Value', 'Percent of Total', 'Sel Pct of Total', 'Pct of Sel', 'Rows', 'Selected Rows'],
    data: selectedColumn.value.Values.map(value => [
      value.ColumnValue,
      value.TotalPct,
      value.SelectAllPct,
      value.SelectRelPct,
      value.TotalValues,
      value.SelectedValues
    ])
  }
}

const loadNodeRows = async () =>
{
  if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    if ((selectedNode.value?.SelectedValues ?? 0) > 10000) {
      throw new Error('Too many rows for quick view. Export the selection instead.')
    }

    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getNodeRows(session.matrixEaseId.value, mxesId, selectedColumn.value.Index, selectedNodeToken.value)
    if (!response.Success || !response.ReportData) {
      throw new Error('Failed to load rows for the selected value.')
    }

    rowReport.value = {
      title: `${selectedColumn.value.name} rows`,
      columns: response.ReportData.columns,
      data: response.ReportData.data
    }
  })
}

const loadDuplicateEntries = async () =>
{
  if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getDuplicateEntries(session.matrixEaseId.value, mxesId, selectedColumn.value.Index, selectedNodeToken.value)
    if (!response.Success || !response.DuplicateEntries) {
      throw new Error('Failed to load duplicate entries.')
    }

    duplicateEntries.value = response.DuplicateEntries
  })
}

const loadDependencyDiagram = async () =>
{
  if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getDependencyDiagram(session.matrixEaseId.value, mxesId, selectedColumn.value.Index, selectedNodeToken.value)
    if (!response.Success || !response.DependencyDiagram) {
      throw new Error('Failed to load dependency diagram data.')
    }

    dependencyDiagram.value = response.DependencyDiagram
  })
}

const loadMeasures = async () =>
{
  if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getColumnMeasures(
      session.matrixEaseId.value,
      mxesId,
      selectedColumn.value.Index,
      selectedNodeToken.value,
      measuresPanel.selectedIndexes,
      measuresPanel.filtered
    )

    if (!response.Success || !response.MeasureStats) {
      throw new Error('Failed to load measure statistics.')
    }

    measureStats.value = response.MeasureStats
  })
}

const loadQuickChartOrReport = async () =>
{
  if (!session.matrixEaseId.value) {
    return
  }

  await runColumnAction(async () =>
  {
    if (chartPanel.dimensionIndex === null) {
      throw new Error('Choose one dimension before loading a chart or report.')
    }

    const totalMeasureIndexes = [...chartPanel.totalIndexes]
    const averageMeasureIndexes = [...chartPanel.averageIndexes]
    const countIndexes = [...chartPanel.countIndexes]

    if ((totalMeasureIndexes.length + averageMeasureIndexes.length + countIndexes.length) === 0) {
      throw new Error('Choose at least one measure or count column.')
    }

    const mxesId = String(route.params.mxesId ?? '')
    const response = await api.getQuickChartData(session.matrixEaseId.value, mxesId, {
      chartType: chartPanel.chartType,
      dimensionIndexes: [chartPanel.dimensionIndex],
      totalMeasureIndexes,
      averageMeasureIndexes,
      countMeasureIndexes: countIndexes,
      filtered: chartPanel.filtered
    })

    if (!response.Success) {
      throw new Error('Failed to load quick chart/report data.')
    }

    quickChartData.value = response.ChartData ?? null
    quickReport.value = response.ReportData ?? null
  })
}

onMounted(async () =>
{
  await loadViewer()
})
</script>

<template>
  <main class="h-screen overflow-hidden bg-slate-100 text-slate-900">
    <div class="grid h-full grid-rows-[auto_auto_minmax(0,1fr)]">
      <header class="border-b border-slate-200 bg-white">
        <div class="grid gap-3 px-3 py-2 xl:grid-cols-[minmax(0,1.6fr)_minmax(24rem,1fr)]">
          <div class="flex min-w-0 items-center gap-3">
            <UButton color="primary" variant="soft" size="sm" to="/">
              Home
            </UButton>
            <UBadge color="primary" variant="soft">Matrix Viewer</UBadge>
            <div class="min-w-0">
              <p class="text-[11px] font-medium uppercase tracking-[0.22em] text-slate-500">Shared Nuxt Workspace</p>
              <h1 class="truncate text-lg font-semibold text-slate-950">
                {{ mangaName || 'Loading MatrixEase' }}
              </h1>
            </div>
          </div>

          <div v-if="payload" class="grid grid-cols-4 gap-px overflow-hidden rounded-xl border border-slate-200 bg-slate-200 text-sm">
            <div class="bg-slate-50 px-3 py-2">
              <p class="text-[10px] uppercase tracking-[0.18em] text-slate-500">Rows</p>
              <p class="font-semibold text-slate-950">{{ payload.TotalRows.toLocaleString() }}</p>
            </div>
            <div class="bg-slate-50 px-3 py-2">
              <p class="text-[10px] uppercase tracking-[0.18em] text-slate-500">Selected</p>
              <p class="font-semibold text-slate-950">{{ payload.SelectedRows.toLocaleString() }}</p>
            </div>
            <div class="bg-slate-50 px-3 py-2">
              <p class="text-[10px] uppercase tracking-[0.18em] text-slate-500">Visible</p>
              <p class="font-semibold text-slate-950">{{ visibleColumns.length }}</p>
            </div>
            <div class="bg-slate-50 px-3 py-2">
              <p class="text-[10px] uppercase tracking-[0.18em] text-slate-500">Mode</p>
              <p class="truncate font-semibold text-slate-950">{{ payload.SelectOperation }}</p>
            </div>
          </div>
        </div>
      </header>

      <div class="space-y-2 border-b border-slate-200 bg-slate-100 px-3 py-2">
        <UAlert v-if="errorMessage" color="error" variant="soft" :description="errorMessage" />
        <UAlert v-if="successMessage" color="success" variant="soft" :description="successMessage" />
        <UAlert v-if="busyMessage" color="info" variant="soft" :description="busyMessage" />
      </div>

      <div v-if="loading" class="grid gap-3 overflow-auto px-3 py-3 md:grid-cols-2 xl:grid-cols-3">
        <USkeleton v-for="index in 6" :key="index" class="h-40 w-full" />
      </div>

      <section v-else class="grid min-h-0 grid-cols-[22rem_minmax(0,1fr)]">
        <aside class="grid min-h-0 grid-rows-[auto_minmax(0,1fr)] border-r border-slate-200 bg-white">
          <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
            <div>
              <h2 class="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-500">Inspector</h2>
              <p class="text-xs text-slate-500">{{ selectionSummary }}</p>
            </div>
            <UButton color="neutral" variant="outline" size="xs" @click="showingSettings = !showingSettings">
              {{ showingSettings ? 'Hide Settings' : 'Settings' }}
            </UButton>
          </div>

          <div class="space-y-3 overflow-y-auto px-3 py-3">
            <section class="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <div class="grid gap-2">
                <div class="grid grid-cols-2 gap-2 text-xs">
                  <div class="rounded-lg border border-slate-200 bg-white px-2 py-2">
                    <p class="uppercase tracking-[0.16em] text-slate-500">Focus</p>
                    <p class="mt-1 font-medium text-slate-900">{{ selectedNode?.ColumnValue || '(none)' }}</p>
                  </div>
                  <div class="rounded-lg border border-slate-200 bg-white px-2 py-2">
                    <p class="uppercase tracking-[0.16em] text-slate-500">Column</p>
                    <p class="mt-1 truncate font-medium text-slate-900">{{ selectedColumn?.name || '(none)' }}</p>
                  </div>
                </div>

                <UFormField label="Selection expression">
                  <UTextarea v-model="pendingExpression" :rows="4" autoresize />
                </UFormField>

                <div class="grid grid-cols-2 gap-2">
                  <UButton color="primary" size="sm" @click="appendSelection('overwrite_selection')">
                    Set
                  </UButton>
                  <UButton color="neutral" variant="outline" size="sm" @click="clearExpression">
                    Clear
                  </UButton>
                  <UButton color="neutral" variant="outline" size="sm" @click="appendSelection('and_selections')">
                    And
                  </UButton>
                  <UButton color="neutral" variant="outline" size="sm" @click="appendSelection('or_selection')">
                    Or
                  </UButton>
                </div>

                <div class="grid grid-cols-2 gap-2">
                  <UButton color="primary" size="sm" :loading="applyingFilter" @click="applyCurrentFilter">
                    Apply Filter
                  </UButton>
                  <UButton color="neutral" variant="outline" size="sm" @click="loadViewer">
                    Reload
                  </UButton>
                </div>
              </div>
            </section>

            <section v-if="selectedColumn" class="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Column Tools</p>
                  <p class="font-medium text-slate-900">{{ selectedColumn.name }}</p>
                  <p class="text-xs text-slate-500">{{ selectedColumn.ColType }} • {{ selectedColumn.DataType }}</p>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-2">
                <UButton color="neutral" variant="outline" size="sm" :loading="runningColumnAction" @click="loadColumnStats">
                  Stats
                </UButton>
                <UButton color="neutral" variant="outline" size="sm" @click="openColumnReport">
                  Report
                </UButton>
              </div>

              <div class="grid gap-2">
                <UCheckbox v-model="bucketForm.bucketized" label="Bucketized" />
                <UFormField label="Bucket size">
                  <UInput v-model.number="bucketForm.bucketSize" type="number" />
                </UFormField>
                <UFormField label="Bucket modifier">
                  <UInput v-model.number="bucketForm.bucketMod" type="number" step="any" />
                </UFormField>
              </div>

              <UButton color="primary" size="sm" :loading="runningColumnAction" @click="applyBucketSettings">
                Apply Bucket Settings
              </UButton>
            </section>

            <section v-if="selectedNode" class="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <div>
                <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Value Tools</p>
                <p class="font-medium text-slate-900">{{ selectedNode.ColumnValue || '(empty)' }}</p>
                <p class="text-xs text-slate-500">{{ percentageLabel(selectedNode) }}</p>
              </div>

              <div class="grid grid-cols-2 gap-2">
                <UButton color="neutral" variant="outline" size="sm" :loading="runningColumnAction" @click="loadNodeRows">
                  Rows
                </UButton>
                <UButton color="neutral" variant="outline" size="sm" :loading="runningColumnAction" @click="loadDuplicateEntries">
                  Duplicates
                </UButton>
                <UButton color="neutral" variant="outline" size="sm" :loading="runningColumnAction" @click="loadDependencyDiagram">
                  Dependency
                </UButton>
              </div>
            </section>

            <section v-if="selectedNode && measureColumns.length > 0" class="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <div>
                <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Measures</p>
                <p class="text-xs text-slate-500">Compute aggregates for the selected node.</p>
              </div>

              <UCheckbox v-model="measuresPanel.filtered" label="Use filtered results" />

              <div class="grid gap-2">
                <UCheckbox
                  v-for="column in measureColumns"
                  :key="column.index"
                  :model-value="measuresPanel.selectedIndexes.includes(column.index)"
                  :label="column.name"
                  @update:model-value="() => toggleIndexSelection(measuresPanel.selectedIndexes, column.index)"
                />
              </div>

              <UButton color="primary" size="sm" :loading="runningColumnAction" @click="loadMeasures">
                Load Measures
              </UButton>
            </section>

            <section class="space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <div>
                <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Quick Charts</p>
                <p class="text-xs text-slate-500">Run report or chart queries without leaving the viewer.</p>
              </div>

              <div class="grid gap-2">
                <UFormField label="Dimension">
                  <USelect
                    v-model="chartPanel.dimensionIndex"
                    :items="dimensionColumns.map(column => ({ label: column.name, value: column.index }))"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
                <UFormField label="Mode">
                  <USelect
                    v-model="chartPanel.chartType"
                    :items="[
                      { label: 'Report', value: 'report' },
                      { label: 'Bar', value: 'bar' },
                      { label: 'Line', value: 'line' },
                      { label: 'Radar', value: 'radar' },
                      { label: 'Polar Area', value: 'polarArea' }
                    ]"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
              </div>

              <UCheckbox v-model="chartPanel.filtered" label="Use filtered results" />

              <div class="grid gap-3">
                <div class="space-y-1">
                  <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Totals</p>
                  <UCheckbox
                    v-for="column in measureColumns"
                    :key="`tot-${column.index}`"
                    :model-value="chartPanel.totalIndexes.includes(column.index)"
                    :label="column.name"
                    @update:model-value="() => toggleIndexSelection(chartPanel.totalIndexes, column.index)"
                  />
                </div>
                <div class="space-y-1">
                  <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Averages</p>
                  <UCheckbox
                    v-for="column in measureColumns"
                    :key="`avg-${column.index}`"
                    :model-value="chartPanel.averageIndexes.includes(column.index)"
                    :label="column.name"
                    @update:model-value="() => toggleIndexSelection(chartPanel.averageIndexes, column.index)"
                  />
                </div>
                <div class="space-y-1">
                  <p class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Counts</p>
                  <UCheckbox
                    v-for="column in dimensionColumns"
                    :key="`cnt-${column.index}`"
                    :model-value="chartPanel.countIndexes.includes(column.index)"
                    :label="column.name"
                    @update:model-value="() => toggleIndexSelection(chartPanel.countIndexes, column.index)"
                  />
                </div>
              </div>

              <UButton color="primary" size="sm" :loading="runningColumnAction" @click="loadQuickChartOrReport">
                Load Chart / Report
              </UButton>
            </section>

            <section v-if="showingSettings" class="space-y-4 rounded-xl border border-slate-200 bg-slate-50 p-3">
              <h2 class="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-500">Viewer Settings</h2>

              <div class="grid gap-3">
                <UFormField label="Low bound">
                  <UInput v-model.number="settings.showLowBound" type="number" />
                </UFormField>
                <UFormField label="High bound">
                  <UInput v-model.number="settings.showHighBound" type="number" />
                </UFormField>
                <UFormField label="Percent mode">
                  <USelect
                    v-model="settings.showPercentage"
                    :items="[
                      { label: 'Selected total %', value: 'pct_tot_sel' },
                      { label: 'Selected only %', value: 'pct_of_sel' }
                    ]"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
                <UFormField label="Selection mode">
                  <USelect
                    v-model="settings.selectOperation"
                    :items="[
                      { label: 'Overwrite', value: 'overwrite_selection' },
                      { label: 'And', value: 'and_selections' },
                      { label: 'Or', value: 'or_selection' }
                    ]"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
              </div>

              <div class="grid gap-2">
                <UCheckbox v-model="settings.showLowEqual" label="Include low bound" />
                <UCheckbox v-model="settings.showHighEqual" label="Include high bound" />
                <UCheckbox v-model="settings.colAscending" label="Sort ascending by count" />
              </div>

              <div class="space-y-2">
                <h3 class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">Visible columns</h3>
                <div class="grid gap-2">
                  <UCheckbox
                    v-for="column in columnEntries"
                    :key="column.name"
                    :model-value="settings.hideColumns[column.Index] !== true"
                    :label="column.name"
                    @update:model-value="value => settings.hideColumns[column.Index] = !value"
                  />
                </div>
              </div>

              <UButton color="primary" size="sm" :loading="refreshingSettings" @click="saveSettings">
                Save Settings
              </UButton>
            </section>
          </div>
        </aside>

        <section class="grid min-h-0 grid-rows-[minmax(0,1fr)_auto] bg-slate-100">
          <div class="grid min-h-0 grid-rows-[auto_minmax(0,1fr)]">
            <div class="flex items-center justify-between border-b border-slate-200 bg-white px-3 py-2">
              <div>
                <h2 class="text-[11px] font-semibold uppercase tracking-[0.22em] text-slate-500">Matrix Surface</h2>
                <p class="text-xs text-slate-500">SVG workspace tuned for larger datasets and lower DOM overhead.</p>
              </div>
              <div class="grid grid-cols-2 gap-px overflow-hidden rounded-lg border border-slate-200 bg-slate-200 text-[11px]">
                <div class="bg-slate-50 px-2 py-1 text-slate-600">{{ visibleColumns.length }} cols</div>
                <div class="bg-slate-50 px-2 py-1 text-slate-600">{{ maxRenderedRows }} rows/col</div>
              </div>
            </div>

            <div class="min-h-0 overflow-auto bg-slate-200 p-3">
              <div class="min-h-full min-w-full overflow-x-auto rounded-xl border border-slate-300 bg-white">
                <svg
                  class="block"
                  :width="svgWidth"
                  :height="svgHeight"
                  role="img"
                  aria-label="MatrixEase SVG viewer"
                >
                  <g
                    v-for="column in svgColumns"
                    :key="column.name"
                    :transform="`translate(${svgXForColumn(column.visualIndex)},0)`"
                  >
                    <rect x="0" y="0" :width="svgConfig.columnWidth" :height="svgConfig.headerHeight" rx="10" fill="#f8fafc" stroke="#cbd5e1" />
                    <text x="14" y="18" fill="#0f172a" font-size="14" font-weight="700">
                      {{ trimSvgText(column.name, 24) }}
                    </text>
                    <text x="14" y="33" fill="#64748b" font-size="10">
                      {{ trimSvgText(`${column.ColType} • ${column.DataType} • ${column.DistinctValues} distinct`, 34) }}
                    </text>

                    <g
                      v-for="(value, valueIndex) in column.renderValues"
                      :key="`${column.name}-${value.ColumnValue}`"
                      class="cursor-pointer"
                      @click="selectValue(column.name, value)"
                    >
                      <rect
                        x="0"
                        :y="svgYForValue(valueIndex)"
                        :width="svgConfig.columnWidth"
                        :height="svgConfig.nodeHeight"
                        rx="10"
                        :fill="selectedColumnName === column.name && selectedValueKey === value.ColumnValue ? '#ccfbf1' : '#ffffff'"
                        :stroke="selectedColumnName === column.name && selectedValueKey === value.ColumnValue ? '#14b8a6' : '#cbd5e1'"
                        stroke-width="1.5"
                      />
                      <rect x="10" :y="svgYForValue(valueIndex) + 38" :width="svgTotalBarWidth(value)" height="9" rx="4" fill="#bbf7d0" />
                      <rect x="10" :y="svgYForValue(valueIndex) + 38" :width="svgBarWidth(value)" height="9" rx="4" fill="#14b8a6" />
                      <text x="14" :y="svgYForValue(valueIndex) + 18" fill="#0f172a" font-size="12" font-weight="600">
                        {{ trimSvgText(value.ColumnValue || '(empty)', 28) }}
                      </text>
                      <text x="14" :y="svgYForValue(valueIndex) + 31" fill="#64748b" font-size="10">
                        {{ trimSvgText(percentageLabel(value), 32) }}
                      </text>
                      <text x="14" :y="svgYForValue(valueIndex) + 58" fill="#64748b" font-size="10">
                        {{ trimSvgText(`Total ${value.TotalPct.toFixed(2)}% • Rows ${value.SelectedValues}`, 34) }}
                      </text>
                    </g>
                  </g>
                </svg>
              </div>
            </div>
          </div>

          <div class="max-h-[38vh] overflow-auto border-t border-slate-200 bg-white">
            <div class="grid gap-3 p-3 xl:grid-cols-2">
              <div v-if="columnStats" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Column Statistics</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="columnStats = null">Close</UButton>
                </div>
                <pre class="overflow-x-auto bg-slate-950 p-3 text-xs text-teal-200">{{ JSON.stringify(columnStats, null, 2) }}</pre>
              </div>

              <div v-if="dependencyDiagram" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Dependency Data</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="dependencyDiagram = null">Close</UButton>
                </div>
                <pre class="overflow-x-auto bg-slate-950 p-3 text-xs text-teal-200">{{ JSON.stringify(dependencyDiagram, null, 2) }}</pre>
              </div>

              <div v-if="measureStats" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50 xl:col-span-2">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Measure Statistics</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="measureStats = null">Close</UButton>
                </div>
                <div class="overflow-x-auto">
                  <table class="min-w-full divide-y divide-slate-200 text-sm">
                    <thead class="bg-white text-left text-slate-500">
                      <tr>
                        <th class="px-3 py-2 font-medium">Measure</th>
                        <th class="px-3 py-2 font-medium">Count</th>
                        <th class="px-3 py-2 font-medium">Total</th>
                        <th class="px-3 py-2 font-medium">Average</th>
                        <th class="px-3 py-2 font-medium">Min</th>
                        <th class="px-3 py-2 font-medium">Max</th>
                        <th class="px-3 py-2 font-medium">Range</th>
                        <th class="px-3 py-2 font-medium">Std Dev</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr v-for="(stats, name) in measureStats" :key="name" class="bg-white/70">
                        <td class="px-3 py-2 font-medium text-slate-900">{{ name }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Count }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Total }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Average }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Min }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Max }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.Range }}</td>
                        <td class="px-3 py-2 text-slate-700">{{ stats.StandardDeviation }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div v-if="rowReport" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50 xl:col-span-2">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">{{ rowReport.title }}</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="rowReport = null">Close</UButton>
                </div>
                <div class="overflow-x-auto">
                  <table class="min-w-full divide-y divide-slate-200 text-sm">
                    <thead class="bg-white text-left text-slate-500">
                      <tr>
                        <th v-for="column in rowReport.columns" :key="column" class="px-3 py-2 font-medium">{{ column }}</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr v-for="(row, rowIndex) in rowReport.data" :key="rowIndex" class="bg-white/70">
                        <td v-for="(value, valueIndex) in row" :key="valueIndex" class="px-3 py-2 text-slate-700">{{ value }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div v-if="quickReport" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50 xl:col-span-2">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Quick Report</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="quickReport = null">Close</UButton>
                </div>
                <div class="overflow-x-auto">
                  <table class="min-w-full divide-y divide-slate-200 text-sm">
                    <thead class="bg-white text-left text-slate-500">
                      <tr>
                        <th v-for="column in quickReport.columns" :key="column" class="px-3 py-2 font-medium">{{ column }}</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      <tr v-for="(row, rowIndex) in quickReport.data" :key="rowIndex" class="bg-white/70">
                        <td v-for="(value, valueIndex) in row" :key="valueIndex" class="px-3 py-2 text-slate-700">{{ value }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div v-if="duplicateEntries" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Duplicate Entries</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="duplicateEntries = null">Close</UButton>
                </div>
                <ul class="grid gap-2 p-3 md:grid-cols-2">
                  <li
                    v-for="entry in duplicateEntries"
                    :key="entry"
                    class="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700"
                  >
                    {{ entry }}
                  </li>
                </ul>
              </div>

              <div v-if="quickChartData" class="overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
                <div class="flex items-center justify-between border-b border-slate-200 px-3 py-2">
                  <h2 class="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">Quick Chart Data</h2>
                  <UButton color="neutral" variant="ghost" size="xs" @click="quickChartData = null">Close</UButton>
                </div>
                <div class="space-y-5 p-3">
                  <div
                    v-for="dataset in quickChartData.datasets"
                    :key="dataset.label"
                    class="space-y-2"
                  >
                    <h3 class="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500">{{ dataset.label }}</h3>
                    <div class="space-y-2">
                      <div
                        v-for="(label, index) in quickChartData.labels"
                        :key="`${dataset.label}-${label}`"
                        class="grid grid-cols-[minmax(8rem,14rem)_1fr_auto] items-center gap-2"
                      >
                        <span class="truncate text-sm text-slate-700">{{ label }}</span>
                        <div class="h-3 overflow-hidden rounded-full bg-slate-200">
                          <div
                            class="h-full rounded-full bg-teal-500"
                            :style="{ width: `${Math.min(100, Math.max(2, Number(dataset.data[index] || 0)))}%` }"
                          />
                        </div>
                        <span class="text-xs font-medium text-slate-500">{{ dataset.data[index] }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>
      </section>
    </div>
  </main>
</template>
