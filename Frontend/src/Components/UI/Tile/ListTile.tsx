import { defineComponent, Fragment, type VNode, type VNodeChild } from "vue";
import { cn } from "@/lib/utils";
import Tile from "./Tile";

const ListTile = defineComponent({
	props: {
		class: {
			type: String,
			required: false,
		},
	},

	setup(props, { slots }) {
		return () => {
			const raw = slots.default?.() ?? [];

			const children = flatten(raw);

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

export default ListTile;

function flatten(vnodes: VNodeChild[]): VNode[] {
	const result: VNode[] = [];

	vnodes.forEach((vnode) => {
		if (vnode == null || typeof vnode === "boolean") return;

		if (typeof vnode === "object" && "type" in vnode) {
			const node = vnode as VNode;

			if (node.type === Fragment) {
				const children = (node.children ?? []) as VNodeChild[];
				result.push(...flatten(children));
			} else if (node.type !== Text && node.type !== Comment) {
				result.push(node);
			}
		}
	});

	return result;
}
