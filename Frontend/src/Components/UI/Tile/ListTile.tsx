import { defineComponent } from "vue";
import { flattenChildren } from "@/lib/component.utils";
import { cn } from "@/lib/tailwind.utils";
import Tile from "./Tile";

export default defineComponent({
  name: "ListTile",
  props: {
    class: {
      type: String,
      required: false,
    },
  },

  setup(props, { slots }) {
    return () => {
      // Flatten the slot children
      const raw = slots.default?.() ?? [];
      const children = flattenChildren(raw);

      // Insert dividers between children
      const childrenWithDividers = children.flatMap((child, index) => {
        if (index === children.length - 1) return [child];
        return [child, <div class="h-[0.5px] w-full bg-gray-200 my-2"></div>];
      });

      return (
        <Tile class={cn("rounded-xl border border-gray-200 p-0", props.class)}>
          <div class="flex flex-col">{childrenWithDividers}</div>
        </Tile>
      );
    };
  },
});
