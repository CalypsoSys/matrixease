import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { createMatrixEaseApi, type MatrixEasePayload } from '@/services/api'
import { useRuntimeStore } from '@/stores/runtime'
import { useSessionStore } from '@/stores/session'
import {
  appendSelectionExpression,
  buildColumnEntries,
  buildSelectionExpression,
  buildSelectionToken,
  createViewerBucketForm,
  createViewerSettings,
  renderedValuesForColumn,
  syncBucketForm,
  toggleIndexSelection,
  type ViewerColumnEntry
} from '@/utils/viewer'

export function useViewer(mxesId: string)
{
  const router = useRouter()
  const runtimeStore = useRuntimeStore()
  const session = useSessionStore()
  const api = createMatrixEaseApi(runtimeStore.runtime)

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
  const columnStats = ref<Record<string, unknown> | null>(null)
  const rowReport = ref<{ title: string, columns: string[], data: Array<Array<string | number | boolean | null>> } | null>(null)
  const duplicateEntries = ref<string[] | null>(null)
  const dependencyDiagram = ref<Record<string, unknown> | null>(null)
  const measureStats = ref<Record<string, Record<string | number, string | number | null>> | null>(null)
  const quickChartData = ref<{ labels: string[], datasets: Array<{ label: string, data: number[] }> } | null>(null)
  const quickReport = ref<{ columns: string[], data: Array<Array<string | number | boolean | null>> } | null>(null)
  const exportingCsv = ref(false)
  const deletingManga = ref(false)

  const settings = reactive(createViewerSettings())
  const bucketForm = reactive(createViewerBucketForm())
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

  const columnEntries = computed(() => buildColumnEntries(payload.value))
  const visibleColumns = computed(() => columnEntries.value.filter(column => payload.value?.HideColumns[column.Index] !== true))
  const selectedColumn = computed(() => visibleColumns.value.find(column => column.name === selectedColumnName.value) ?? null)
  const selectedNode = computed(() => selectedColumn.value?.Values.find(value => value.ColumnValue === selectedValueKey.value) ?? null)
  const selectedNodeToken = computed(() => buildSelectionToken(selectedColumn.value, selectedNode.value ?? null))
  const selectionSummary = computed(() => {
    if (!selectedColumn.value || !selectedNode.value) {
      return 'Select a value to inspect it and build filters.'
    }

    return `${selectedNode.value.ColumnValue || '(empty)'} @ ${selectedColumn.value.name}`
  })
  const measureColumns = computed(() => visibleColumns.value.filter(column => column.ColType === 'Measure').map(column => ({ name: column.name, index: column.Index })))
  const dimensionColumns = computed(() => visibleColumns.value.filter(column => column.ColType !== 'Measure' || column.DistinctValues <= 100).map(column => ({ name: column.name, index: column.Index })))
  const renderedColumns = computed(() =>
    visibleColumns.value.map(column => ({
      ...column,
      renderValues: renderedValuesForColumn(column, settings, 18)
    }))
  )

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
    syncBucketForm(selectedColumn.value, bucketForm)
  }

  const loadViewer = async () =>
  {
    loading.value = true
    errorMessage.value = ''

    try {
      if (!session.matrixEaseId) {
        await session.refreshBootstrap(api)
      }

      if (!session.matrixEaseId) {
        throw new Error('Session bootstrap did not return a matrix identifier.')
      }

      const response = await api.getManga(session.matrixEaseId, mxesId)
      mangaName.value = response.MangaName
      payload.value = response.MangaData
      syncSettingsFromPayload()
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to load viewer payload.'
    } finally {
      loading.value = false
    }
  }

  const selectValue = (columnName: string, columnValue: string) =>
  {
    selectedColumnName.value = columnName
    selectedValueKey.value = columnValue
    syncBucketForm(visibleColumns.value.find(column => column.name === columnName) ?? null, bucketForm)
  }

  const appendSelection = (mode?: string) =>
  {
    const expression = buildSelectionExpression(selectedColumn.value, selectedNode.value ?? null)

    if (!expression) {
      errorMessage.value = 'Select a value before building a filter.'
      return
    }

    pendingExpression.value = appendSelectionExpression(pendingExpression.value, expression, mode ?? settings.selectOperation)
  }

  const refreshMatrixFromPickup = async (pickupKey: string) =>
  {
    if (!session.matrixEaseId) {
      return
    }

    for (let attempt = 0; attempt < 120; attempt += 1) {
      const pickup = await api.getPickupStatus(session.matrixEaseId, mxesId, pickupKey)

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
    if (!session.matrixEaseId) {
      return
    }

    applyingFilter.value = true
    errorMessage.value = ''
    successMessage.value = ''
    busyMessage.value = 'Applying filter and waiting for the matrix to refresh.'

    try {
      const result = await api.applyFilter(session.matrixEaseId, mxesId, pendingExpression.value)

      if (!result.Success || !result.PickupKey) {
        throw new Error(result.Error ?? 'Failed to start filter update.')
      }

      await refreshMatrixFromPickup(result.PickupKey)
      successMessage.value = 'Matrix filter updated.'
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to apply filter.'
    } finally {
      busyMessage.value = ''
      applyingFilter.value = false
    }
  }

  const saveSettings = async () =>
  {
    if (!session.matrixEaseId) {
      return
    }

    refreshingSettings.value = true
    errorMessage.value = ''
    successMessage.value = ''

    try {
      const response = await api.updateSettings(session.matrixEaseId, mxesId, settings)

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

  const applyBucketSettings = async () =>
  {
    if (!selectedColumn.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const result = await api.bucketizeColumn(session.matrixEaseId!, mxesId, {
        columnName: selectedColumn.value!.name,
        columnIndex: selectedColumn.value!.Index,
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

  const loadColumnStats = async () => {
    if (!selectedColumn.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const response = await api.getDetailedColumnStats(session.matrixEaseId, mxesId, selectedColumn.value!.name, selectedColumn.value!.Index)
      if (!response.Success || !response.ColStats) {
        throw new Error('Failed to load column statistics.')
      }

      columnStats.value = response.ColStats
    })
  }

  const loadNodeRows = async () => {
    if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const response = await api.getNodeRows(session.matrixEaseId!, mxesId, selectedColumn.value!.Index, selectedNodeToken.value)
      if (!response.Success || !response.ReportData) {
        throw new Error('Failed to load rows for the selected value.')
      }

      rowReport.value = {
        title: `${selectedColumn.value!.name} rows`,
        columns: response.ReportData.columns,
        data: response.ReportData.data
      }
    })
  }

  const loadDuplicateEntries = async () => {
    if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const response = await api.getDuplicateEntries(session.matrixEaseId!, mxesId, selectedColumn.value!.Index, selectedNodeToken.value)
      if (!response.Success || !response.DuplicateEntries) {
        throw new Error('Failed to load duplicate entries.')
      }

      duplicateEntries.value = response.DuplicateEntries
    })
  }

  const loadDependencyDiagram = async () => {
    if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const response = await api.getDependencyDiagram(session.matrixEaseId!, mxesId, selectedColumn.value!.Index, selectedNodeToken.value)
      if (!response.Success || !response.DependencyDiagram) {
        throw new Error('Failed to load dependency diagram data.')
      }

      dependencyDiagram.value = response.DependencyDiagram
    })
  }

  const loadMeasures = async () => {
    if (!selectedColumn.value || !selectedNodeToken.value || !session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      const response = await api.getColumnMeasures(session.matrixEaseId!, mxesId, selectedColumn.value!.Index, selectedNodeToken.value, measuresPanel.selectedIndexes, measuresPanel.filtered)
      if (!response.Success || !response.MeasureStats) {
        throw new Error('Failed to load measure statistics.')
      }

      measureStats.value = response.MeasureStats as Record<string, Record<string | number, string | number | null>>
    })
  }

  const loadQuickChartOrReport = async () => {
    if (!session.matrixEaseId) {
      return
    }

    await runColumnAction(async () => {
      if (chartPanel.dimensionIndex === null) {
        throw new Error('Choose one dimension before loading a chart or report.')
      }

      if (chartPanel.totalIndexes.length + chartPanel.averageIndexes.length + chartPanel.countIndexes.length === 0) {
        throw new Error('Choose at least one measure or count column.')
      }

      const response = await api.getQuickChartData(session.matrixEaseId!, mxesId, {
        chartType: chartPanel.chartType,
        dimensionIndexes: [chartPanel.dimensionIndex],
        totalMeasureIndexes: [...chartPanel.totalIndexes],
        averageMeasureIndexes: [...chartPanel.averageIndexes],
        countMeasureIndexes: [...chartPanel.countIndexes],
        filtered: chartPanel.filtered
      })

      if (!response.Success) {
        throw new Error('Failed to load quick chart/report data.')
      }

      quickChartData.value = response.ChartData ?? null
      quickReport.value = response.ReportData ?? null
    })
  }

  const exportSelectedAsCsv = async () =>
  {
    if (!session.matrixEaseId) {
      return
    }

    exportingCsv.value = true
    errorMessage.value = ''
    successMessage.value = ''

    try {
      const blob = await api.exportCsv(session.matrixEaseId, mxesId)
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'mxes_manga.csv'
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      successMessage.value = 'CSV export started.'
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to export CSV.'
    } finally {
      exportingCsv.value = false
    }
  }

  const deleteManga = async () =>
  {
    if (!session.matrixEaseId) {
      return
    }

    if (!window.confirm('Deleting a saved MatrixEase is irreversible. Continue?')) {
      return
    }

    deletingManga.value = true
    errorMessage.value = ''
    successMessage.value = ''

    try {
      const response = await api.deleteManga(session.matrixEaseId, mxesId)
      if (!response.Success) {
        throw new Error('The project could not be deleted.')
      }

      await router.push({ name: 'dashboard' })
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Failed to delete project.'
    } finally {
      deletingManga.value = false
    }
  }

  onMounted(() => {
    void loadViewer()
  })

  return {
    loading,
    errorMessage,
    successMessage,
    busyMessage,
    mangaName,
    payload,
    showingSettings,
    settings,
    bucketForm,
    selectedColumnName,
    selectedValueKey,
    pendingExpression,
    applyingFilter,
    refreshingSettings,
    runningColumnAction,
    columnEntries,
    visibleColumns,
    selectedColumn,
    selectedNode,
    selectionSummary,
    measureColumns,
    dimensionColumns,
    renderedColumns,
    columnStats,
    rowReport,
    duplicateEntries,
    dependencyDiagram,
    measureStats,
    quickChartData,
    quickReport,
    exportingCsv,
    deletingManga,
    measuresPanel,
    chartPanel,
    selectValue,
    appendSelection,
    applyCurrentFilter,
    saveSettings,
    loadColumnStats,
    openColumnReport,
    applyBucketSettings,
    loadNodeRows,
    loadDuplicateEntries,
    loadDependencyDiagram,
    loadMeasures,
    loadQuickChartOrReport,
    exportSelectedAsCsv,
    deleteManga,
    toggleIndexSelection,
    loadViewer
  }
}
