import { defineComponent } from "vue";
import { cn } from "@/lib/tailwind.utils";

export default defineComponent({
  name: "Tile",
  props: {
    class: {
      type: String,
      required: false,
    },
  },

  setup(props, { slots }) {
    return () => (
      <div class={cn("box-border rounded-2xl p-5", props.class)}>
        {slots.default?.()}
      </div>
    );
  },
});
