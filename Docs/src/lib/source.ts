import { docs } from 'collections/server';
import { loader } from "fumadocs-core/source";
import { createOpenAPI } from "fumadocs-openapi/server";

export const docsSource = loader({
    baseUrl: "/docs",
    source: docs.toFumadocsSource(),
});

export const openapi = createOpenAPI({
    disablePlayground: true,
});
