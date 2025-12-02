import { defineComponent, Fragment, type VNode, type VNodeChild } from "vue";
import { cn } from "@/lib/utils";
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
      const children = flatten(raw);

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

function flatten(vnodes: VNodeChild[]): VNode[] {
  const result: VNode[] = [];

  // Recursively process each vnode
  vnodes.forEach((vnode) => {
    // Skip null/boolean nodes
    if (vnode == null || typeof vnode === "boolean") return;

    if (typeof vnode === "object" && "type" in vnode) {
      const node = vnode as VNode;

      // If it's a Fragment, flatten its children
      if (node.type === Fragment) {
        const children = (node.children ?? []) as VNodeChild[];
        result.push(...flatten(children));
      } else if (node.type !== Text && node.type !== Comment) {
        // Regular vnode, add to result
        result.push(node);
      }
    }
  });

  return result;
}
