<script setup lang="ts">
import Button from 'primevue/button'
import Card from 'primevue/card'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import type { MatrixEaseCatalogEntry } from '@/services/api'
import { formatDate } from '@/utils/format'

defineProps<{
  busy: boolean
  projects: MatrixEaseCatalogEntry[]
  errorMessage: string
}>()

const emit = defineEmits<{
  openProject: [mxesId: string]
  reload: []
}>()

function resolveViewerId(project: MatrixEaseCatalogEntry)
{
  const candidate = project.ViewerPath || project.Url
  const trimmed = candidate.replace(/\/+$/, '')
  const lastSegment = trimmed.split('/').pop()

  return lastSegment && lastSegment.length > 0 ? lastSegment : candidate
}

const openProject = (project: MatrixEaseCatalogEntry) =>
{
  emit('openProject', resolveViewerId(project))
}
</script>

<template>
  <Card class="surface-card">
    <template #title>
      <div class="section-header">
        <div>
          <h2>Project Catalog</h2>
          <p>Existing matrix projects from the active catalog session.</p>
        </div>
        <Button label="Refresh" severity="secondary" text :loading="busy" @click="emit('reload')" />
      </div>
    </template>

    <div v-if="errorMessage" class="stack-sm">
      <Message severity="error" :closable="false">{{ errorMessage }}</Message>
    </div>

    <div v-if="projects.length === 0" class="empty-state">
      <p>{{ busy ? 'Loading project catalog...' : 'No projects are available for this session yet.' }}</p>
    </div>

    <div v-else class="catalog-list">
      <article v-for="project in projects" :key="project.Url" class="catalog-row">
        <div>
          <div class="catalog-row__title">
            <h3>{{ project.Name }}</h3>
            <Tag :value="project.Status" severity="secondary" />
          </div>
          <p>{{ project.Type }} • {{ formatDate(project.Created) }}</p>
        </div>

        <Button label="Open Viewer" @click="openProject(project)" />
      </article>
    </div>
  </Card>
</template>
