import { defineComponent } from "vue";
import { cn } from "@/lib/utils";

const Tile = defineComponent({
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
export default Tile;
