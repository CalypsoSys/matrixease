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
import { onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import logoUrl from '@/assets/matrixeaselogo_grey.png'
import { getApiMessage } from '@/services/api'
import { fetchProjects, type MatrixEaseProject } from '@/services/matrixease-api'
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

function statusKey(project: MatrixEaseProject): string {
  if (project.IsPending) {
    return 'pending'
  }

  return (project.Status || '').trim().toLowerCase()
}

function signOut(): void {
  auth.clearSession()
  projects.value = []
}

onMounted(loadProjects)
</script>
