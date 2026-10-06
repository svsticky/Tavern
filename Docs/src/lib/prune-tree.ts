import type { Node, Root } from "fumadocs-core/page-tree";

// The generated reference docs contain well over a thousand pages. Sending the complete tree
// to the browser on every page made each page ~1 MB, so the sidebar only gets the folders on
// the path to the current page, plus one level of lookahead for every other folder.
// Collapsed folders link to their index page, which reveals their children once opened.

function containsUrl(node: Node, url: string): boolean {
    if (node.type === "page") return node.url === url;
    if (node.type === "folder")
        return (
            node.index?.url === url ||
            node.children.some((child) => containsUrl(child, url))
        );
    return false;
}

function pruneNode(node: Node, url: string, lookahead: boolean): Node {
    if (node.type !== "folder") return node;

    if (containsUrl(node, url))
        return {
            ...node,
            children: node.children.map((child) => pruneNode(child, url, true)),
        };

    return {
        ...node,
        children: lookahead
            ? node.children.map((child) => pruneNode(child, url, false))
            : [],
    };
}

export function pruneTree(tree: Root, url: string): Root {
    return {
        ...tree,
        children: tree.children.map((child) => pruneNode(child, url, true)),
    };
}
