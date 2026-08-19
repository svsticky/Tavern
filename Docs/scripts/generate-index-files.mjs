import fs from "fs";
import path from "path";

export function generateIndexFiles(
    baseDir,
    subtitle = null,
    recursive = false,
) {
    // Find all folders
    const folders = fs
        .readdirSync(baseDir, { withFileTypes: true })
        .filter((dirent) => dirent.isDirectory())
        .map((dirent) => dirent.name)
        .sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));

    // Find all mdx files that are not index.mdx in the folder
    const files = fs
        .readdirSync(baseDir)
        .filter((f) => f.endsWith(".mdx") && f !== "index.mdx");

    if (folders.length === 0 && files.length === 0) return;

    // This index.mdx renders AT this folder's own route (e.g. a folder "answer.util"
    // becomes the page /docs/Frontend/util/answer.util), so its own name is never part
    // of a relative link's starting point the way a regular page's filename would be.
    // Without re-adding it here, the browser resolves a plain "./child" link one level
    // too high (it drops this folder's own name instead of descending into it).
    const currentFolderName = path.basename(baseDir);

    // Create links for the index
    const folderLinks = folders
        .map((f) => `- [${f}](./${currentFolderName}/${f})`)
        .join("\n");
    const fileLinks = files
        .map(
            (f) =>
                `- [${f.replace(".mdx", "")}](./${currentFolderName}/${f.replace(".mdx", "")})`,
        )
        .join("\n");
    const links =
        folderLinks + (folderLinks && fileLinks ? "\n" : "") + fileLinks;

    // Create an index.mdx file for the Backend
    const indexContent = `---
title: ${path.basename(baseDir).charAt(0).toUpperCase() + path.basename(baseDir).slice(1)}
---
${subtitle ? `## ${subtitle}` : ""}
${links}
`;

    fs.writeFileSync(path.join(baseDir, "index.mdx"), indexContent);

    if (recursive) {
        folders.forEach((folder) => {
            generateIndexFiles(path.join(baseDir, folder), null, true);
        });
    }
}
