<template>
  <main v-if="auth.isReady" class="app-shell">
    <section v-if="!auth.isAuthenticated" class="login-wrap">
      <div class="panel login-panel">
        <div class="panel-header">
          <div class="brand-lockup">
            <img :src="logoUrl" alt="MatrixEase" />
            <div>
              <h1 class="brand-title">MatrixEase</h1>
              <p class="brand-subtitle">Project workspace</p>
            </div>
          </div>
        </div>
        <form class="panel-body form-stack" @submit.prevent="submitAuth">
          <div v-if="authError" class="message-error">{{ authError }}</div>
          <div v-if="authNotice" class="p-3 text-sm text-teal-800 bg-teal-50 border border-teal-100 rounded">
            {{ authNotice }}
          </div>

          <label>
            <span class="field-label">Email</span>
            <InputText v-model.trim="emailAddress" class="w-full" autocomplete="email" type="email" required />
          </label>

          <label>
            <span class="field-label">Password</span>
            <Password
              v-model="password"
              class="w-full"
              input-class="w-full"
              autocomplete="current-password"
              :feedback="false"
              toggle-mask
              required
            />
          </label>

          <div class="form-actions">
            <Button :label="authMode === 'signin' ? 'Sign in' : 'Create account'" icon="pi pi-user" type="submit" :loading="authBusy" />
            <Button
              class="p-button-text"
              :label="authMode === 'signin' ? 'Create account' : 'Use existing account'"
              type="button"
              @click="toggleAuthMode"
            />
          </div>
        </form>
      </div>
    </section>

    <section v-else>
      <header class="app-topbar">
        <div class="brand-lockup">
          <img :src="logoUrl" alt="MatrixEase" />
          <div>
            <h1 class="brand-title">MatrixEase</h1>
            <p class="brand-subtitle">Project workspace</p>
          </div>
        </div>
        <div class="topbar-actions">
          <span v-if="auth.email" class="account-email">{{ auth.email }}</span>
          <Button icon="pi pi-refresh" label="Refresh" severity="secondary" :loading="loadingProjects" @click="loadProjects()" />
          <Button icon="pi pi-sign-out" label="Sign out" severity="contrast" @click="signOut" />
        </div>
      </header>

      <div class="page-frame">
        <section v-if="activeMatrixProject" class="matrix-workspace">
          <div class="matrix-toolbar">
            <Button icon="pi pi-arrow-left" label="Projects" severity="secondary" outlined @click="closeMatrix" />
            <div>
              <h2 class="panel-title">{{ matrixName || activeMatrixProject.Name }}</h2>
              <p class="brand-subtitle">{{ activeMatrixProject.OriginalName }}</p>
            </div>
            <Button icon="pi pi-refresh" label="Reload" severity="secondary" :loading="matrixLoading" @click="openProjectMatrix(activeMatrixProject)" />
          </div>

          <div v-if="matrixError" class="message-error">{{ matrixError }}</div>
          <section v-if="matrixLoading" class="panel matrix-loading">
            <i class="pi pi-spin pi-spinner" aria-hidden="true"></i>
            <strong>Loading MatrixEase data</strong>
          </section>

          <template v-else-if="matrixData">
            <section class="matrix-summary-grid">
              <div class="matrix-stat">
                <span>Total rows</span>
                <strong>{{ formatRows(matrixData.TotalRows) }}</strong>
              </div>
              <div class="matrix-stat">
                <span>Selected rows</span>
                <strong>{{ formatRows(matrixData.SelectedRows) }}</strong>
              </div>
              <div class="matrix-stat">
                <span>Columns</span>
                <strong>{{ matrixColumns.length.toLocaleString() }}</strong>
              </div>
              <div class="matrix-stat">
                <span>Selection</span>
                <strong>{{ matrixData.SelectionExpression || 'All rows' }}</strong>
              </div>
            </section>

            <div class="matrix-layout">
              <aside class="panel matrix-column-panel">
                <div class="panel-header">
                  <div>
                    <h3 class="panel-title">Columns</h3>
                    <p class="brand-subtitle">{{ matrixColumns.length.toLocaleString() }} available</p>
                  </div>
                </div>
                <div class="matrix-column-list">
                  <button
                    v-for="column in matrixColumns"
                    :key="column.Name"
                    class="matrix-column-button"
                    :class="{ active: selectedMatrixColumn?.Name === column.Name }"
                    type="button"
                    @click="selectedMatrixColumnName = column.Name"
                  >
                    <span>{{ column.Name }}</span>
                    <small>{{ column.ColType }} · {{ column.DataType }} · {{ column.DistinctValues.toLocaleString() }}</small>
                  </button>
                </div>
              </aside>

              <section class="panel matrix-detail-panel">
                <template v-if="selectedMatrixColumn">
                  <div class="panel-header">
                    <div>
                      <h3 class="panel-title">{{ selectedMatrixColumn.Name }}</h3>
                      <p class="brand-subtitle">
                        {{ selectedMatrixColumn.ColType }} · {{ selectedMatrixColumn.DataType }} · {{ matrixBucketLabel(selectedMatrixColumn) }}
                      </p>
                    </div>
                    <span class="status-pill">{{ selectedMatrixColumn.Values.length.toLocaleString() }} values</span>
                  </div>

                  <div class="matrix-detail-body">
                    <div class="matrix-metric-grid">
                      <div>
                        <span>Distinct</span>
                        <strong>{{ selectedMatrixColumn.DistinctValues.toLocaleString() }}</strong>
                      </div>
                      <div>
                        <span>Empty</span>
                        <strong>{{ selectedMatrixColumn.NullEmpty.toLocaleString() }}</strong>
                      </div>
                      <div>
                        <span>Selectivity</span>
                        <strong>{{ formatDecimal(selectedMatrixColumn.Selectivity, 6) }}</strong>
                      </div>
                      <div>
                        <span>Top value</span>
                        <strong>{{ topColumnValue?.ColumnValue || 'N/A' }}</strong>
                      </div>
                    </div>

                    <div class="matrix-values-header">
                      <div>
                        <h4>Value distribution</h4>
                        <p class="brand-subtitle">Showing first {{ selectedColumnValues.length.toLocaleString() }} values</p>
                      </div>
                    </div>

                    <div class="matrix-values-table-wrap">
                      <table class="matrix-values-table">
                        <thead>
                          <tr>
                            <th>Value</th>
                            <th>Rows</th>
                            <th>Selected</th>
                            <th>Total %</th>
                            <th>Selected %</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr v-for="value in selectedColumnValues" :key="`${selectedMatrixColumn.Name}-${value.ColumnValue}`">
                            <th scope="row">
                              <div class="matrix-value-cell">
                                <span>{{ value.ColumnValue || '(blank)' }}</span>
                                <small v-if="value.Duplicates > 1">{{ value.Duplicates }} cases</small>
                              </div>
                            </th>
                            <td>{{ value.TotalValues.toLocaleString() }}</td>
                            <td>{{ value.SelectedValues.toLocaleString() }}</td>
                            <td>
                              <div class="pct-cell">
                                <span :style="{ width: `${Math.min(value.TotalPct, 100)}%` }"></span>
                              </div>
                              {{ formatPercent(value.TotalPct) }}
                            </td>
                            <td>{{ formatPercent(value.SelectRelPct) }}</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </div>
                </template>
                <div v-else class="empty-state">
                  <i class="pi pi-table" aria-hidden="true"></i>
                  <strong>No columns returned</strong>
                </div>
              </section>
            </div>
          </template>
        </section>

        <template v-else>
        <section class="panel upload-panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">New project</h2>
              <p class="brand-subtitle">CSV or Excel upload</p>
            </div>
          </div>
          <form class="panel-body upload-form" @submit.prevent="submitUpload">
            <div v-if="uploadError" class="message-error">{{ uploadError }}</div>
            <div v-if="uploadNotice" class="message-success">{{ uploadNotice }}</div>

            <div class="upload-grid">
              <label>
                <span class="field-label">Project name</span>
                <InputText v-model.trim="uploadName" class="w-full" required />
              </label>

              <label>
                <span class="field-label">File</span>
                <input ref="fileInput" class="native-file" type="file" accept=".csv,.txt,.tsv,.xls,.xlsx" required @change="handleFileChange" />
              </label>

              <label>
                <span class="field-label">Type</span>
                <select v-model="sheetType" class="native-select">
                  <option value="csv">CSV</option>
                  <option value="excel">Excel</option>
                </select>
              </label>

              <label v-if="sheetType === 'csv'">
                <span class="field-label">Separator</span>
                <select v-model="csvSeparator" class="native-select">
                  <option value="comma">Comma</option>
                  <option value="tab">Tab</option>
                  <option value="space">Space</option>
                  <option value="pipe">Pipe</option>
                  <option value="colon">Colon</option>
                  <option value="semicolon">Semicolon</option>
                </select>
              </label>

              <label>
                <span class="field-label">Header on row</span>
                <InputNumber v-model="headerRow" class="w-full" input-class="w-full" :min="0" show-buttons />
              </label>

              <label>
                <span class="field-label">Header rows</span>
                <InputNumber v-model="headerRows" class="w-full" input-class="w-full" :min="0" show-buttons />
              </label>

              <label>
                <span class="field-label">Max rows</span>
                <InputNumber v-model="maxRows" class="w-full" input-class="w-full" :min="0" show-buttons />
              </label>

              <label>
                <span class="field-label">Ignore columns</span>
                <InputText v-model.trim="ignoreCols" class="w-full" />
              </label>
            </div>

            <div class="toggle-grid">
              <label class="toggle-option">
                <Checkbox v-model="ignoreBlankRows" binary input-id="ignore-blank-rows" />
                <span>Ignore blank rows</span>
              </label>
              <label class="toggle-option">
                <Checkbox v-model="ignoreTextCase" binary input-id="ignore-text-case" />
                <span>Ignore text case</span>
              </label>
              <label class="toggle-option">
                <Checkbox v-model="trimLeadingWhitespace" binary input-id="trim-leading-whitespace" />
                <span>Trim leading whitespace</span>
              </label>
              <label class="toggle-option">
                <Checkbox v-model="trimTrailingWhitespace" binary input-id="trim-trailing-whitespace" />
                <span>Trim trailing whitespace</span>
              </label>
            </div>

            <div v-if="uploadBusy || uploadProgress > 0 || uploadStatusText" class="upload-status-row">
              <progress max="100" :value="uploadProgress"></progress>
              <span>{{ uploadStatusText || `${uploadProgress}%` }}</span>
              <Button
                v-if="currentUploadStatusKey"
                icon="pi pi-list"
                label="Details"
                type="button"
                severity="secondary"
                text
                @click="openCurrentUploadStatusDetails"
              />
            </div>

            <div class="form-actions">
              <Button icon="pi pi-upload" label="Upload" type="submit" :loading="uploadBusy" />
              <Button icon="pi pi-times" label="Reset" type="button" severity="secondary" outlined @click="resetUploadForm()" />
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">Projects</h2>
              <p class="brand-subtitle">Saved MatrixEase analyses</p>
            </div>
          </div>
          <div class="panel-body">
            <div v-if="projectError" class="message-error mb-4">{{ projectError }}</div>
            <DataTable :value="projects" :loading="loadingProjects" data-key="ProjectId" responsive-layout="scroll">
              <template #empty>
                <div class="empty-state">
                  <i class="pi pi-table" aria-hidden="true"></i>
                  <strong>No projects yet</strong>
                </div>
              </template>

              <Column field="Name" header="MatrixEase Name">
                <template #body="{ data }">
                  <span class="project-name">{{ data.Name }}</span>
                </template>
              </Column>
              <Column field="OriginalName" header="Source" />
              <Column field="SheetType" header="Type" />
              <Column field="MaxRows" header="Limit">
                <template #body="{ data }">{{ formatLimit(data.MaxRows) }}</template>
              </Column>
              <Column field="TotalRows" header="Rows">
                <template #body="{ data }">{{ formatRows(data.TotalRows) }}</template>
              </Column>
              <Column field="Status" header="Status">
                <template #body="{ data }">
                  <span class="status-pill" :data-status="statusKey(data)">
                    {{ data.Status || (data.IsPending ? 'Pending' : 'Unknown') }}
                  </span>
                </template>
              </Column>
              <Column field="Created" header="Created">
                <template #body="{ data }">{{ formatDateTime(data.Created) }}</template>
              </Column>
              <Column header="">
                <template #body="{ data }">
                  <Button
                    v-if="canOpenProject(data)"
                    icon="pi pi-external-link"
                    label="Open"
                    type="button"
                    @click="openProjectMatrix(data)"
                  />
                  <Button
                    v-if="canShowProjectStatusDetails(data)"
                    icon="pi pi-list"
                    label="Details"
                    type="button"
                    severity="secondary"
                    text
                    @click="openProjectStatusDetails(data)"
                  />
                </template>
              </Column>
            </DataTable>
          </div>
        </section>
        </template>
      </div>

      <Drawer v-model:visible="statusDialogVisible" position="right" class="process-drawer" :modal="false" @hide="closeStatusDialog">
        <template #header>
          <div class="process-drawer-heading">
            <span>Processing details</span>
            <strong>{{ statusDialogTitle }}</strong>
          </div>
        </template>

        <div class="process-drawer-body">
          <div v-if="statusDialogMessage" class="message-success">{{ statusDialogMessage }}</div>
          <div v-if="statusDialogError" class="message-error">{{ statusDialogError }}</div>
          <div v-if="statusDialogLoading && statusDialogRows.length === 0" class="muted">Loading status...</div>

          <section class="process-summary">
            <div>
              <span class="summary-label">Project</span>
              <strong>{{ statusDialogTitle }}</strong>
            </div>
            <span class="status-pill" :data-status="statusValueKey(statusDrawerStatus)">
              {{ statusDrawerStatus }}
            </span>
          </section>

          <dl class="process-meta">
            <div v-if="statusDrawerSource">
              <dt>Source</dt>
              <dd>{{ statusDrawerSource }}</dd>
            </div>
            <div v-if="statusDrawerType">
              <dt>Type</dt>
              <dd>{{ statusDrawerType }}</dd>
            </div>
            <div>
              <dt>Limit</dt>
              <dd>{{ statusDrawerLimit }}</dd>
            </div>
            <div>
              <dt>Rows</dt>
              <dd>{{ statusDrawerRows }}</dd>
            </div>
            <div>
              <dt>Elapsed</dt>
              <dd>{{ statusDrawerElapsed }}</dd>
            </div>
          </dl>

          <section v-if="currentStatusRow" class="current-step">
            <span class="summary-label">Current step</span>
            <strong>{{ currentStatusRow.Key }}</strong>
            <p>{{ currentStatusRow.Desc || currentStatusRow.Status || 'Waiting for status update.' }}</p>
          </section>

          <ol class="process-timeline">
            <li v-for="row in statusTimelineRows" :key="row.Key" :data-state="timelineState(row)">
              <span class="timeline-marker" aria-hidden="true"></span>
              <div>
                <strong>{{ row.Key }}</strong>
                <span>{{ row.Desc || timelineFallback(row) }}</span>
              </div>
              <time>{{ row.Elapsed || '' }}</time>
            </li>
          </ol>

          <div v-if="statusDialogRows.length > 0" class="status-table-wrap">
            <table class="status-table">
              <thead>
                <tr>
                  <th>Key</th>
                  <th>Started</th>
                  <th>Elapsed</th>
                  <th>Description</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in statusDialogRows" :key="row.Key">
                  <th scope="row">{{ row.Key }}</th>
                  <td>{{ formatStatusStarted(row.Started) }}</td>
                  <td>{{ row.Elapsed || '00:00:00' }}</td>
                  <td>{{ row.Desc || '' }}</td>
                  <td>
                    <span class="status-pill" :data-status="statusValueKey(row.Status)">
                      {{ row.Status || 'Unknown' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </Drawer>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Drawer from 'primevue/drawer'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import logoUrl from '@/assets/matrixeaselogo_grey.png'
import { getApiMessage } from '@/services/api'
import {
  fetchMatrixEaseProject,
  fetchProjectStatus,
  fetchProjects,
  uploadProject,
  type MatrixEaseColumn,
  type MatrixEaseData,
  type MatrixEaseProject,
  type MatrixEaseStatusEntry,
  type MatrixEaseStatusMap
} from '@/services/matrixease-api'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime, formatLimit, formatRows } from '@/utils/formatters'

const auth = useAuthStore()

const authMode = ref<'signin' | 'signup'>('signin')
const emailAddress = ref('')
const password = ref('')
const authBusy = ref(false)
const authError = ref('')
const authNotice = ref('')
const projects = ref<MatrixEaseProject[]>([])
const projectsSnapshot = ref('')
const loadingProjects = ref(false)
const projectError = ref('')
const activeMatrixProject = ref<MatrixEaseProject | null>(null)
const matrixData = ref<MatrixEaseData | null>(null)
const matrixName = ref('')
const matrixLoading = ref(false)
const matrixError = ref('')
const selectedMatrixColumnName = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const selectedFile = ref<File | null>(null)
const uploadName = ref('')
const sheetType = ref<'csv' | 'excel'>('csv')
const csvSeparator = ref('comma')
const headerRow = ref(1)
const headerRows = ref(1)
const maxRows = ref(0)
const ignoreCols = ref('')
const ignoreBlankRows = ref(true)
const ignoreTextCase = ref(true)
const trimLeadingWhitespace = ref(true)
const trimTrailingWhitespace = ref(true)
const uploadBusy = ref(false)
const uploadProgress = ref(0)
const uploadError = ref('')
const uploadNotice = ref('')
const uploadStatusText = ref('')
const currentUploadStatusKey = ref('')
const currentUploadStatusData = ref<MatrixEaseStatusMap | null>(null)
const statusDialogVisible = ref(false)
const statusDialogTitle = ref('Processing details')
const statusDialogKey = ref('')
const statusDialogData = ref<MatrixEaseStatusMap | null>(null)
const statusDrawerContext = ref<StatusDrawerContext | null>(null)
const statusDialogMessage = ref('')
const statusDialogError = ref('')
const statusDialogLoading = ref(false)
let uploadStatusTimer: number | undefined
let statusDetailsTimer: number | undefined

type StatusRow = MatrixEaseStatusEntry & {
  Key: string
}

type LoadProjectsOptions = {
  showLoading?: boolean
}

type StatusDrawerContext = {
  title: string
  source?: string
  sheetType?: string
  maxRows?: number
  totalRows?: number | null
  status?: string
  isPending?: boolean
}

type MatrixEaseColumnView = MatrixEaseColumn & {
  Name: string
}

const statusDialogRows = computed(() => toStatusRows(statusDialogData.value))
const statusTimelineRows = computed(() => toStatusRowsWithPlaceholders(statusDialogData.value))
const currentStatusRow = computed(() => {
  const rows = statusDialogRows.value
  const active = rows.find((row) => ['Running', 'Started', 'Starting'].includes(row.Status ?? ''))
  const failed = rows.find((row) => row.Status === 'Failed')
  const latest = [...rows].reverse().find((row) => row.Desc || row.Status)

  return active || failed || latest || null
})
const statusDrawerStatus = computed(() => {
  const contextStatus = statusDrawerContext.value?.status
  const status = currentStatusRow.value?.Status || contextStatus || 'Pending'

  if (statusDrawerContext.value?.isPending && ['Queued', 'Started', 'Starting'].includes(status) === false) {
    return status || 'Pending'
  }

  return status
})
const statusDrawerSource = computed(() => statusDrawerContext.value?.source || '')
const statusDrawerType = computed(() => statusDrawerContext.value?.sheetType || '')
const statusDrawerLimit = computed(() => formatLimit(statusDrawerContext.value?.maxRows ?? 0))
const statusDrawerRows = computed(() => formatRows(statusDrawerContext.value?.totalRows ?? null))
const statusDrawerElapsed = computed(() => currentStatusRow.value?.Elapsed || '00:00:00')
const matrixColumns = computed<MatrixEaseColumnView[]>(() =>
  Object.entries(matrixData.value?.Columns ?? {})
    .map(([name, column]) => ({
      Name: name,
      ...column
    }))
    .sort((left, right) => left.Index - right.Index)
)
const selectedMatrixColumn = computed(() => {
  if (matrixColumns.value.length === 0) {
    return null
  }

  return matrixColumns.value.find((column) => column.Name === selectedMatrixColumnName.value) ?? matrixColumns.value[0]
})
const selectedColumnValues = computed(() => selectedMatrixColumn.value?.Values?.slice(0, 100) ?? [])
const topColumnValue = computed(() => selectedMatrixColumn.value?.Values?.[0] ?? null)

function toggleAuthMode(): void {
  authError.value = ''
  authNotice.value = ''
  authMode.value = authMode.value === 'signin' ? 'signup' : 'signin'
}

async function submitAuth(): Promise<void> {
  authBusy.value = true
  authError.value = ''
  authNotice.value = ''

  try {
    const hasSession =
      authMode.value === 'signin'
        ? await auth.signIn(emailAddress.value, password.value)
        : await auth.signUp(emailAddress.value, password.value)

    if (!hasSession) {
      authNotice.value = 'Check your email to finish account setup.'
      return
    }

    await loadProjects({ showLoading: false })
  } catch (error) {
    authError.value = getApiMessage(error, 'Authentication failed.')
  } finally {
    authBusy.value = false
  }
}

async function loadProjects(options: LoadProjectsOptions = {}): Promise<void> {
  if (!auth.isAuthenticated) {
    return
  }

  const showLoading = options.showLoading ?? true
  if (showLoading) {
    loadingProjects.value = true
  }

  projectError.value = ''

  try {
    const response = await fetchProjects()
    if (!response.Success) {
      projectError.value = response.Message || 'Could not load projects.'
      projects.value = []
      projectsSnapshot.value = ''
      return
    }

    setProjects(response.Projects)
  } catch (error) {
    projectError.value = getApiMessage(error, 'Could not load projects.')
    projects.value = []
    projectsSnapshot.value = ''
  } finally {
    if (showLoading) {
      loadingProjects.value = false
    }
  }
}

function setProjects(nextProjects: MatrixEaseProject[]): void {
  const nextSnapshot = JSON.stringify(
    nextProjects.map((project) => ({
      ProjectId: project.ProjectId,
      Name: project.Name,
      OriginalName: project.OriginalName,
      SheetType: project.SheetType,
      Created: project.Created,
      MaxRows: project.MaxRows,
      TotalRows: project.TotalRows,
      Status: project.Status,
      IsPending: project.IsPending
    }))
  )

  if (nextSnapshot === projectsSnapshot.value) {
    return
  }

  projects.value = nextProjects
  projectsSnapshot.value = nextSnapshot
}

async function openProjectMatrix(project: MatrixEaseProject): Promise<void> {
  activeMatrixProject.value = project
  matrixData.value = null
  matrixName.value = project.Name
  matrixError.value = ''
  matrixLoading.value = true
  selectedMatrixColumnName.value = ''

  try {
    const response = await fetchMatrixEaseProject(project.ProjectId)
    if (response.Success === false || !response.MangaData) {
      matrixError.value = response.Message || 'Could not load MatrixEase data.'
      return
    }

    matrixName.value = response.MangaName || project.Name
    matrixData.value = response.MangaData
    selectedMatrixColumnName.value = matrixColumns.value[0]?.Name ?? ''
  } catch (error) {
    matrixError.value = getApiMessage(error, 'Could not load MatrixEase data.')
  } finally {
    matrixLoading.value = false
  }
}

function closeMatrix(): void {
  activeMatrixProject.value = null
  matrixData.value = null
  matrixName.value = ''
  matrixError.value = ''
  matrixLoading.value = false
  selectedMatrixColumnName.value = ''
}

function canOpenProject(project: MatrixEaseProject): boolean {
  return project.IsPending === false && statusKey(project) === 'complete'
}

function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '0.00%'
  }

  return `${value.toFixed(2)}%`
}

