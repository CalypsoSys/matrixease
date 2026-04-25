<script setup lang="ts">
import Button from 'primevue/button'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'

defineProps<{
  cookiesAccepted: boolean
  hasAccess: boolean
  googleSignedIn: boolean
  emailAddress: string
  captchaPrompt: string
  captchaAnswer: string
  emailCode: string
  busy: boolean
  errorMessage: string
  statusMessage: string
}>()

const emit = defineEmits<{
  acceptCookies: []
  updateEmailAddress: [value: string]
  updateCaptchaAnswer: [value: string]
  updateEmailCode: [value: string]
  sendEmailCode: []
  validateEmailCode: []
  googleLogin: []
}>()
</script>

<template>
  <Card class="surface-card">
    <template #title>
      <div class="section-header">
        <div>
          <h2>Access</h2>
          <p>Legacy static flow used cookies plus either email-code or Google authentication. The SPA now uses the same API flow directly.</p>
        </div>
        <Button
          v-if="!cookiesAccepted"
          label="Accept Cookies"
          severity="secondary"
          text
          @click="emit('acceptCookies')"
        />
      </div>
    </template>

    <div class="stack-sm">
      <Message v-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>
      <Message v-if="statusMessage" severity="success" :closable="false">{{ statusMessage }}</Message>
      <Message v-if="hasAccess" severity="success" :closable="false">Catalog access is active for this session.</Message>
    </div>

    <div class="form-grid">
      <label class="field field--wide">
        <span>Email address</span>
        <InputText :model-value="emailAddress" fluid @update:model-value="emit('updateEmailAddress', String($event))" />
      </label>

      <label class="field">
        <span>{{ captchaPrompt || 'Captcha' }}</span>
        <InputText :model-value="captchaAnswer" fluid @update:model-value="emit('updateCaptchaAnswer', String($event))" />
      </label>

      <div class="field auth-actions">
        <span>&nbsp;</span>
        <Button label="Send Email Code" :disabled="busy || !cookiesAccepted" :loading="busy" @click="emit('sendEmailCode')" />
      </div>

      <label class="field">
        <span>Email code</span>
        <InputText :model-value="emailCode" fluid @update:model-value="emit('updateEmailCode', String($event))" />
      </label>

      <div class="field auth-actions">
        <span>&nbsp;</span>
        <Button label="Validate Code" severity="secondary" outlined :disabled="busy || !cookiesAccepted" :loading="busy" @click="emit('validateEmailCode')" />
      </div>
    </div>

    <div class="auth-divider">or</div>

    <div class="actions-row">
      <Button
        :label="googleSignedIn ? 'Google Connected' : 'Sign In With Google'"
        :severity="googleSignedIn ? 'secondary' : undefined"
        :outlined="googleSignedIn"
        :disabled="busy || !cookiesAccepted"
        @click="emit('googleLogin')"
      />
    </div>
  </Card>
</template>
