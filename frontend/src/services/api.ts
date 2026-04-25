import type { MatrixEaseRuntime } from '@/services/runtime'

export type SessionBootstrap = {
  Success: boolean
  MatrixEaseId: string
  CookiesAccepted: boolean
  HasCatalogAccess: boolean
  GoogleSignedIn: boolean
  HasEmailIdentity: boolean
}

export type MatrixEaseCatalogEntry = {
  Name: string
  Url: string
  ViewerPath?: string
  Original: string
  Type: string
  Created: string
  MaxRows: number
  TotalRows: string | number
  Status: string
}

export type MatrixEaseCatalogResponse = {
  Success: boolean
  MyMangas: MatrixEaseCatalogEntry[]
}

export type MatrixEaseAccessResponse = {
  Success: boolean
  AccessToken: string
}

export type MatrixEaseCaptchaResponse = {
  num1: number
  num2: number
}

export type MatrixEaseJobStartResponse = {
  Success: boolean
  Error?: string
  MatrixId?: string
  PickupKey?: string
  StatusData?: unknown
}

export type MatrixEaseStatusResponse = {
  Success: boolean
  Complete?: boolean
  Message?: string
  StatusData?: Record<string, unknown>
  Results?: MatrixEasePayload
}

export type MatrixEaseColumnValue = {
  ColumnValue: string
  Duplicates: number
  TotalPct: number
  SelectAllPct: number
  SelectRelPct: number
  TotalValues: number
  SelectedValues: number
}

export type MatrixEaseColumn = {
  Index: number
  ColType: string
  DataType: string
  NullEmpty: number
  Selectivity: number
  DistinctValues: number
  Bucketized: boolean
  OnlyBuckets: boolean
  CurBucketSize: number
  MinBucketSize: number
  CurBucketMod: number
  MinBucketMod: number
  AllowedBuckets: string[]
  Attributes: Record<string, unknown> | null
  Values: MatrixEaseColumnValue[]
}

export type MatrixEasePayload = {
  TotalRows: number
  SelectedRows: number
  Columns: Record<string, MatrixEaseColumn>
  ShowLowEqual: boolean
  ShowLowBound: number
  ShowHighEqual: boolean
  ShowHighBound: number
  ShowPercentage: string
  SelectOperation: string
  SelectionExpression: string | null
  ColAscending: boolean
  HideColumns: boolean[]
}

export type MatrixEaseViewerResponse = {
  MangaName: string
  MangaData: MatrixEasePayload
}

export type MatrixEaseColumnStatsResponse = {
  Success: boolean
  ColStats?: Record<string, unknown>
}

export type MatrixEaseReportResponse = {
  Success: boolean
  ReportData?: {
    columns: string[]
    data: Array<Array<string | number | boolean | null>>
  }
}

export type MatrixEaseDuplicatesResponse = {
  Success: boolean
  DuplicateEntries?: string[]
}

export type MatrixEaseDependencyResponse = {
  Success: boolean
  DependencyDiagram?: Record<string, unknown>
}

export type MatrixEaseMeasuresResponse = {
  Success: boolean
  MeasureStats?: Record<string, Record<string, string | number | null>>
}

export type MatrixEaseQuickDataResponse = {
  Success: boolean
  ChartData?: {
    labels: string[]
    datasets: Array<{
      label: string
      data: number[]
      backgroundColor?: string | string[]
      borderColor?: string | string[]
    }>
  }
  ReportData?: {
    columns: string[]
    data: Array<Array<string | number | boolean | null>>
  }
}

