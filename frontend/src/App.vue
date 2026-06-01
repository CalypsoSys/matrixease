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
            <p class="brand-subtitle">{{ auth.email || 'Signed in' }}</p>
          </div>
        </div>
        <div class="flex gap-2">
          <Button icon="pi pi-refresh" label="Refresh" severity="secondary" :loading="loadingProjects" @click="loadProjects" />
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
            </DataTable>
          </div>
        </section>
      </div>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import logoUrl from '@/assets/matrixeaselogo_grey.png'
import { getApiMessage } from '@/services/api'
import {
  fetchProjectStatus,
  fetchProjects,
  uploadProject,
  type MatrixEaseProject
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
let uploadStatusTimer: number | undefined

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

    await loadProjects()
  } catch (error) {
    authError.value = getApiMessage(error, 'Authentication failed.')
  } finally {
    authBusy.value = false
  }
}

async function loadProjects(): Promise<void> {
  if (!auth.isAuthenticated) {
    return
  }

  loadingProjects.value = true
  projectError.value = ''

  try {
    const response = await fetchProjects()
    if (!response.Success) {
      projectError.value = response.Message || 'Could not load projects.'
      projects.value = []
      return
    }

    projects.value = response.Projects
  } catch (error) {
    projectError.value = getApiMessage(error, 'Could not load projects.')
    projects.value = []
  } finally {
    loadingProjects.value = false
  }
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
    uploadStatusText.value = summarizeStatus(response.StatusData) || 'Processing'
    await loadProjects()

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
      clearUploadStatusPolling()
      resetUploadForm(false)
      await loadProjects()
      return
    }

    uploadStatusText.value = summarizeStatus(response.StatusData) || 'Processing'
    await loadProjects()
  } catch (error) {
    uploadError.value = getApiMessage(error, 'Could not load upload status.')
    clearUploadStatusPolling()
  }
}

function summarizeStatus(statusData: unknown): string {
  if (!statusData || typeof statusData !== 'object') {
    return ''
  }

  const steps = Object.values(statusData as Record<string, { Desc?: string; Status?: string }>)
  const running = steps.find((step) => ['Running', 'Started', 'Starting'].includes(step.Status ?? '') && step.Desc)
  const latest = steps.reverse().find((step) => step.Desc)

  return running?.Desc || latest?.Desc || ''
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

function statusKey(project: MatrixEaseProject): string {
  if (project.IsPending) {
    return 'pending'
  }

  return (project.Status || '').trim().toLowerCase()
}

function signOut(): void {
  clearUploadStatusPolling()
  auth.clearSession()
  projects.value = []
}

onMounted(loadProjects)
onBeforeUnmount(clearUploadStatusPolling)
</script>
