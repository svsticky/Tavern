import { createRouter, createWebHistory } from "vue-router";
import MainLayout from "@/Layouts/MainLayout.vue";
import HomePage from "@/Views/HomePage";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      component: MainLayout,
      children: [
        {
          path: "",
          name: "dashboard",
          component: HomePage,
        },
      ],
    },
  ],
});

export default router;
