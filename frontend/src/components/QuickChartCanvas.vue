<script setup lang="ts">
import { Chart, type ChartConfiguration, registerables } from 'chart.js'
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'

Chart.register(...registerables)

const props = defineProps<{
  chartType: string
  chartData: {
    labels: string[]
    datasets: Array<{
      label: string
      data: number[]
      backgroundColor?: string | string[]
      borderColor?: string | string[]
    }>
  }
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
let chart: Chart | null = null

const renderChart = async () =>
{
  await nextTick()

  if (!canvasRef.value) {
    return
  }

  if (chart) {
    chart.destroy()
    chart = null
  }

  const type = props.chartType === 'polarArea' ? 'polarArea' : props.chartType as ChartConfiguration['type']
  chart = new Chart(canvasRef.value, {
    type,
    data: {
      labels: props.chartData.labels,
      datasets: props.chartData.datasets.map((dataset, index) => ({
        ...dataset,
        backgroundColor: dataset.backgroundColor ?? `hsl(${(index * 75) % 360} 70% 60%)`,
        borderColor: dataset.borderColor ?? `hsl(${(index * 75) % 360} 70% 45%)`,
        borderWidth: 2
      }))
    },
    options: {
      responsive: true,
      maintainAspectRatio: false
    }
  })
}

watch(() => [props.chartType, props.chartData], () => {
  void renderChart()
}, { deep: true, immediate: true })

onBeforeUnmount(() => {
  if (chart) {
    chart.destroy()
  }
})
</script>

<template>
  <div class="chart-canvas-wrap">
    <canvas ref="canvasRef" />
  </div>
</template>
