<script setup lang="ts">
import Card from 'primevue/card'

defineProps<{
  label: string
  value: string
  caption: string
}>()
</script>

<template>
  <Card class="stat-surface">
    <p class="stat-surface__label">{{ label }}</p>
    <p class="stat-surface__value">{{ value }}</p>
    <p class="stat-surface__caption">{{ caption }}</p>
  </Card>
</template>
