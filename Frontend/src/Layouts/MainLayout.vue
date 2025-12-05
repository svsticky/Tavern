<script setup lang="ts">
import {
  Bell,
  CalendarDays,
  LayoutDashboard,
  SquareArrowOutUpRight,
} from "lucide-vue-next";
import { useI18n } from "vue-i18n";
import Menubar from "@/Components/UI/Menubar/Menubar";

const { t } = useI18n();

// Define menu items for the Menubar
const menuItems = [
  { label: t("dashboard"), url: "/", icon: LayoutDashboard },
  { label: t("activities"), url: "/Activities", icon: CalendarDays },
  { label: t("announcements"), url: "/Announcements", icon: Bell },
  {
    label: t("external_links"),
    url: "/ExternalLinks",
    icon: SquareArrowOutUpRight,
  },
];

// Define profile options for the Menubar
const profileOptions = {
  username: "JohnDoe",
  avatarUrl: "https://cdn.nos.nl/image/2017/07/16/403534/xxl.jpg",
  options: [
    {
      label: "Settings",
      action: () => {
        alert("Navigating to Settings");
      },
    },
    {
      label: "Logout",
      action: () => {
        alert("Logging out");
      },
    },
  ],
};
</script>

<template>
    <div class="min-w-[415px]">
        <Menubar title='Koala' :items="menuItems" :profileOptions="profileOptions" />

        <router-view class="px-[10%] py-5" />
    </div>
</template>