function formatDecimal(value: number | null | undefined, digits = 4): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '0'
  }

  return value.toFixed(digits)
}

function matrixBucketLabel(column: MatrixEaseColumnView): string {
  if (!column.Bucketized) {
    return 'Native'
  }

  if (column.OnlyBuckets) {
    return 'Buckets only'
  }

  return 'Bucketized'
}

function handleFileChange(event: Event): void {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] ?? null
  selectedFile.value = file

  if (file && !uploadName.value) {
    uploadName.value = file.name.replace(/\.[^.]+$/, '')
  }
}

function validateUploadForm(): boolean {
  uploadError.value = ''
  uploadNotice.value = ''

  if (!uploadName.value) {
    uploadError.value = 'Project name is required.'
    return false
  }

  if (!selectedFile.value) {
    uploadError.value = 'A file is required.'
    return false
  }

  if (headerRow.value < 0 || headerRows.value < 0 || maxRows.value < 0) {
    uploadError.value = 'Row values cannot be negative.'
    return false
  }

  if (headerRow.value === 0 && headerRows.value > 0) {
    uploadError.value = 'Header on row must be greater than zero when header rows are set.'
    return false
  }

  if (headerRow.value > headerRows.value) {
    uploadError.value = 'Header on row cannot be greater than header rows.'
    return false
  }

  return true
}

