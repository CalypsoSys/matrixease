import { apiGet, apiPost } from '@/services/api'

export type MatrixEaseProject = {
  ProjectId: string
  Name: string
  OriginalName: string
  SheetType: string
  Created: string
  MaxRows: number
  TotalRows: number | null
  Status: string
  IsPending: boolean
}

export type MatrixEaseProjectsResponse = {
  Success: boolean
  Message?: string
  Projects: MatrixEaseProject[]
}

export type MatrixEaseUploadOptions = {
  file: File
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
}

export type MatrixEaseStatusEntry = {
  Started?: string
  Elapsed?: string
  Desc?: string
  Status?: string
}

export type MatrixEaseStatusMap = Record<string, MatrixEaseStatusEntry>

export type MatrixEaseUploadResponse = {
  Success: boolean
  MatrixId?: string
  StatusData?: MatrixEaseStatusMap
  Error?: string
  Message?: string
}

export type MatrixEaseStatusResponse = {
  Success: boolean
  Complete?: boolean
  StatusData?: MatrixEaseStatusMap
  Message?: string
}

export function fetchProjects(): Promise<MatrixEaseProjectsResponse> {
  return apiGet<MatrixEaseProjectsResponse>('/api/matrixease/projects')
}

export function uploadProject(
  options: MatrixEaseUploadOptions,
  onProgress?: (percent: number) => void
): Promise<MatrixEaseUploadResponse> {
  const formData = new FormData()
  formData.append('file', options.file)
  formData.append('manga_name', options.mangaName)
  formData.append('header_row', String(options.headerRow))
  formData.append('header_rows', String(options.headerRows))
  formData.append('max_rows', String(options.maxRows))
  formData.append('ignore_blank_rows', String(options.ignoreBlankRows))
  formData.append('ignore_text_case', String(options.ignoreTextCase))
  formData.append('trim_leading_whitespace', String(options.trimLeadingWhitespace))
  formData.append('trim_trailing_whitespace', String(options.trimTrailingWhitespace))
  formData.append('ignore_cols', options.ignoreCols)
  formData.append('sheet_type', options.sheetType)
  formData.append('csv_separator', options.csvSeparator)
  formData.append('csv_quote', options.csvQuote)
  formData.append('csv_escape', options.csvEscape)
  formData.append('csv_null', options.csvNull)
  formData.append('csv_eol', options.csvEol)

  return apiPost<MatrixEaseUploadResponse>('/api/matrixease/upload', formData, {
    onUploadProgress: (event) => {
      if (onProgress && event.total) {
        onProgress(Math.round((event.loaded / event.total) * 100))
      }
    }
  })
}

export function fetchProjectStatus(statusKey: string): Promise<MatrixEaseStatusResponse> {
  return apiGet<MatrixEaseStatusResponse>(`/api/matrixease/manga_status?status_key=${encodeURIComponent(statusKey)}`)
}
