import { docsSource, openapi } from "@/lib/source";

import { APIPage } from "fumadocs-openapi/ui";
import defaultMdxComponents from "fumadocs-ui/mdx";
import {
    DocsBody,
    DocsDescription,
    DocsPage,
    DocsTitle,
} from "fumadocs-ui/page";
import { notFound } from "next/navigation";

export default async function Page(props: {
    params: Promise<{ slug?: string[] }>;
}) {
    const params = await props.params;
    const page = docsSource.getPage(params.slug);
    if (!page) notFound();

    const MDX = page.data.body;

    // @ts-ignore
    const ApiPageLayout = (openapi as any).APIPage;

    return (
        <DocsPage toc={page.data.toc} full={page.data.full}>
            <DocsTitle>{page.data.title}</DocsTitle>
            <DocsDescription>{page.data.description}</DocsDescription>
            <DocsBody>
                <MDX
                    components={{
                        ...defaultMdxComponents,
                        APIPage: (props: any) => (
                            <APIPage {...props} disablePlayground />
                        ),
                    }}
                />
            </DocsBody>
        </DocsPage>
    );
}

// Generated reference sections (TypeDoc/docfx/OpenAPI) contain well over a thousand pages,
// and every prerendered page embeds the full sidebar tree, which made the build slow and
// the image several GB. Those pages are rendered on first request and cached instead.
const GENERATED_SECTIONS = ["Backend", "Frontend", "API"];

// Render pages not returned by generateStaticParams on demand, cached after the first request.
export const dynamicParams = true;

export async function generateStaticParams() {
    return docsSource
        .generateParams()
        .filter(({ slug }) => !GENERATED_SECTIONS.includes(slug[0]));
}

export async function generateMetadata(props: {
    params: Promise<{ slug?: string[] }>;
}) {
    const params = await props.params;
    const page = docsSource.getPage(params.slug);
    if (!page) notFound();

    return {
        title: page.data.title,
        description: page.data.description,
    };
}
