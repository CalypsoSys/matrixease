import { createRouter, createWebHistory } from 'vue-router'
import DashboardPage from '@/pages/DashboardPage.vue'
import ViewerPage from '@/pages/ViewerPage.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: DashboardPage
    },
    {
      path: '/viewer/:mxesId',
      name: 'viewer',
      component: ViewerPage,
      props: true
    }
  ]
})