async function submitUpload(): Promise<void> {
  if (!validateUploadForm() || !selectedFile.value) {
    return
  }

  clearUploadStatusPolling()
  uploadBusy.value = true
  uploadProgress.value = 0
  uploadStatusText.value = ''

  try {
    const response = await uploadProject(
      {
        file: selectedFile.value,
        mangaName: uploadName.value,
        headerRow: headerRow.value,
        headerRows: headerRows.value,
        maxRows: maxRows.value,
        ignoreBlankRows: ignoreBlankRows.value,
        ignoreTextCase: ignoreTextCase.value,
        trimLeadingWhitespace: trimLeadingWhitespace.value,
        trimTrailingWhitespace: trimTrailingWhitespace.value,
        ignoreCols: ignoreCols.value,
        sheetType: sheetType.value,
        csvSeparator: csvSeparator.value,
        csvQuote: 'doublequote',
        csvEscape: 'doublequote',
        csvNull: 'null',
        csvEol: 'crlf'
      },
      (percent) => {
        uploadProgress.value = percent
      }
    )

    if (!response.Success) {
      uploadError.value = response.Error || response.Message || 'Upload failed.'
      return
    }

    uploadProgress.value = 100
    uploadNotice.value = 'Upload queued.'
    currentUploadStatusKey.value = response.MatrixId || ''
    currentUploadStatusData.value = response.StatusData || null
    uploadStatusText.value = summarizeStatus(response.StatusData) || 'Processing'
    await loadProjects({ showLoading: false })

    if (response.MatrixId) {
      startUploadStatusPolling(response.MatrixId)
    }
  } catch (error) {
    uploadError.value = getApiMessage(error, 'Upload failed.')
  } finally {
    uploadBusy.value = false
  }
}

