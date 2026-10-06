import { baseOptions } from "@/app/layout.config";
import { docsSource } from "@/lib/source";
import { pruneTree } from "@/lib/prune-tree";
import { DocsLayout } from "fumadocs-ui/layouts/docs";
import { type ReactNode } from "react";

// Lives under [[...slug]] so it receives the current page and can trim the sidebar to it
export default async function Layout(props: {
    children: ReactNode;
    params: Promise<{ slug?: string[] }>;
}) {
    const params = await props.params;
    const url = docsSource.getPage(params.slug)?.url ?? "";

    return (
        <DocsLayout
            tree={pruneTree(docsSource.pageTree, url)}
            // Prefetching every visible sidebar link fetched a full page payload per link
            sidebar={{ prefetch: false }}
            {...baseOptions}
        >
            {props.children}
        </DocsLayout>
    );
}
