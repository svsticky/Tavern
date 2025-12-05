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
          meta: { title: "Koala" },
        },
      ],
    },
  ],
});

router.afterEach((to) => {
  document.title = (to.meta.title as string) || "Mijn App";
});

export default router;
