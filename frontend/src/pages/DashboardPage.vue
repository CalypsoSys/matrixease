<script setup lang="ts">
import AppShell from '@/layouts/AppShell.vue'
import AuthPanel from '@/components/AuthPanel.vue'
import ProjectCatalogCard from '@/components/ProjectCatalogCard.vue'
import StatSurface from '@/components/StatSurface.vue'
import UploadPanel from '@/components/UploadPanel.vue'
import { useDashboard } from '@/composables/useDashboard'

const dashboard = useDashboard()
const {
  runtime,
  form,
  loading,
  loadingProjects,
  uploading,
  checkingGoogle,
  uploadProgress,
  projects,
  errorMessage,
  statusMessage,
  authBusy,
  emailAddress,
  captchaPrompt,
  captchaAnswer,
  emailCode,
  readyForCatalog,
  validationErrors,
  cookiesAccepted,
  hasAccessToken,
  googleSignedIn
} = dashboard
</script>

<template>
  <AppShell
    title="Workspace Dashboard"
    subtitle="Upload source sheets, monitor processing, and reopen prior matrix projects from one routed SPA shell."
  >
    <div class="dashboard-grid">
      <section class="dashboard-grid dashboard-grid--stats">
        <StatSurface
          label="Catalog Access"
          :value="readyForCatalog ? 'Ready' : 'Pending'"
          :caption="readyForCatalog ? 'Session and access cookie are available.' : 'Accept cookies and authenticate to unlock the catalog.'"
        />
        <StatSurface
          label="Projects"
          :value="String(projects.length)"
          :caption="loadingProjects ? 'Refreshing catalog from the API.' : 'Projects returned from the current session catalog.'"
        />
        <StatSurface
          label="Platform"
          :value="runtime.platform"
          :caption="runtime.embeddedApi ? 'Embedded API mode is enabled.' : 'Using remote API runtime settings.'"
        />
      </section>

      <AuthPanel
        :cookies-accepted="cookiesAccepted"
        :has-access="hasAccessToken"
        :google-signed-in="googleSignedIn"
        :email-address="emailAddress"
        :captcha-prompt="captchaPrompt"
        :captcha-answer="captchaAnswer"
        :email-code="emailCode"
        :busy="authBusy"
        :error-message="!hasAccessToken ? errorMessage : ''"
        :status-message="!hasAccessToken ? statusMessage : ''"
        @accept-cookies="dashboard.acceptCookies"
        @update-email-address="dashboard.updateEmailAddress"
        @update-captcha-answer="dashboard.updateCaptchaAnswer"
        @update-email-code="dashboard.updateEmailCode"
        @send-email-code="dashboard.sendEmailCode"
        @validate-email-code="dashboard.validateEmailCode"
        @google-login="dashboard.startGoogleLogin"
      />

      <UploadPanel
        v-model="form"
        :busy="uploading || checkingGoogle"
        :checking-google="checkingGoogle"
        :error-message="errorMessage"
        :status-message="statusMessage"
        :upload-progress="uploadProgress"
        :validation-errors="validationErrors"
        :cookies-accepted="cookiesAccepted"
        :has-access="hasAccessToken"
        @accept-cookies="dashboard.acceptCookies"
        @file-selected="dashboard.onFileSelected"
        @upload="dashboard.submitUpload"
        @google-sheet="dashboard.submitGoogleSheet"
      />

      <ProjectCatalogCard
        :busy="loading || loadingProjects"
        :projects="projects"
        :error-message="errorMessage"
        @open-project="dashboard.navigateToViewer"
        @reload="dashboard.loadProjects"
      />
    </div>
  </AppShell>
</template>
