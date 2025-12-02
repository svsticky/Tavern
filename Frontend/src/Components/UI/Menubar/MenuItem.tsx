import { defineComponent, type PropType } from "vue";
import type { MenuItem } from "./Types/MenuItem";
import { cn } from "@/lib/utils";

export default defineComponent({
  name: "MenubarItem",
  props: {
    item: { type: Object as PropType<MenuItem>, required: true },
    onClick: { type: Function as PropType<() => void>, required: false },
  },

  setup(props) {
    // Destructure icon component
    const IconComponent = props.item.icon;

    const isActive = window.location.pathname === props.item.url;

    const handleClick = () => {
      props.onClick?.();
    };

    return () => (
      <a
        href={props.item.url}
        class={cn("flex items-center gap-2 text-white font-bold no-underline transition-colors duration-200 ease-in-out border-2 border-transparent",
          // Mobile style
          "py-2 px-3 rounded-lg text-lg h-auto w-full justify-start",
          // Desktop style
          "lg:py-5 lg:px-2 lg:rounded-xl lg:text-sm lg:h-4 lg:w-auto lg:justify-center",
          // Hover & Active style
          "hover:bg-(--theme-460) hover:border-white/20 " + (isActive ? "bg-(--theme-460) border-white/20" : "")
        )}
        onClick={handleClick}
      >
        {IconComponent && <IconComponent />}
        <span>{props.item.label}</span>
      </a>
    );
  },
});