export type UploadSheetRequest = {
  matrixEaseId: string
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

export type GoogleSheetRequest = {
  matrixEaseId: string
  mangaName: string
  headerRow: number
  headerRows: number
  maxRows: number
  ignoreBlankRows: boolean
  ignoreTextCase: boolean
  trimLeadingWhitespace: boolean
  trimTrailingWhitespace: boolean
  ignoreCols: string
  sheetId: string
  range: string
}

function buildApiUrl(apiBase: string, path: string)
{
  return new URL(path, apiBase).toString()
}

async function requestJson<T>(url: string, init?: RequestInit): Promise<T>
{
  const response = await fetch(url, {
    credentials: 'include',
    ...init
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return await response.json() as T
}

function withQuery(url: string, query: Record<string, string | number | boolean | undefined>)
{
  const target = new URL(url)

  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === '') {
      continue
    }

    target.searchParams.set(key, String(value))
  }

  return target.toString()
}

export function createMatrixEaseApi(runtime: MatrixEaseRuntime)
{
  const urlFor = (path: string) => buildApiUrl(runtime.apiBase, path)

  return {
    bootstrapSession: () =>
      requestJson<SessionBootstrap>(urlFor('/api/session/bootstrap')),

    validateCatalogAccess: (matrixEaseId: string, accessToken: string) =>
      requestJson<MatrixEaseAccessResponse>(withQuery(urlFor('/api/session/access'), {
        matrixease_id: matrixEaseId,
        access_token: accessToken
      })),

    getCaptcha: (matrixEaseId: string) =>
      requestJson<MatrixEaseCaptchaResponse>(withQuery(urlFor('/api/session/captcha'), {
        matrixease_id: matrixEaseId
      })),

    sendEmailCode: (matrixEaseId: string, emailAddress: string, captchaResult: string) =>
      requestJson<{ Success: boolean }>(withQuery(urlFor('/api/session/send_email_code'), {
        matrixease_id: matrixEaseId,
        email_to_address: emailAddress,
        result: captchaResult
      })),

    validateEmailCode: (matrixEaseId: string, emailAddress: string, emailCode: string) =>
      requestJson<{ Success: boolean }>(withQuery(urlFor('/api/session/validate_email_code'), {
        matrixease_id: matrixEaseId,
        email_to_address: emailAddress,
        emailed_code: emailCode
      })),

    getMyMangas: (matrixEaseId: string) =>
      requestJson<MatrixEaseCatalogResponse>(withQuery(urlFor('/api/session/my_mangas'), {
        matrixease_id: matrixEaseId
      })),

    checkGoogleLogin: (matrixEaseId: string) =>
      requestJson<{ Success: boolean }>(withQuery(urlFor('/api/google/check_login'), {
        matrixease_id: matrixEaseId
      })),

    getMangaStatus: (matrixEaseId: string, statusKey: string) =>
      requestJson<MatrixEaseStatusResponse>(withQuery(urlFor('/api/matrixease/manga_status'), {
        matrixease_id: matrixEaseId,
        status_key: statusKey
      })),

    getManga: (matrixEaseId: string, mxesId: string) =>
      requestJson<MatrixEaseViewerResponse>(withQuery(urlFor('/api/matrixease'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId
      })),

    deleteManga: (matrixEaseId: string, mxesId: string) =>
      requestJson<{ Success: boolean }>(withQuery(urlFor('/api/matrixease/delete_manga'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId
      })),

    exportCsv: async (matrixEaseId: string, mxesId: string) =>
    {
      const response = await fetch(withQuery(urlFor('/api/matrixease/export_csv'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId
      }), {
        credentials: 'include'
      })

      if (!response.ok) {
        throw new Error(`Export failed with status ${response.status}`)
      }

      return await response.blob()
    },

    getPickupStatus: (matrixEaseId: string, mxesId: string, pickupKey: string) =>
      requestJson<MatrixEaseStatusResponse>(withQuery(urlFor('/api/matrixease/manga_pickup_status'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        pickup_key: pickupKey
      })),

    applyFilter: (matrixEaseId: string, mxesId: string, selectionExpression: string) =>
      requestJson<MatrixEaseJobStartResponse>(withQuery(urlFor('/api/matrixease/filter'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        selection_expression: selectionExpression
      })),

    updateSettings: (matrixEaseId: string, mxesId: string, settings: {
      showLowEqual: boolean
      showLowBound: number
      showHighEqual: boolean
      showHighBound: number
      selectOperation: string
      showPercentage: string
      colAscending: boolean
      hideColumns: boolean[]
    }) =>
      requestJson<{ Success: boolean }>(withQuery(urlFor('/api/matrixease/update_settings'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        show_low_equal: settings.showLowEqual,
        show_low_bound: settings.showLowBound,
        show_high_equal: settings.showHighEqual,
        show_high_bound: settings.showHighBound,
        select_operation: settings.selectOperation,
        show_percentage: settings.showPercentage,
        col_ascending: settings.colAscending,
        hide_columns: settings.hideColumns.join(',')
      })),

    bucketizeColumn: (matrixEaseId: string, mxesId: string, request: {
      columnName: string
      columnIndex: number
      bucketized: boolean
      bucketSize: number
      bucketMod: number
    }) =>
      requestJson<MatrixEaseJobStartResponse>(withQuery(urlFor('/api/matrixease/bucketize'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        column_name: request.columnName,
        column_index: request.columnIndex,
        bucketized: request.bucketized,
        bucket_size: request.bucketSize,
        bucket_mod: request.bucketMod
      })),

    getDetailedColumnStats: (matrixEaseId: string, mxesId: string, columnName: string, columnIndex: number) =>
      requestJson<MatrixEaseColumnStatsResponse>(withQuery(urlFor('/api/matrixease/detailed_col_stats'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        column_name: columnName,
        column_index: columnIndex
      })),

    getNodeRows: (matrixEaseId: string, mxesId: string, columnIndex: number, selectedNode: string) =>
      requestJson<MatrixEaseReportResponse>(withQuery(urlFor('/api/matrixease/get_node_rows'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        col_index: columnIndex,
        selected_node: selectedNode,
        filtered: true
      })),

    getDuplicateEntries: (matrixEaseId: string, mxesId: string, columnIndex: number, selectedNode: string) =>
      requestJson<MatrixEaseDuplicatesResponse>(withQuery(urlFor('/api/matrixease/get_duplicate_entries'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        col_index: columnIndex,
        selected_node: selectedNode,
        filtered: true
      })),

    getDependencyDiagram: (matrixEaseId: string, mxesId: string, columnIndex: number, selectedNode: string) =>
      requestJson<MatrixEaseDependencyResponse>(withQuery(urlFor('/api/matrixease/get_dependency_diagram'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        col_index: columnIndex,
        selected_node: selectedNode,
        filtered: true
      })),

    getColumnMeasures: (matrixEaseId: string, mxesId: string, columnIndex: number, selectedNode: string, measureIndexes: number[], filtered: boolean) =>
      requestJson<MatrixEaseMeasuresResponse>(withQuery(urlFor('/api/matrixease/get_col_measures'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        col_index: columnIndex,
        selected_node: selectedNode,
        col_measure_indexes: measureIndexes.join(','),
        filtered
      })),

    getQuickChartData: (matrixEaseId: string, mxesId: string, request: {
      chartType: string
      dimensionIndexes: number[]
      totalMeasureIndexes: number[]
      averageMeasureIndexes: number[]
      countMeasureIndexes: number[]
      filtered: boolean
    }) =>
      requestJson<MatrixEaseQuickDataResponse>(withQuery(urlFor('/api/matrixease/get_chart_data'), {
        matrixease_id: matrixEaseId,
        mxes_id: mxesId,
        chart_type: request.chartType,
        col_dimension_indexes: request.dimensionIndexes.join(','),
        col_measure_tot_indexes: request.totalMeasureIndexes.join(','),
        col_measure_avg_indexes: request.averageMeasureIndexes.join(','),
        col_measure_cnt_indexes: request.countMeasureIndexes.join(','),
        filtered: request.filtered
      })),

    uploadSheet: async (request: UploadSheetRequest, onProgress?: (value: number) => void) =>
    {
      const formData = new FormData()
      formData.append('matrixease_id', request.matrixEaseId)
      formData.append('file', request.file)
      formData.append('manga_name', request.mangaName)
      formData.append('header_row', request.headerRow.toString())
      formData.append('header_rows', request.headerRows.toString())
      formData.append('max_rows', request.maxRows.toString())
      formData.append('ignore_blank_rows', String(request.ignoreBlankRows))
      formData.append('ignore_text_case', String(request.ignoreTextCase))
      formData.append('trim_leading_whitespace', String(request.trimLeadingWhitespace))
      formData.append('trim_trailing_whitespace', String(request.trimTrailingWhitespace))
      formData.append('ignore_cols', request.ignoreCols)
      formData.append('sheet_type', request.sheetType)
      formData.append('csv_separator', request.csvSeparator)
      formData.append('csv_quote', request.csvQuote)
      formData.append('csv_escape', request.csvEscape)
      formData.append('csv_null', request.csvNull)
      formData.append('csv_eol', request.csvEol)

      return await new Promise<MatrixEaseJobStartResponse>((resolve, reject) =>
      {
        const xhr = new XMLHttpRequest()
        xhr.open('POST', urlFor('/api/uploads/file'))
        xhr.withCredentials = true

        xhr.upload.addEventListener('progress', event =>
        {
          if (!event.lengthComputable || !onProgress) {
            return
          }

          onProgress(Math.round((event.loaded / event.total) * 100))
        })

        xhr.onload = () =>
        {
          if (xhr.status >= 200 && xhr.status < 300) {
            resolve(JSON.parse(xhr.responseText) as MatrixEaseJobStartResponse)
            return
          }

          reject(new Error(`Upload failed with status ${xhr.status}`))
        }

        xhr.onerror = () => reject(new Error('Upload failed'))
        xhr.send(formData)
      })
    },

    submitGoogleSheet: (request: GoogleSheetRequest) =>
      requestJson<MatrixEaseJobStartResponse>(withQuery(urlFor('/api/google/sheet'), {
        matrixease_id: request.matrixEaseId,
        manga_name: request.mangaName,
        header_row: request.headerRow,
        header_rows: request.headerRows,
        max_rows: request.maxRows,
        ignore_blank_rows: request.ignoreBlankRows,
        ignore_text_case: request.ignoreTextCase,
        trim_leading_whitespace: request.trimLeadingWhitespace,
        trim_trailing_whitespace: request.trimTrailingWhitespace,
        ignore_cols: request.ignoreCols,
        sheet_id: request.sheetId,
        range: request.range
      }))
  }
}

export { buildApiUrl }
export type MatrixEaseApi = ReturnType<typeof createMatrixEaseApi>