function startUploadStatusPolling(statusKey: string): void {
  clearUploadStatusPolling()
  uploadStatusTimer = window.setInterval(() => {
    void pollUploadStatus(statusKey)
  }, 2500)
  void pollUploadStatus(statusKey)
}

async function pollUploadStatus(statusKey: string): Promise<void> {
  try {
    const response = await fetchProjectStatus(statusKey)
    if (!response.Success) {
      uploadError.value = response.Message || 'Could not load upload status.'
      clearUploadStatusPolling()
      return
    }

    if (response.Complete) {
      uploadNotice.value = response.Message || 'Project ready.'
      uploadStatusText.value = ''
      uploadProgress.value = 0
      currentUploadStatusKey.value = ''
      currentUploadStatusData.value = null
      clearUploadStatusPolling()
      resetUploadForm(false)
      await loadProjects({ showLoading: false })
      return
    }

    currentUploadStatusData.value = response.StatusData || null
    uploadStatusText.value = summarizeStatus(response.StatusData) || 'Processing'
    if (statusDialogKey.value === statusKey) {
      statusDialogData.value = response.StatusData || null
      statusDialogMessage.value = ''
    }
    await loadProjects({ showLoading: false })
  } catch (error) {
    uploadError.value = getApiMessage(error, 'Could not load upload status.')
    clearUploadStatusPolling()
  }
}

