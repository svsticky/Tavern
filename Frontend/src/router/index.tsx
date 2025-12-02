import HomePage from "@/Views/HomePage";
import MainLayout from "@/Layouts/MainLayout.vue";
import { createRouter, createWebHistory } from "vue-router";

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
