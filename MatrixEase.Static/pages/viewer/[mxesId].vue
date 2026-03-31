<script setup lang="ts">
const route = useRoute()
const api = useMatrixEaseApi()
const session = useMatrixEaseSession()

const loading = ref(true)
const errorMessage = ref('')
const mangaName = ref('')
const payload = ref<MatrixEasePayload | null>(null)

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

const topValues = (column: MatrixEaseColumn) => column.Values.slice(0, 12)

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
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load MatrixEase viewer.'
  } finally {
    loading.value = false
  }
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

      <div v-if="loading" class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <USkeleton v-for="index in 6" :key="index" class="h-44 w-full" />
      </div>

      <section v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
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
