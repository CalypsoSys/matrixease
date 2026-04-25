<script setup lang="ts">
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import ProgressBar from 'primevue/progressbar'
import type { UploadFormState } from '@/utils/matrix'

const props = defineProps<{
  modelValue: UploadFormState
  busy: boolean
  checkingGoogle: boolean
  errorMessage: string
  statusMessage: string
  uploadProgress: number
  validationErrors: string[]
  cookiesAccepted: boolean
  hasAccess: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: UploadFormState]
  acceptCookies: []
  fileSelected: [event: Event]
  upload: []
  googleSheet: []
}>()

const updateField = <K extends keyof UploadFormState>(field: K, value: UploadFormState[K]) =>
{
  emit('update:modelValue', {
    ...props.modelValue,
    [field]: value
  })
}
</script>

<template>
  <Card class="surface-card">
    <template #title>
      <div class="section-header">
        <div>
          <h2>Upload Workflow</h2>
          <p>Thin service and state layer on top of the existing MatrixEase API.</p>
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

    <div class="form-grid">
      <label class="field">
        <span>Project name</span>
        <input
          :value="modelValue.mangaName"
          class="field-input"
          type="text"
          @input="updateField('mangaName', ($event.target as HTMLInputElement).value)"
        />
      </label>

      <label class="field">
        <span>Header row</span>
        <input
          :value="modelValue.headerRow"
          class="field-input"
          type="number"
          min="0"
          @input="updateField('headerRow', Number(($event.target as HTMLInputElement).value))"
        />
      </label>

      <label class="field">
        <span>Header rows</span>
        <input
          :value="modelValue.headerRows"
          class="field-input"
          type="number"
          min="0"
          @input="updateField('headerRows', Number(($event.target as HTMLInputElement).value))"
        />
      </label>

      <label class="field">
        <span>Max rows</span>
        <input
          :value="modelValue.maxRows"
          class="field-input"
          type="number"
          min="0"
          @input="updateField('maxRows', Number(($event.target as HTMLInputElement).value))"
        />
      </label>

      <label class="field field--wide">
        <span>Ignore columns</span>
        <input
          :value="modelValue.ignoreCols"
          class="field-input"
          type="text"
          @input="updateField('ignoreCols', ($event.target as HTMLInputElement).value)"
        />
      </label>

      <label class="field">
        <span>Sheet type</span>
        <select
          :value="modelValue.sheetType"
          class="field-input"
          @change="updateField('sheetType', ($event.target as HTMLSelectElement).value)"
        >
          <option value="csv">CSV</option>
          <option value="excel">Spreadsheet</option>
        </select>
      </label>

      <template v-if="modelValue.sheetType === 'csv'">
        <label class="field">
          <span>CSV separator</span>
          <select :value="modelValue.csvSeparator" class="field-input" @change="updateField('csvSeparator', ($event.target as HTMLSelectElement).value)">
            <option value="comma">Comma</option>
            <option value="tab">Tab</option>
            <option value="space">Space</option>
            <option value="pipe">Pipe</option>
            <option value="colon">Colon</option>
            <option value="semicolon">Semicolon</option>
          </select>
        </label>

        <label class="field">
          <span>CSV quote</span>
          <select :value="modelValue.csvQuote" class="field-input" @change="updateField('csvQuote', ($event.target as HTMLSelectElement).value)">
            <option value="doublequote">Double quote</option>
            <option value="singlequote">Single quote</option>
          </select>
        </label>

        <label class="field">
          <span>CSV escape</span>
          <select :value="modelValue.csvEscape" class="field-input" @change="updateField('csvEscape', ($event.target as HTMLSelectElement).value)">
            <option value="doublequote">Double quote</option>
            <option value="backslash">Backslash</option>
          </select>
        </label>

        <label class="field">
          <span>CSV null</span>
          <select :value="modelValue.csvNull" class="field-input" @change="updateField('csvNull', ($event.target as HTMLSelectElement).value)">
            <option value="null">null</option>
            <option value="empty">empty</option>
          </select>
        </label>

        <label class="field">
          <span>CSV line endings</span>
          <select :value="modelValue.csvEol" class="field-input" @change="updateField('csvEol', ($event.target as HTMLSelectElement).value)">
            <option value="crlf">CRLF</option>
            <option value="lf">LF</option>
          </select>
        </label>
      </template>

      <label class="field">
        <span>Google Sheet ID</span>
        <input
          :value="modelValue.sheetId"
          class="field-input"
          type="text"
          @input="updateField('sheetId', ($event.target as HTMLInputElement).value)"
        />
      </label>

      <label class="field field--wide">
        <span>Google range</span>
        <input
          :value="modelValue.range"
          class="field-input"
          type="text"
          @input="updateField('range', ($event.target as HTMLInputElement).value)"
        />
      </label>
    </div>

    <div class="checkbox-row">
      <label class="checkbox">
        <input
          :checked="modelValue.ignoreBlankRows"
          type="checkbox"
          @change="updateField('ignoreBlankRows', ($event.target as HTMLInputElement).checked)"
        />
        <span>Ignore blank rows</span>
      </label>

      <label class="checkbox">
        <input
          :checked="modelValue.ignoreTextCase"
          type="checkbox"
          @change="updateField('ignoreTextCase', ($event.target as HTMLInputElement).checked)"
        />
        <span>Ignore text case</span>
      </label>

      <label class="checkbox">
        <input
          :checked="modelValue.trimLeadingWhitespace"
          type="checkbox"
          @change="updateField('trimLeadingWhitespace', ($event.target as HTMLInputElement).checked)"
        />
        <span>Trim leading whitespace</span>
      </label>

      <label class="checkbox">
        <input
          :checked="modelValue.trimTrailingWhitespace"
          type="checkbox"
          @change="updateField('trimTrailingWhitespace', ($event.target as HTMLInputElement).checked)"
        />
        <span>Trim trailing whitespace</span>
      </label>
    </div>

    <label class="field field--wide">
      <span>Upload file</span>
      <input class="field-input" type="file" @change="emit('fileSelected', $event)" />
    </label>

    <div v-if="validationErrors.length > 0" class="stack-sm">
      <Message severity="warn" :closable="false">{{ validationErrors.join(' ') }}</Message>
    </div>

    <div v-if="errorMessage" class="stack-sm">
      <Message severity="error" :closable="false">{{ errorMessage }}</Message>
    </div>

    <div v-if="statusMessage" class="stack-sm">
      <Message severity="success" :closable="false">{{ statusMessage }}</Message>
    </div>

    <div v-if="uploadProgress > 0" class="stack-sm">
      <p class="status-copy">Upload progress: {{ uploadProgress }}%</p>
      <ProgressBar :value="uploadProgress" />
    </div>

    <div class="actions-row">
      <Button
        label="Upload file"
        :disabled="busy || !cookiesAccepted || !hasAccess"
        :loading="busy"
        @click="emit('upload')"
      />
      <Button
        label="Import Google Sheet"
        severity="secondary"
        text
        :disabled="busy || !cookiesAccepted || !hasAccess"
        :loading="checkingGoogle"
        @click="emit('googleSheet')"
      />
    </div>
  </Card>
</template>
