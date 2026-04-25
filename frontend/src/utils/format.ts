export function formatNumber(value: number)
{
  return new Intl.NumberFormat('en-US').format(value)
}

export function formatDate(value: string)
{
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(date)
}