function summarizeStatus(statusData: MatrixEaseStatusMap | null | undefined): string {
  const rows = toStatusRows(statusData)
  const running = rows.find((step) => ['Running', 'Started', 'Starting'].includes(step.Status ?? '') && step.Desc)
  const latest = [...rows].reverse().find((step) => step.Desc)

  return running?.Desc || latest?.Desc || ''
}

function toStatusRows(statusData: MatrixEaseStatusMap | null | undefined): StatusRow[] {
  if (!statusData || typeof statusData !== 'object') {
    return []
  }

  return Object.entries(statusData)
    .map(([key, value]) => ({
      Key: key,
      ...value
    }))
    .sort(sortStatusRows)
}

function toStatusRowsWithPlaceholders(statusData: MatrixEaseStatusMap | null | undefined): StatusRow[] {
  const rowsByKey = new Map(toStatusRows(statusData).map((row) => [row.Key, row]))
  const keys = ['PreProcess', 'Queued', 'Processing', 'Analyzing', 'Saving', 'Complete', 'Failed']

  return keys
    .filter((key) => key !== 'Failed' || rowsByKey.has(key))
    .map((key) => rowsByKey.get(key) ?? { Key: key })
}

function sortStatusRows(left: StatusRow, right: StatusRow): number {
  const sortOrder = ['PreProcess', 'Queued', 'Processing', 'Analyzing', 'Saving', 'Complete', 'Failed']
  const leftIndex = sortOrder.indexOf(left.Key)
  const rightIndex = sortOrder.indexOf(right.Key)

  if (leftIndex === -1 && rightIndex === -1) {
    return left.Key.localeCompare(right.Key)
  }
  if (leftIndex === -1) {
    return 1
  }
  if (rightIndex === -1) {
    return -1
  }

  return leftIndex - rightIndex
}

