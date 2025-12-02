<script setup lang="ts">
import {
  Bell,
  CalendarDays,
  LayoutDashboard,
  SquareArrowOutUpRight,
} from "lucide-vue-next";
import Menubar from "@/Components/UI/Menubar/Menubar";

// Define menu items for the Menubar
const menuItems = [
  { label: "Dashboard", url: "/", icon: LayoutDashboard },
  { label: "Activities", url: "/Activities", icon: CalendarDays },
  { label: "Announcements", url: "/Announcements", icon: Bell },
  {
    label: "External links",
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