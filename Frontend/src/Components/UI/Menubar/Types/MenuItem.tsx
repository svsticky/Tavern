import type { FunctionalComponent } from "vue";

export type MenuItem = {
    label: string;
    url: string;
    icon?: FunctionalComponent;
}