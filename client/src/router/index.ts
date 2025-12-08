import { createRouter, createWebHistory } from 'vue-router'
import StudentsView from '../views/StudentsView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'students',
      component: StudentsView,
    },
  ],
})

export default router
