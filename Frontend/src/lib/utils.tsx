import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { Fragment, type VNode, type VNodeChild } from "vue";

export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

type DateFormatType =
  | "fullDateTime"
  | "shortDate"
  | "monthShort"
  | "timeOnly"
  | "defaultDate";

export function formatDate(date: Date, format: DateFormatType): string {
  switch (format) {
    case "fullDateTime":
      return date.toLocaleDateString("default", {
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    case "shortDate":
      return date.toLocaleDateString("default", {
        day: "numeric",
        month: "short",
      });
    case "monthShort":
      return date.toLocaleDateString("default", {
        month: "short",
      });
    case "timeOnly":
      return date.toLocaleTimeString("default", {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    case "defaultDate":
    default:
      return date.toLocaleDateString();
  }
}

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
