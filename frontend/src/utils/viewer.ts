import type { MatrixEaseColumn, MatrixEaseColumnValue, MatrixEasePayload } from '@/services/api'

export type ViewerColumnEntry = MatrixEaseColumn & {
  name: string
}

export type ViewerSettings = {
  showLowEqual: boolean
  showLowBound: number
  showHighEqual: boolean
  showHighBound: number
  showPercentage: string
  selectOperation: string
  colAscending: boolean
  hideColumns: boolean[]
}

export type ViewerBucketForm = {
  bucketized: boolean
  bucketSize: number
  bucketMod: number
}

export function createViewerSettings(): ViewerSettings
{
  return {
    showLowEqual: true,
    showLowBound: 0,
    showHighEqual: true,
    showHighBound: 100,
    showPercentage: 'pct_tot_sel',
    selectOperation: 'overwrite_selection',
    colAscending: false,
    hideColumns: []
  }
}

export function createViewerBucketForm(): ViewerBucketForm
{
  return {
    bucketized: false,
    bucketSize: 0,
    bucketMod: 0
  }
}

export function buildColumnEntries(payload: MatrixEasePayload | null): ViewerColumnEntry[]
{
  if (!payload) {
    return []
  }

  return Object.entries(payload.Columns)
    .map(([name, column]) => ({ name, ...column }))
    .sort((left, right) => left.Index - right.Index)
}

export function buildSelectionToken(column: ViewerColumnEntry | null, value: MatrixEaseColumnValue | null)
{
  if (!column || !value) {
    return ''
  }

  return `${value.ColumnValue}@${column.name}:${column.Index}`
}

export function buildSelectionExpression(column: ViewerColumnEntry | null, value: MatrixEaseColumnValue | null)
{
  const token = buildSelectionToken(column, value)
  return token ? `"${token}"` : ''
}

export function appendSelectionExpression(current: string, expression: string, operation: string)
{
  if (!expression) {
    return current
  }

  if (current && (operation === 'or_selection' || operation === 'and_selections')) {
    return `${current}${operation === 'or_selection' ? ' OR ' : ' AND '}${expression}`
  }

  return expression
}

export function showViewerValue(settings: ViewerSettings, selectRelPct: number)
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

export function renderedValuesForColumn(column: MatrixEaseColumn, settings: ViewerSettings, limit = 24)
{
  return column.Values
    .filter(value => showViewerValue(settings, value.SelectRelPct))
    .slice(0, limit)
}

export function percentageLabel(value: MatrixEaseColumnValue, showPercentage: string)
{
  if (showPercentage === 'pct_of_sel') {
    return `Selected ${value.SelectRelPct.toFixed(2)}%`
  }

  return `Selected total ${value.SelectAllPct.toFixed(2)}%`
}

export function toggleIndexSelection(list: number[], index: number)
{
  const existingIndex = list.indexOf(index)

  if (existingIndex >= 0) {
    list.splice(existingIndex, 1)
    return
  }

  list.push(index)
}

export function syncBucketForm(column: ViewerColumnEntry | null, bucketForm: ViewerBucketForm)
{
  if (!column) {
    bucketForm.bucketized = false
    bucketForm.bucketSize = 0
    bucketForm.bucketMod = 0
    return
  }

  bucketForm.bucketized = column.Bucketized
  bucketForm.bucketSize = column.CurBucketSize
  bucketForm.bucketMod = column.CurBucketMod
}
