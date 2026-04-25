import type { MatrixEasePayload } from '@/services/api'

export type UploadFormState = {
  mangaName: string
  headerRow: number
  headerRows: number
  maxRows: number
  ignoreBlankRows: boolean
  ignoreTextCase: boolean
  trimLeadingWhitespace: boolean
  trimTrailingWhitespace: boolean
  ignoreCols: string
  sheetType: string
  csvSeparator: string
  csvQuote: string
  csvEscape: string
  csvNull: string
  csvEol: string
  sheetId: string
  range: string
}

export function createDefaultUploadForm(): UploadFormState
{
  return {
    mangaName: '',
    headerRow: 1,
    headerRows: 1,
    maxRows: 0,
    ignoreBlankRows: true,
    ignoreTextCase: true,
    trimLeadingWhitespace: true,
    trimTrailingWhitespace: true,
    ignoreCols: '',
    sheetType: 'csv',
    csvSeparator: 'comma',
    csvQuote: 'doublequote',
    csvEscape: 'doublequote',
    csvNull: 'null',
    csvEol: 'crlf',
    sheetId: '',
    range: ''
  }
}

export function validateUploadForm(form: UploadFormState)
{
  const errors: string[] = []

  if (!form.mangaName.trim()) {
    errors.push('MatrixEase name is required.')
  }

  if (form.headerRow < 0 || (form.headerRow === 0 && form.headerRows > 0)) {
    errors.push('Header row must be zero or greater, and zero requires zero header rows.')
  }

  if (form.headerRow > form.headerRows) {
    errors.push('Header on row cannot be greater than header rows.')
  }

  if (!['excel', 'csv'].includes(form.sheetType)) {
    errors.push('Sheet type must be csv or excel.')
  }

  return errors
}

export function summarizePayload(payload: MatrixEasePayload)
{
  const columns = Object.values(payload.Columns)

  return {
    columnCount: columns.length,
    measureCount: columns.filter(column => column.ColType === 'Measure').length,
    bucketizedCount: columns.filter(column => column.Bucketized).length
  }
}
