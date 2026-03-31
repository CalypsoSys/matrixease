<script setup lang="ts">
const runtime = useMatrixEaseRuntime()
const platform = useMatrixEasePlatform()
const api = useMatrixEaseApi()
const session = useMatrixEaseSession()

const loading = ref(true)
const loadingProjects = ref(false)
const uploading = ref(false)
const checkingGoogle = ref(false)
const uploadProgress = ref(0)
const projects = ref<MatrixEaseCatalogEntry[]>([])
const statusMessage = ref('')
const errorMessage = ref('')

const form = reactive({
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
})

const selectedFile = ref<File | null>(null)

const readyForCatalog = computed(() => session.hasAcceptedCookies.value && session.hasAccessToken.value)

const validationErrors = computed(() =>
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

  return errors
})

const loadProjects = async () =>
{
  if (!readyForCatalog.value || !session.matrixEaseId.value) {
    projects.value = []
    return
  }

  loadingProjects.value = true
  errorMessage.value = ''

  try {
    const response = await api.getMyMangas(session.matrixEaseId.value)
    projects.value = response.Success ? response.MyMangas : []

    if (!response.Success) {
      errorMessage.value = 'The API did not return a project catalog.'
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load projects.'
  } finally {
    loadingProjects.value = false
  }
}

const initialize = async () =>
{
  loading.value = true
  errorMessage.value = ''

  try {
    await session.refreshBootstrap()

    if (readyForCatalog.value) {
      const hasValidAccess = await session.validateAccess()
      if (hasValidAccess) {
        await loadProjects()
      }
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to initialize session.'
  } finally {
    loading.value = false
  }
}

const onFileSelected = (event: Event) =>
{
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

const submitUpload = async () =>
{
  if (!selectedFile.value) {
    errorMessage.value = 'Choose a CSV or spreadsheet file before uploading.'
    return
  }

  if (validationErrors.value.length > 0 || !session.matrixEaseId.value) {
    errorMessage.value = validationErrors.value.join(' ')
    return
  }

  uploading.value = true
  uploadProgress.value = 0
  statusMessage.value = ''
  errorMessage.value = ''

  try {
    const response = await api.uploadSheet({
      matrixEaseId: session.matrixEaseId.value,
      file: selectedFile.value,
      mangaName: form.mangaName,
      headerRow: form.headerRow,
      headerRows: form.headerRows,
      maxRows: form.maxRows,
      ignoreBlankRows: form.ignoreBlankRows,
      ignoreTextCase: form.ignoreTextCase,
      trimLeadingWhitespace: form.trimLeadingWhitespace,
      trimTrailingWhitespace: form.trimTrailingWhitespace,
      ignoreCols: form.ignoreCols,
      sheetType: form.sheetType,
      csvSeparator: form.csvSeparator,
      csvQuote: form.csvQuote,
      csvEscape: form.csvEscape,
      csvNull: form.csvNull,
      csvEol: form.csvEol
    }, value => {
      uploadProgress.value = value
    })

    if (response.Success) {
      statusMessage.value = 'Upload accepted. Background processing status polling is the next migration slice.'
      await loadProjects()
      return
    }

    errorMessage.value = response.Error ?? 'Upload failed.'
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Upload failed.'
  } finally {
    uploading.value = false
  }
}

const submitGoogleSheet = async () =>
{
  if (validationErrors.value.length > 0 || !session.matrixEaseId.value) {
    errorMessage.value = validationErrors.value.join(' ')
    return
  }

  checkingGoogle.value = true
  statusMessage.value = ''
  errorMessage.value = ''

  try {
    const googleStatus = await api.checkGoogleLogin(session.matrixEaseId.value)
    if (!googleStatus.Success) {
      errorMessage.value = 'Google authentication is required before importing a sheet.'
      return
    }

    const response = await api.submitGoogleSheet({
      matrixEaseId: session.matrixEaseId.value,
      mangaName: form.mangaName,
      headerRow: form.headerRow,
      headerRows: form.headerRows,
      maxRows: form.maxRows,
      ignoreBlankRows: form.ignoreBlankRows,
      ignoreTextCase: form.ignoreTextCase,
      trimLeadingWhitespace: form.trimLeadingWhitespace,
      trimTrailingWhitespace: form.trimTrailingWhitespace,
      ignoreCols: form.ignoreCols,
      sheetId: form.sheetId,
      range: form.range
    })

    if (response.Success) {
      statusMessage.value = 'Google Sheet processing started. Status polling is still being migrated.'
      await loadProjects()
      return
    }

    errorMessage.value = response.Error ?? 'Google Sheet import failed.'
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Google Sheet import failed.'
  } finally {
    checkingGoogle.value = false
  }
}

onMounted(async () =>
{
  await initialize()
})
</script>

<template>
  <main class="min-h-screen text-slate-900">
    <div class="mx-auto flex min-h-screen max-w-7xl flex-col gap-8 px-4 py-6 sm:px-6 lg:px-8">
      <section class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/80 shadow-2xl shadow-slate-300/40 backdrop-blur">
        <div class="grid gap-8 p-8 lg:grid-cols-[1.3fr_0.9fr] lg:p-10">
          <div class="space-y-6">
            <div class="flex flex-wrap items-center gap-3">
              <UBadge color="primary" variant="soft" size="lg">
                MatrixEase
              </UBadge>
              <UBadge :color="runtime.platform === 'electron' ? 'warning' : 'primary'" variant="outline">
                {{ runtime.platform }}
              </UBadge>
            </div>

            <div class="space-y-4">
              <h1 class="max-w-4xl text-4xl font-semibold tracking-tight text-slate-950 md:text-6xl">
                One shared UI for static web delivery and Electron.
              </h1>
              <p class="max-w-2xl text-base leading-7 text-slate-600 md:text-lg">
                This page is the first migrated slice of the MatrixEase frontend. It initializes the session, reads the project catalog, and uses the new API-oriented upload paths instead of the legacy script bootstrap and duplicated wwwroot assets.
              </p>
            </div>

            <div class="flex flex-wrap gap-3">
              <UButton color="primary" @click="platform.openLink('docs')">
                Documentation
              </UButton>
              <UButton color="neutral" variant="outline" @click="platform.openLink('about')">
                About
              </UButton>
              <UButton color="neutral" variant="ghost" @click="platform.openLink('support')">
                Support
              </UButton>
            </div>
          </div>

          <UCard class="border-slate-200/80 bg-slate-950 text-white">
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm font-medium uppercase tracking-[0.3em] text-teal-200">Session</span>
                <UBadge :color="readyForCatalog ? 'success' : 'warning'" variant="soft">
                  {{ readyForCatalog ? 'catalog ready' : 'needs auth' }}
                </UBadge>
              </div>
            </template>

            <div class="space-y-4 text-sm">
              <div>
                <p class="text-slate-400">API base</p>
                <p class="break-all font-mono text-teal-200">{{ runtime.apiBase }}</p>
              </div>
              <div>
                <p class="text-slate-400">MatrixEase ID</p>
                <p class="break-all font-mono text-white">{{ session.matrixEaseId || 'pending' }}</p>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <p class="text-slate-400">Cookies</p>
                  <p class="font-medium text-white">{{ session.hasAcceptedCookies ? 'accepted' : 'required' }}</p>
                </div>
                <div>
                  <p class="text-slate-400">Access token</p>
                  <p class="font-medium text-white">{{ session.hasAccessToken ? 'present' : 'missing' }}</p>
                </div>
              </div>

              <UButton
                v-if="!session.hasAcceptedCookies"
                color="primary"
                block
                @click="session.acceptCookies()"
              >
                Accept Cookies For Local Session
              </UButton>
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
        v-if="statusMessage"
        color="success"
        variant="soft"
        :description="statusMessage"
      />

      <section class="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <UCard class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur">
          <template #header>
            <div class="flex items-center justify-between gap-3">
              <div>
                <h2 class="text-xl font-semibold text-slate-950">Projects</h2>
                <p class="text-sm text-slate-500">Saved MatrixEase jobs visible to the active session.</p>
              </div>
              <UButton color="neutral" variant="outline" :loading="loadingProjects" @click="loadProjects">
                Refresh
              </UButton>
            </div>
          </template>

          <div v-if="loading" class="py-10">
            <USkeleton class="h-24 w-full" />
          </div>

          <div v-else-if="projects.length === 0" class="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-sm leading-7 text-slate-600">
            No projects are visible yet. That can mean the session is not authenticated for catalog access, or you have not created one yet.
          </div>

          <div v-else class="overflow-x-auto">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-50 text-left text-slate-500">
                <tr>
                  <th class="px-4 py-3 font-medium">Name</th>
                  <th class="px-4 py-3 font-medium">Source</th>
                  <th class="px-4 py-3 font-medium">Type</th>
                  <th class="px-4 py-3 font-medium">Rows</th>
                  <th class="px-4 py-3 font-medium">Status</th>
                  <th class="px-4 py-3 font-medium">Created</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="project in projects" :key="`${project.Name}-${project.Created}`" class="bg-white/70">
                  <td class="px-4 py-3 font-medium text-slate-900">
                    <a class="text-teal-700 hover:text-teal-900" :href="project.Url">{{ project.Name }}</a>
                  </td>
                  <td class="px-4 py-3 text-slate-600">{{ project.Original }}</td>
                  <td class="px-4 py-3 text-slate-600">{{ project.Type }}</td>
                  <td class="px-4 py-3 text-slate-600">{{ project.TotalRows }}</td>
                  <td class="px-4 py-3">
                    <UBadge color="neutral" variant="subtle">{{ project.Status }}</UBadge>
                  </td>
                  <td class="px-4 py-3 text-slate-600">{{ new Date(project.Created).toLocaleString() }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </UCard>

        <div class="grid gap-6">
          <UCard class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur">
            <template #header>
              <div>
                <h2 class="text-xl font-semibold text-slate-950">Project Options</h2>
                <p class="text-sm text-slate-500">Shared parsing settings for file and Google Sheet imports.</p>
              </div>
            </template>

            <div class="grid gap-4 md:grid-cols-2">
              <UFormField label="MatrixEase name" required>
                <UInput v-model="form.mangaName" placeholder="Quarterly sales matrix" />
              </UFormField>
              <UFormField label="Ignore columns">
                <UInput v-model="form.ignoreCols" placeholder="Region, 4, 7" />
              </UFormField>
              <UFormField label="Header on row">
                <UInput v-model.number="form.headerRow" type="number" min="0" />
              </UFormField>
              <UFormField label="Header rows">
                <UInput v-model.number="form.headerRows" type="number" min="0" />
              </UFormField>
              <UFormField label="Stop after N rows">
                <UInput v-model.number="form.maxRows" type="number" min="0" />
              </UFormField>
              <div class="grid grid-cols-2 gap-3">
                <UCheckbox v-model="form.ignoreBlankRows" label="Ignore blank rows" />
                <UCheckbox v-model="form.ignoreTextCase" label="Ignore text case" />
                <UCheckbox v-model="form.trimLeadingWhitespace" label="Trim leading whitespace" />
                <UCheckbox v-model="form.trimTrailingWhitespace" label="Trim trailing whitespace" />
              </div>
            </div>

            <ul v-if="validationErrors.length > 0" class="mt-4 list-disc space-y-1 pl-5 text-sm text-rose-700">
              <li v-for="message in validationErrors" :key="message">{{ message }}</li>
            </ul>
          </UCard>

          <UCard class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur">
            <template #header>
              <div>
                <h2 class="text-xl font-semibold text-slate-950">Load File Based Sheet</h2>
                <p class="text-sm text-slate-500">Uses the new `/api/uploads/file` endpoint.</p>
              </div>
            </template>

            <div class="grid gap-4">
              <div class="grid gap-4 md:grid-cols-2">
                <UFormField label="Source type">
                  <USelect
                    v-model="form.sheetType"
                    :items="[
                      { label: 'CSV', value: 'csv' },
                      { label: 'Excel', value: 'excel' }
                    ]"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
                <UFormField v-if="form.sheetType === 'csv'" label="CSV separator">
                  <USelect
                    v-model="form.csvSeparator"
                    :items="[
                      { label: 'Comma', value: 'comma' },
                      { label: 'Tab', value: 'tab' },
                      { label: 'Space', value: 'space' },
                      { label: 'Pipe', value: 'pipe' },
                      { label: 'Colon', value: 'colon' },
                      { label: 'Semicolon', value: 'semicolon' }
                    ]"
                    option-attribute="label"
                    value-attribute="value"
                  />
                </UFormField>
              </div>

              <UFormField label="File">
                <input
                  class="block w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-700"
                  type="file"
                  @change="onFileSelected"
                >
              </UFormField>

              <UProgress v-if="uploading" :model-value="uploadProgress" />

              <UButton color="primary" :loading="uploading" @click="submitUpload">
                Upload and Process File
              </UButton>
            </div>
          </UCard>

          <UCard class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur">
            <template #header>
              <div>
                <h2 class="text-xl font-semibold text-slate-950">Load Google Sheet</h2>
                <p class="text-sm text-slate-500">Uses the new `/api/google/*` routes.</p>
              </div>
            </template>

            <div class="grid gap-4">
              <UFormField label="Sheet ID">
                <UInput v-model="form.sheetId" placeholder="Google Sheet ID" />
              </UFormField>
              <UFormField label="Range">
                <UInput v-model="form.range" placeholder="Sheet1!A1:Z5000" />
              </UFormField>

              <UButton color="neutral" :loading="checkingGoogle" @click="submitGoogleSheet">
                Import Google Sheet
              </UButton>
            </div>
          </UCard>
        </div>
      </section>
    </div>
  </main>
</template>
