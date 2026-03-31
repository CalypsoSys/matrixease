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

export type MatrixEaseJobStartResponse = {
  Success: boolean
  Error?: string
  MatrixId?: string
  PickupKey?: string
  StatusData?: unknown
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

function apiUrl(path: string)
{
  const runtime = useMatrixEaseRuntime()
  return new URL(path, runtime.apiBase).toString()
}

export function useMatrixEaseApi()
{
  const bootstrapSession = () =>
    $fetch<SessionBootstrap>(apiUrl('/api/session/bootstrap'), {
      credentials: 'include'
    })

  const validateCatalogAccess = (matrixEaseId: string, accessToken: string) =>
    $fetch<MatrixEaseAccessResponse>(apiUrl('/api/session/access'), {
      credentials: 'include',
      query: {
        matrixease_id: matrixEaseId,
        access_token: accessToken
      }
    })

  const getMyMangas = (matrixEaseId: string) =>
    $fetch<MatrixEaseCatalogResponse>(apiUrl('/api/session/my_mangas'), {
      credentials: 'include',
      query: {
        matrixease_id: matrixEaseId
      }
    })

  const checkGoogleLogin = (matrixEaseId: string) =>
    $fetch<{ Success: boolean }>(apiUrl('/api/google/check_login'), {
      credentials: 'include',
      query: {
        matrixease_id: matrixEaseId
      }
    })

  const uploadSheet = async (request: UploadSheetRequest, onProgress?: (value: number) => void) =>
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
      xhr.open('POST', apiUrl('/api/uploads/file'))
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
  }

  const submitGoogleSheet = (request: GoogleSheetRequest) =>
    $fetch<MatrixEaseJobStartResponse>(apiUrl('/api/google/sheet'), {
      credentials: 'include',
      query: {
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
      }
    })

  return {
    bootstrapSession,
    validateCatalogAccess,
    getMyMangas,
    checkGoogleLogin,
    uploadSheet,
    submitGoogleSheet
  }
}
