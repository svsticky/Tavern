import { ArrowRight } from "lucide-vue-next";
import { defineComponent } from "vue";
import { cn } from "@/lib/tailwind.utils";

export default defineComponent({
  name: "Button",
  props: {
    class: {
      type: String,
      required: false,
    },
    showArrow: {
      type: Boolean,
      default: false,
    },
  },

  setup(props, { slots }) {
    return () => (
      <button
        type="button"
        class={cn(
          `bg-white text-(--theme) font-semibold px-6 py-2 rounded-lg  hover:bg-gray-100 transition cursor-pointer`,
          props.class,
        )}
      >
        {slots.default?.()}

        {/* Optional Arrow Icon */}
        {props.showArrow && <ArrowRight class="inline-block ml-2" size={16} />}
      </button>
    );
  },
});
