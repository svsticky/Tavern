import { Fragment, type VNode, type VNodeChild } from "vue";

// Helper function to flatten VNodeChildren
export function flattenChildren(vnodes: VNodeChild[]): VNode[] {
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
        result.push(...flattenChildren(children));
      } else if (node.type !== Text && node.type !== Comment) {
        // Regular vnode, add to result
        result.push(node);
      }
    }
  });

  return result;
}
