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

const topValues = (column: MatrixEaseColumn) =>
{
  return column.Values
    .filter(value => showValue(value.SelectRelPct))
    .slice(0, 18)
}

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

const matrixColumnStyle = computed(() => ({
  gridTemplateColumns: `repeat(${Math.max(visibleColumns.value.length, 1)}, minmax(18rem, 1fr))`
}))

const selectionSummary = computed(() =>
{
  if (!selectedColumn.value || !selectedNode.value) {
    return 'Select a value to inspect it and build filters.'
  }

  return `${selectedNode.value.ColumnValue || '(empty)'} @ ${selectedColumn.value.name}`
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

const barWidth = (value: MatrixEaseColumnValue) =>
{
  const metric = settings.showPercentage === 'pct_of_sel' ? value.SelectRelPct : value.SelectAllPct
  return `${Math.max(4, Math.min(metric, 100))}%`
}

const percentageLabel = (value: MatrixEaseColumnValue) =>
{
  if (settings.showPercentage === 'pct_of_sel') {
    return `Selected ${value.SelectRelPct.toFixed(2)}%`
  }

  return `Selected total ${value.SelectAllPct.toFixed(2)}%`
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
    const operator = operation === 'or_selection' ? ' OR ' : ' AND '
    pendingExpression.value = `${pendingExpression.value}${operator}${expression}`
  } else {
    pendingExpression.value = expression
  }
}

const clearExpression = () =>
{
  pendingExpression.value = ''
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

    for (let attempt = 0; attempt < 120; attempt += 1) {
      const pickup = await api.getPickupStatus(session.matrixEaseId.value, mxesId, result.PickupKey)
      if (!pickup.Success) {
        throw new Error(pickup.Message ?? 'Failed while waiting for filtered results.')
      }

      if (pickup.Complete && pickup.Results) {
        payload.value = pickup.Results
        syncSettingsFromPayload()
        successMessage.value = 'Matrix filter updated.'
        busyMessage.value = ''
        return
      }

      await new Promise(resolve => setTimeout(resolve, 1000))
    }

    throw new Error('Timed out waiting for filter results.')
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

onMounted(async () =>
{
  await loadViewer()
})
</script>

<template>
  <main class="min-h-screen text-slate-900">
    <div class="mx-auto flex min-h-screen max-w-7xl flex-col gap-8 px-4 py-6 sm:px-6 lg:px-8">
      <section class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/80 shadow-2xl shadow-slate-300/40 backdrop-blur">
        <div class="grid gap-6 p-8 lg:grid-cols-[1.2fr_0.8fr] lg:p-10">
          <div class="space-y-4">
            <UBadge color="primary" variant="soft" size="lg">
              Matrix Viewer
            </UBadge>
            <h1 class="text-4xl font-semibold tracking-tight text-slate-950 md:text-5xl">
              {{ mangaName || 'Loading MatrixEase' }}
            </h1>
            <p class="max-w-2xl text-base leading-7 text-slate-600">
              This is the first Nuxt-based viewer route. It loads the saved matrix payload and renders a readable column/value summary while the legacy SVG-heavy interactions are still being migrated.
            </p>
            <div class="flex flex-wrap gap-3">
              <UButton color="primary" to="/">
                Back Home
              </UButton>
            </div>
          </div>

          <UCard class="border-slate-200/80 bg-slate-950 text-white">
            <template #header>
              <h2 class="text-sm font-medium uppercase tracking-[0.3em] text-teal-200">Summary</h2>
            </template>
            <div v-if="payload" class="grid gap-4 text-sm">
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <p class="text-slate-400">Total rows</p>
                  <p class="text-2xl font-semibold text-white">{{ payload.TotalRows.toLocaleString() }}</p>
                </div>
                <div>
                  <p class="text-slate-400">Selected rows</p>
                  <p class="text-2xl font-semibold text-white">{{ payload.SelectedRows.toLocaleString() }}</p>
                </div>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <p class="text-slate-400">Columns</p>
                  <p class="text-xl font-semibold text-white">{{ visibleColumns.length }}</p>
                </div>
                <div>
                  <p class="text-slate-400">Selection mode</p>
                  <p class="font-medium text-white">{{ payload.SelectOperation }}</p>
                </div>
              </div>
              <div>
                <p class="text-slate-400">Current expression</p>
                <p class="font-medium text-white">{{ payload.SelectionExpression || 'None' }}</p>
              </div>
            </div>
          </UCard>
        </div>
      </section>

      <UAlert
        v-if="errorMessage"
        color="error"
        variant="soft"
        :description="errorMessage"
      />
      <UAlert
        v-if="successMessage"
        color="success"
        variant="soft"
        :description="successMessage"
      />
      <UAlert
        v-if="busyMessage"
        color="info"
        variant="soft"
        :description="busyMessage"
      />

      <div v-if="loading" class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <USkeleton v-for="index in 6" :key="index" class="h-44 w-full" />
      </div>

      <section v-else class="grid gap-6 xl:grid-cols-[0.95fr_1.55fr]">
        <div class="space-y-6">
          <UCard class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur">
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">Selection</h2>
                <UButton color="neutral" variant="outline" @click="showingSettings = !showingSettings">
                  {{ showingSettings ? 'Close Settings' : 'Viewer Settings' }}
                </UButton>
              </div>
            </template>

            <div class="space-y-4">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-sm text-slate-500">Current focus</p>
                <p class="mt-1 font-medium text-slate-900">{{ selectionSummary }}</p>
              </div>

              <UFormField label="Pending expression">
                <UTextarea v-model="pendingExpression" :rows="4" autoresize />
              </UFormField>

              <div class="flex flex-wrap gap-3">
                <UButton color="primary" @click="appendSelection('overwrite_selection')">
                  Set Selection
                </UButton>
                <UButton color="neutral" variant="outline" @click="appendSelection('and_selections')">
                  And
                </UButton>
                <UButton color="neutral" variant="outline" @click="appendSelection('or_selection')">
                  Or
                </UButton>
                <UButton color="neutral" variant="ghost" @click="clearExpression">
                  Clear
                </UButton>
              </div>

              <div class="flex flex-wrap gap-3">
                <UButton color="primary" :loading="applyingFilter" @click="applyCurrentFilter">
                  Apply Filter
                </UButton>
                <UButton color="neutral" variant="outline" @click="loadViewer">
                  Reload Matrix
                </UButton>
              </div>

              <div v-if="selectedColumn" class="space-y-3 rounded-2xl bg-slate-50 p-4">
                <div>
                  <p class="text-sm text-slate-500">Selected column</p>
                  <p class="font-medium text-slate-900">{{ selectedColumn.name }} • {{ selectedColumn.DataType }}</p>
                </div>

                <div class="flex flex-wrap gap-3">
                  <UButton color="neutral" variant="outline" :loading="runningColumnAction" @click="loadColumnStats">
                    Column Stats
                  </UButton>
                  <UButton color="neutral" variant="outline" @click="openColumnReport">
                    Column Report
                  </UButton>
                </div>

                <div class="grid gap-3 md:grid-cols-2">
                  <UCheckbox v-model="bucketForm.bucketized" label="Bucketized" />
                  <UFormField label="Bucket type">
                    <UInput v-model.number="bucketForm.bucketSize" type="number" />
                  </UFormField>
                  <UFormField label="Bucket modifier">
                    <UInput v-model.number="bucketForm.bucketMod" type="number" step="any" />
                  </UFormField>
                </div>

                <UButton color="primary" :loading="runningColumnAction" @click="applyBucketSettings">
                  Apply Bucket Settings
                </UButton>
              </div>

              <div v-if="selectedNode" class="space-y-3 rounded-2xl bg-slate-50 p-4">
                <div>
                  <p class="text-sm text-slate-500">Selected value</p>
                  <p class="font-medium text-slate-900">{{ selectedNode.ColumnValue || '(empty)' }}</p>
                </div>

                <div class="flex flex-wrap gap-3">
                  <UButton color="neutral" variant="outline" :loading="runningColumnAction" @click="loadNodeRows">
                    Node Rows
                  </UButton>
                  <UButton color="neutral" variant="outline" :loading="runningColumnAction" @click="loadDuplicateEntries">
                    Duplicate Entries
                  </UButton>
                  <UButton color="neutral" variant="outline" :loading="runningColumnAction" @click="loadDependencyDiagram">
                    Dependency Data
                  </UButton>
                </div>
              </div>
            </div>
          </UCard>

          <UCard
            v-if="showingSettings"
            class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
          >
            <template #header>
              <h2 class="text-lg font-semibold text-slate-950">Viewer Settings</h2>
            </template>

            <div class="space-y-5">
              <div class="grid gap-4 md:grid-cols-2">
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

              <div class="grid gap-3 md:grid-cols-2">
                <UCheckbox v-model="settings.showLowEqual" label="Include low bound" />
                <UCheckbox v-model="settings.showHighEqual" label="Include high bound" />
                <UCheckbox v-model="settings.colAscending" label="Sort columns ascending by value count" />
              </div>

              <div class="space-y-3">
                <h3 class="text-sm font-medium text-slate-700">Visible columns</h3>
                <div class="grid gap-2 md:grid-cols-2">
                  <UCheckbox
                    v-for="column in columnEntries"
                    :key="column.name"
                    :model-value="settings.hideColumns[column.Index] !== true"
                    :label="column.name"
                    @update:model-value="value => settings.hideColumns[column.Index] = !value"
                  />
                </div>
              </div>

              <UButton color="primary" :loading="refreshingSettings" @click="saveSettings">
                Save Settings
              </UButton>
            </div>
          </UCard>
        </div>

        <section class="space-y-4">
          <UCard
            v-if="columnStats"
            class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
          >
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">Column Statistics</h2>
                <UButton color="neutral" variant="ghost" @click="columnStats = null">Close</UButton>
              </div>
            </template>
            <pre class="overflow-x-auto rounded-xl bg-slate-950 p-4 text-xs text-teal-200">{{ JSON.stringify(columnStats, null, 2) }}</pre>
          </UCard>

          <UCard
            v-if="rowReport"
            class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
          >
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">{{ rowReport.title }}</h2>
                <UButton color="neutral" variant="ghost" @click="rowReport = null">Close</UButton>
              </div>
            </template>
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-slate-200 text-sm">
                <thead class="bg-slate-50 text-left text-slate-500">
                  <tr>
                    <th v-for="column in rowReport.columns" :key="column" class="px-4 py-3 font-medium">{{ column }}</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  <tr v-for="(row, rowIndex) in rowReport.data" :key="rowIndex" class="bg-white/70">
                    <td v-for="(value, valueIndex) in row" :key="valueIndex" class="px-4 py-3 text-slate-700">{{ value }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </UCard>

          <UCard
            v-if="duplicateEntries"
            class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
          >
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">Duplicate Entries</h2>
                <UButton color="neutral" variant="ghost" @click="duplicateEntries = null">Close</UButton>
              </div>
            </template>
            <ul class="grid gap-2 md:grid-cols-2">
              <li
                v-for="entry in duplicateEntries"
                :key="entry"
                class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700"
              >
                {{ entry }}
              </li>
            </ul>
          </UCard>

          <UCard
            v-if="dependencyDiagram"
            class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
          >
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">Dependency Data</h2>
                <UButton color="neutral" variant="ghost" @click="dependencyDiagram = null">Close</UButton>
              </div>
            </template>
            <pre class="overflow-x-auto rounded-xl bg-slate-950 p-4 text-xs text-teal-200">{{ JSON.stringify(dependencyDiagram, null, 2) }}</pre>
          </UCard>

          <div class="overflow-x-auto rounded-[2rem] border border-white/70 bg-white/60 p-4 shadow-xl shadow-slate-200/50 backdrop-blur">
            <div class="grid min-w-max gap-4" :style="matrixColumnStyle">
              <UCard
                v-for="column in visibleColumns"
                :key="column.name"
                class="h-full border-slate-200/80 bg-white/80 shadow-md"
              >
                <template #header>
                  <div class="space-y-1">
                    <div class="flex items-center justify-between gap-3">
                      <h2 class="text-base font-semibold text-slate-950">{{ column.name }}</h2>
                      <UBadge color="neutral" variant="subtle">{{ column.DataType }}</UBadge>
                    </div>
                    <p class="text-xs text-slate-500">
                      {{ column.ColType }} • {{ column.DistinctValues }} distinct
                    </p>
                  </div>
                </template>

                <div class="space-y-3">
                  <button
                    v-for="value in topValues(column)"
                    :key="`${column.name}-${value.ColumnValue}`"
                    type="button"
                    class="block w-full rounded-2xl border p-3 text-left transition"
                    :class="selectedColumnName === column.name && selectedValueKey === value.ColumnValue
                      ? 'border-teal-500 bg-teal-50 shadow'
                      : 'border-slate-200 bg-slate-50 hover:border-teal-300 hover:bg-white'"
                    @click="selectValue(column.name, value)"
                  >
                    <div class="mb-2 flex items-start justify-between gap-3">
                      <p class="min-w-0 flex-1 break-words font-medium text-slate-900">{{ value.ColumnValue || '(empty)' }}</p>
                      <UBadge color="primary" variant="soft">{{ value.SelectedValues }}</UBadge>
                    </div>
                    <div class="h-2 overflow-hidden rounded-full bg-slate-200">
                      <div class="h-full rounded-full bg-teal-500" :style="{ width: barWidth(value) }" />
                    </div>
                    <div class="mt-2 flex items-center justify-between gap-3 text-xs text-slate-500">
                      <span>{{ percentageLabel(value) }}</span>
                      <span>Total {{ value.TotalPct.toFixed(2) }}%</span>
                    </div>
                  </button>
                </div>
              </UCard>
            </div>
          </div>
        </section>
      </section>

      <section v-if="false" class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <UCard
          v-for="column in visibleColumns"
          :key="column.name"
          class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
        >
          <template #header>
            <div class="space-y-1">
              <div class="flex items-center justify-between gap-3">
                <h2 class="text-lg font-semibold text-slate-950">{{ column.name }}</h2>
                <UBadge color="neutral" variant="subtle">{{ column.DataType }}</UBadge>
              </div>
              <p class="text-sm text-slate-500">
                {{ column.ColType }} • {{ column.DistinctValues }} distinct • {{ column.NullEmpty }} empty
              </p>
            </div>
          </template>

          <div class="space-y-4">
            <div class="grid grid-cols-2 gap-3 text-sm">
              <div class="rounded-2xl bg-slate-50 p-3">
                <p class="text-slate-500">Selectivity</p>
                <p class="font-semibold text-slate-900">{{ column.Selectivity }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-3">
                <p class="text-slate-500">Bucketized</p>
                <p class="font-semibold text-slate-900">{{ column.Bucketized ? 'Yes' : 'No' }}</p>
              </div>
            </div>

            <div>
              <h3 class="mb-2 text-sm font-medium text-slate-700">Top values</h3>
              <div class="space-y-2">
                <div
                  v-for="value in topValues(column)"
                  :key="`${column.name}-${value.ColumnValue}`"
                  class="rounded-2xl border border-slate-200 bg-slate-50 p-3"
                >
                  <div class="flex items-start justify-between gap-3">
                    <p class="min-w-0 flex-1 break-words font-medium text-slate-900">{{ value.ColumnValue || '(empty)' }}</p>
                    <UBadge color="primary" variant="soft">{{ value.SelectedValues }}</UBadge>
                  </div>
                  <p class="mt-1 text-xs text-slate-500">
                    Total {{ value.TotalValues }} • Selected {{ value.SelectRelPct.toFixed(2) }}% • Overall {{ value.TotalPct.toFixed(2) }}%
                  </p>
                </div>
              </div>
            </div>
          </div>
        </UCard>
      </section>
    </div>
  </main>
</template>
