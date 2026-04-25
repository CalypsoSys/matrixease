<script setup lang="ts">
import AppSidebar from '@/components/AppSidebar.vue'

defineProps<{
  title: string
  subtitle: string
}>()
</script>

<template>
  <div class="app-shell">
    <AppSidebar />

    <main class="app-shell__main">
      <header class="page-header">
        <div>
          <p class="page-header__eyebrow">MatrixEase</p>
          <h1>{{ title }}</h1>
          <p>{{ subtitle }}</p>
        </div>
      </header>

      <section class="page-content">
        <slot />
      </section>
    </main>
  </div>
</template>
