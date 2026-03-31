<script setup lang="ts">
const runtime = useMatrixEaseRuntime()

const architectureItems = [
  {
    title: 'Shared frontend',
    description: 'One Nuxt UI app replaces the duplicated static trees in MatrixEase.Web, MatrixEase.App, and static.matrixease.wwwroot.'
  },
  {
    title: 'API-only backend',
    description: 'MatrixEase.Web becomes the public HTTP API for api.matrixease.com instead of serving static pages.'
  },
  {
    title: 'Explicit platform config',
    description: 'Web and Electron differences move into runtime config and adapters instead of overrides.js patches.'
  }
]
</script>

<template>
  <main class="min-h-screen text-slate-900">
    <div class="mx-auto flex min-h-screen max-w-6xl flex-col gap-10 px-6 py-10 lg:px-10">
      <section class="grid gap-6 rounded-[2rem] border border-white/70 bg-white/80 p-8 shadow-2xl shadow-slate-300/40 backdrop-blur md:grid-cols-[1.6fr_1fr] md:p-10">
        <div class="space-y-6">
          <UBadge color="primary" variant="soft" size="lg">
            MatrixEase Frontend
          </UBadge>
          <div class="space-y-4">
            <h1 class="max-w-3xl text-4xl font-semibold tracking-tight text-slate-950 md:text-6xl">
              Shared Nuxt UI shell for web and Electron.
            </h1>
            <p class="max-w-2xl text-base leading-7 text-slate-600 md:text-lg">
              This project is the replacement for the duplicated legacy wwwroot trees. It will host the common Vue UI, consume the MatrixEase APIs, and switch platform behavior through runtime configuration.
            </p>
          </div>
          <div class="flex flex-wrap gap-3">
            <UButton :to="runtime.docsBase" target="_blank" color="primary">
              Docs
            </UButton>
            <UButton :to="runtime.marketingBase" target="_blank" color="neutral" variant="outline">
              Marketing Site
            </UButton>
          </div>
        </div>

        <UCard class="border-slate-200/80 bg-slate-950 text-white">
          <template #header>
            <div class="flex items-center justify-between gap-3">
              <span class="text-sm font-medium uppercase tracking-[0.3em] text-teal-200">Runtime</span>
              <UBadge :color="runtime.platform === 'electron' ? 'warning' : 'primary'" variant="soft">
                {{ runtime.platform }}
              </UBadge>
            </div>
          </template>

          <div class="space-y-4 text-sm">
            <div>
              <p class="text-slate-400">API base</p>
              <p class="break-all font-mono text-teal-200">{{ runtime.apiBase }}</p>
            </div>
            <div>
              <p class="text-slate-400">Embedded API</p>
              <p class="font-medium text-white">{{ runtime.embeddedApi ? 'enabled' : 'disabled' }}</p>
            </div>
            <div>
              <p class="text-slate-400">Migration state</p>
              <p class="font-medium text-white">Scaffolded and ready for feature-by-feature porting.</p>
            </div>
          </div>
        </UCard>
      </section>

      <section class="grid gap-4 md:grid-cols-3">
        <UCard
          v-for="item in architectureItems"
          :key="item.title"
          class="border-white/80 bg-white/75 shadow-lg shadow-slate-200/60 backdrop-blur"
        >
          <template #header>
            <h2 class="text-lg font-semibold text-slate-950">{{ item.title }}</h2>
          </template>

          <p class="leading-7 text-slate-600">
            {{ item.description }}
          </p>
        </UCard>
      </section>
    </div>
  </main>
</template>
