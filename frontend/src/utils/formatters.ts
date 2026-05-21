export function formatRows(rows: number | null): string {
  if (rows === null || rows === undefined) {
    return 'N/A'
  }

  return rows.toLocaleString()
}

export function formatLimit(maxRows: number): string {
  return maxRows === 0 ? 'All' : maxRows.toLocaleString()
}

export function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  return date.toLocaleString()
}
