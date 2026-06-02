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
      </div>

      <Dialog v-model:visible="statusDialogVisible" modal :header="statusDialogTitle" class="status-dialog" @hide="closeStatusDialog">
        <div class="status-dialog-body">
          <div v-if="statusDialogMessage" class="message-success">{{ statusDialogMessage }}</div>
          <div v-if="statusDialogError" class="message-error">{{ statusDialogError }}</div>
          <div v-if="statusDialogLoading && statusDialogRows.length === 0" class="muted">Loading status...</div>

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
      </Dialog>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import logoUrl from '@/assets/matrixeaselogo_grey.png'
import { getApiMessage } from '@/services/api'
import {
  fetchProjectStatus,
  fetchProjects,
  uploadProject,
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

const statusDialogRows = computed(() => toStatusRows(statusDialogData.value))

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

  const sortOrder = ['PreProcess', 'Queued', 'Processing', 'Analyzing', 'Saving', 'Complete', 'Failed']

  return Object.entries(statusData)
    .map(([key, value]) => ({
      Key: key,
      ...value
    }))
    .sort((left, right) => {
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
    })
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

  openStatusDetails(currentUploadStatusKey.value, uploadName.value || 'Upload', currentUploadStatusData.value)
}

function openProjectStatusDetails(project: MatrixEaseProject): void {
  openStatusDetails(project.ProjectId, project.Name || project.OriginalName || 'Processing details')
}

function openStatusDetails(statusKey: string, title: string, initialStatusData?: MatrixEaseStatusMap | null): void {
  clearStatusDetailsPolling()
  statusDialogKey.value = statusKey
  statusDialogTitle.value = `${title} processing details`
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