function timelineState(row: StatusRow): string {
  const status = row.Status || ''

  if (status === 'Complete') {
    return 'complete'
  }
  if (status === 'Failed') {
    return 'failed'
  }
  if (['Running', 'Started', 'Starting'].includes(status)) {
    return 'active'
  }

  return 'waiting'
}

function timelineFallback(row: StatusRow): string {
  if (row.Status) {
    return row.Status
  }

  return 'Waiting'
}

function formatStatusStarted(value?: string): string {
  if (!value) {
    return ''
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString()
}

function canShowProjectStatusDetails(project: MatrixEaseProject): boolean {
  return project.IsPending || ['pending', 'running', 'started', 'starting'].includes(statusKey(project))
}

function openCurrentUploadStatusDetails(): void {
  if (!currentUploadStatusKey.value) {
    return
  }

  openStatusDetails(
    currentUploadStatusKey.value,
    uploadName.value || 'Upload',
    currentUploadStatusData.value,
    {
      title: uploadName.value || 'Upload',
      source: selectedFile.value?.name,
      sheetType: sheetType.value,
      maxRows: maxRows.value,
      totalRows: null,
      status: uploadStatusText.value ? 'Running' : 'Pending',
      isPending: true
    }
  )
}

function openProjectStatusDetails(project: MatrixEaseProject): void {
  const title = project.Name || project.OriginalName || 'Processing details'
  openStatusDetails(project.ProjectId, title, null, {
    title,
    source: project.OriginalName,
    sheetType: project.SheetType,
    maxRows: project.MaxRows,
    totalRows: project.TotalRows,
    status: project.Status || (project.IsPending ? 'Pending' : 'Unknown'),
    isPending: project.IsPending
  })
}

function openStatusDetails(
  statusKey: string,
  title: string,
  initialStatusData?: MatrixEaseStatusMap | null,
  context?: StatusDrawerContext
): void {
  clearStatusDetailsPolling()
  statusDialogKey.value = statusKey
  statusDialogTitle.value = title
  statusDrawerContext.value = context || { title }
  statusDialogData.value = initialStatusData || null
  statusDialogMessage.value = ''
  statusDialogError.value = ''
  statusDialogVisible.value = true

  void refreshStatusDetails()
  statusDetailsTimer = window.setInterval(() => {
    void refreshStatusDetails()
  }, 2500)
}

async function refreshStatusDetails(): Promise<void> {
  if (!statusDialogKey.value) {
    return
  }

  statusDialogLoading.value = true
  try {
    const response = await fetchProjectStatus(statusDialogKey.value)
    if (!response.Success) {
      if (response.Complete) {
        statusDrawerContext.value = {
          ...(statusDrawerContext.value ?? { title: statusDialogTitle.value }),
          status: 'Failed'
        }
        statusDialogError.value = response.Message || 'Processing failed.'
        clearStatusDetailsPolling()
        await loadProjects({ showLoading: false })
        return
      }

      statusDialogError.value = response.Message || 'Could not load processing status.'
      clearStatusDetailsPolling()
      return
    }

    if (response.StatusData) {
      statusDialogData.value = response.StatusData
    }

    if (response.Complete) {
      statusDialogMessage.value = response.Message || 'Processing complete.'
      clearStatusDetailsPolling()
      await loadProjects({ showLoading: false })
    } else {
      statusDialogMessage.value = ''
    }
  } catch (error) {
    statusDialogError.value = getApiMessage(error, 'Could not load processing status.')
    clearStatusDetailsPolling()
  } finally {
    statusDialogLoading.value = false
  }
}

function closeStatusDialog(): void {
  statusDialogVisible.value = false
  statusDialogKey.value = ''
  statusDialogData.value = null
  statusDrawerContext.value = null
  statusDialogMessage.value = ''
  statusDialogError.value = ''
  clearStatusDetailsPolling()
}

function resetUploadForm(clearMessages = true): void {
  selectedFile.value = null
  uploadName.value = ''
  sheetType.value = 'csv'
  csvSeparator.value = 'comma'
  headerRow.value = 1
  headerRows.value = 1
  maxRows.value = 0
  ignoreCols.value = ''
  ignoreBlankRows.value = true
  ignoreTextCase.value = true
  trimLeadingWhitespace.value = true
  trimTrailingWhitespace.value = true
  uploadProgress.value = 0
  uploadStatusText.value = ''
  currentUploadStatusKey.value = ''
  currentUploadStatusData.value = null
  clearUploadStatusPolling()

  if (clearMessages) {
    uploadError.value = ''
    uploadNotice.value = ''
  }

  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

function clearUploadStatusPolling(): void {
  if (uploadStatusTimer !== undefined) {
    window.clearInterval(uploadStatusTimer)
    uploadStatusTimer = undefined
  }
}

function clearStatusDetailsPolling(): void {
  if (statusDetailsTimer !== undefined) {
    window.clearInterval(statusDetailsTimer)
    statusDetailsTimer = undefined
  }
}

function statusKey(project: MatrixEaseProject): string {
  if (project.IsPending) {
    return 'pending'
  }

  return (project.Status || '').trim().toLowerCase()
}

function statusValueKey(status: string | undefined): string {
  return (status || '').trim().toLowerCase()
}

function signOut(): void {
  clearUploadStatusPolling()
  closeStatusDialog()
  closeMatrix()
  auth.clearSession()
  projects.value = []
  projectsSnapshot.value = ''
}

onMounted(loadProjects)
onBeforeUnmount(() => {
  clearUploadStatusPolling()
  clearStatusDetailsPolling()
})
</script>
