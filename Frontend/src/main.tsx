import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import "./theme-generator";

import "./style.css";
import { i18n } from "./i18n";
import images from "./lib/images";

// Set favicon
const link: HTMLLinkElement | null =
  document.querySelector("link[rel~='icon']");
if (link) {
  link.href = images.sticky_logo_head_board_color;
} else {
  const newLink = document.createElement("link");
  newLink.rel = "icon";
  newLink.href = images.sticky_logo_head_board_color;
  document.head.appendChild(newLink);
}

createApp(App).use(router).use(i18n).mount("#app");
