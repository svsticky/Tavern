import { createI18n } from "vue-i18n";
import enUS from "./en-US.json";
import nlNL from "./nl-NL.json";

export const i18n = createI18n({
  legacy: false,
  locale: "nl-NL",
  fallbackLocale: "en-US",
  messages: {
    "en-US": enUS,
    "nl-NL": nlNL
  }
});
