import { exec } from "child_process";
import { readdirSync, unlinkSync } from "fs";
import fs from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";
import { generateIndexFiles } from "./generate-index-files.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const frontendDir = path.resolve(__dirname, "../../Frontend");
const typedocOutDir = path.resolve(__dirname, "../typedoc-files");
const finalDocsDir = path.resolve(__dirname, "../content/Docs/Frontend");

console.log("✅ Step 1: Generating TypeDoc...");
await fs.rm(typedocOutDir, { recursive: true, force: true });
await new Promise((resolve, reject) => {
    // Execute typedoc using the configured typedoc.json in the frontend directory
    exec("npx typedoc", { cwd: frontendDir }, (err, stdout, stderr) => {
        if (err) {
            console.error("❌ TypeDoc error:", stderr);
            return reject(err);
        }
        console.log(stdout);
        resolve();
    });
});

// Recursive conversion from .md -> .mdx + linkfix
async function convertMarkdownToMdx(srcDir, destDir) {
    const entries = await fs.readdir(srcDir, { withFileTypes: true });

    for (const entry of entries) {
        const srcPath = path.join(srcDir, entry.name);
        const isMd = entry.isFile() && entry.name.endsWith(".md");

        if (entry.isDirectory()) {
            await convertMarkdownToMdx(srcPath, path.join(destDir, entry.name));
        } else if (isMd) {
            const rawName =
                entry.name === "README.md"
                    ? path.basename(srcDir) === "typedoc-files"
                        ? "Frontend"
                        : path.basename(srcDir)
                    : entry.name.replace(".md", "");
            const title = rawName
                ? rawName.charAt(0).toUpperCase() + rawName.slice(1)
                : "Untitled";

            let content = await fs.readFile(srcPath, "utf-8");

            content = content
                .replace(/\]\(([^)]+)\.md\)/g, "]($1)") // Remove .md from links
                .replace(/\/README(?:\.md)?/g, "") // Remove /README.md from links
                .replace(/\/\(([^)]+)\)/g, "/$1"); // Fix links with parentheses

            // Convert standard markdown code blocks to MDX CodeBlock components
            // Added explicit newlines to isolate the JSX block from markdown paragraphs
            content = content.replace(
                /```(\w+)\n([\s\S]*?)```/g,
                (_match, lang, code) => {
                    // Escape backticks inside code blocks
                    const escapedCode = code.replace(/`/g, "\\`").replace(/\$/g, "\\$");

                    return `\n\n<CodeBlock language="${lang}">\n{\n\`${escapedCode}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert function signatures to CodeBlock
            content = content.replace(
                /^>\s*\*\*(\w+)\*\*\(([^)]*)\):\s*(.+)$/gm,
                (_match, funcName, params, returnTypeRaw) => {
                    // Remove inline backticks
                    const cleanReturnType = returnTypeRaw.replace(/`/g, "");

                    // Remove backticks from params as well (just in case)
                    const cleanParams = params.replace(/`/g, "");

                    const code = `function ${funcName}(${cleanParams}): ${cleanReturnType}`;

                    return `\n\n<CodeBlock language="ts">\n{\n\`${code}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert type alias definitions with generics to CodeBlock
            content = content.replace(
                /^>\s*\*\*(\w+)\*\*\\<`?([^>`\s]+)`?\\>\s*=\s*`([^`]+)`/gm,
                (_match, aliasName, typeParam, typeValue) => {
                    const code = `type ${aliasName}<${typeParam}> = ${typeValue}`;
                    return `\n\n<CodeBlock language="ts">\n{\n\`${code}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert type alias declarations to CodeBlock
            // Added explicit layout structure to prevent paragraph nesting errors in Turbopack
            content = content.replace(
                /^>\s*\*\*(\w+)\*\*\s*=\s*(.+)$/gm,
                (_match, typeName, typeRaw) => {
                    let cleanType = typeRaw
                        .replace(/`/g, "") // remove inline backticks
                        .replace(/\*([^*]+)\*/g, "$1") // remove *italic* markers
                        .trim();

                    const code = `type ${typeName} = ${cleanType}`;

                    return `\n\n<CodeBlock language="ts">\n{\n\`${code}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert const declarations with types to CodeBlock
            content = content.replace(
                /^>\s*`const`\s+\*\*(\w+)\*\*:\s+(.+)$/gm,
                (_match, constName, typeRaw) => {
                    let cleanType = typeRaw
                        .replace(/`/g, "") // Remove backticks
                        .replace(/\*([^*]+)\*/g, "$1") // Remove *italic* from typeof etc.
                        .trim();

                    const code = `const ${constName}: ${cleanType}`;

                    return `\n\n<CodeBlock language="ts">\n{\n\`${code}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert inline code blocks to CodeBlock components
            content = content.replace(
                /^((?:\s*)`[^`\n]+`(?:<[^`]*>)?(?:\s*))+$/gm,
                (match) => {
                    // Strip backticks
                    const cleaned = match.replace(/`/g, "").trim();

                    return `\n\n<CodeBlock language="ts">\n{\n\`${cleaned}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert interface properties (including optional ones) to CodeBlock
            content = content.replace(
                /^>\s*(?:`optional`\s*)?\*\*(\w+)\*\*\s*:\s*(.+)$/gm,
                (_match, propName, typeRaw) => {
                    let cleanType = typeRaw
                        .replace(/`/g, "") // remove backticks
                        .replace(/\*([^*]+)\*/g, "$1") // remove italics
                        .trim();

                    const isOptional = /`optional`/.test(_match);
                    const name = isOptional ? `${propName}?` : propName;

                    const code = `${name}: ${cleanType}`;

                    return `\n\n<CodeBlock language="ts">\n{\n\`${code}\`\n}\n</CodeBlock>\n\n`;
                },
            );

            // Convert inline code blocks with links to CodeBlock components
            content = content.replace(/^((\s*)`[^`\n]+`.*)+$/gm, (line) => {
                // Strip backticks
                const cleaned = line.replace(/`/g, "").trim();

                // Wrap in CodeBlock
                return `\n\n<CodeBlock language="ts">\n{\n\`${cleaned}\`\n}\n</CodeBlock>\n\n`;
            });

            // Convert blockquotes to styled divs
            content = content.replace(/^>?\s*Note:(.*)$/gm, (_, noteText) => {
                return `
<div className="my-4 p-4 border-l-4 border-blue-400 bg-blue-50 text-blue-800 rounded-md">
  💡${noteText.trim()}
</div>`;
            });

            // Convert warnings to styled divs
            content = content.replace(
                /^>?\s*Warning:(.*)$/gm,
                (_, warnText) => {
                    return `  
<div className="my-4 p-4 border-l-4 border-red-400 bg-red-50 text-red-800 rounded-md">
  🚨${warnText.trim()}
</div>`;
                },
            );

            // Add import statement for CodeBlock if it contains <CodeBlock>
            const importCodeBlock = `import { CodeBlock } from '@/components/code-block'\n\n`;
            if (content.includes("<CodeBlock")) {
                content = importCodeBlock + content;
            }

            // replace folder links with [...something] to something
            content = content
                .split("\n")
                .map((line) => {
                    if (line.includes("Defined in:")) {
                        return line;
                    } else {
                        return line.replace(/\/\\?\[\.\.\.(.+?)\\?\]/g, "/$1");
                    }
                })
                .join("\n");

            const frontmatter = `---\ntitle: ${title}\nsidebar_position: 1\n---\n\n`;

            const destName = entry.name
                .replace(".md", ".mdx")
                .replace("README.mdx", "index.mdx");

            // Fix links to point to the correct file if it is an index file (as an index file hasn't his name in the path already)
            if (destName === "index.mdx") {
                content = content
                    .replace(/\(([^()\/]+\/)/g, `(./${rawName}/$1`) // Add title to links
                    .split("\n")
                    .map((line) => {
                        if (line.includes("Defined in:")) {
                            return line;
                        } else {
                            return line.replace(/\[\.\.\.(.+?)\]/g, "$1"); // Fix links with [...something] to /something
                        }
                    })
                    .join("\n");
            }

            const destPath = path
                .join(destDir, destName)
                .replace(/\/\(([^)]+)\)/g, "/$1")
                .replace(/\\\(([^)]+)\)/g, "\\$1")
                .split("\n")
                .map((line) => {
                    if (line.includes("Defined in:")) {
                        return line;
                    } else {
                        return line.replace(/\[\.\.\.(.+?)\]/g, "$1"); // Fix path with [...something] to /something
                    }
                })
                .join("\n");

            await fs.mkdir(path.dirname(destPath), { recursive: true });
            await fs.writeFile(destPath, frontmatter + content, "utf-8");
        }
    }
}

console.log("✅ Step 2: Convert all .md files to .mdx...");
await fs.rm(finalDocsDir, { recursive: true, force: true });
await convertMarkdownToMdx(typedocOutDir, finalDocsDir);

console.log("✅ Step 3: Create index files...");
// Delete index files generated by typedoc
removeIndexFiles(finalDocsDir);

// Clean the mdx files
await cleanMdxFiles(finalDocsDir);

// Generate own index files
generateIndexFiles(finalDocsDir, null, true);

console.log("🎉 Done! Result files can be found in:", finalDocsDir);

function removeIndexFiles(dir) {
    const entries = readdirSync(dir, { withFileTypes: true });

    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);

        if (entry.isDirectory()) {
            // Recursive if it is a directory
            removeIndexFiles(fullPath);
        } else if (entry.isFile() && entry.name === "index.mdx") {
            // Remove index.mdx
            unlinkSync(fullPath);
        }
    }
}

// Cleans the part between frontmatter and start of file, except from imports
async function cleanMdxFiles(dir) {
    const entries = await fs.readdir(dir, { withFileTypes: true });

    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);

        if (entry.isDirectory()) {
            // If directory, clean all .mdx files within
            await cleanMdxFiles(path.join(dir, entry.name));
        } else if (entry.isFile() && entry.name.endsWith(".mdx")) {
            let content = await fs.readFile(fullPath, "utf-8");

            // Match frontmatter
            const frontmatterMatch = content.match(/^---[\s\S]*?---/);
            if (!frontmatterMatch) continue; // no frontmatter → do nothing

            const frontmatter = frontmatterMatch[0];

            // Get everything after the frontmatter
            const afterFrontmatter = content.slice(frontmatter.length);

            // Split in lines
            const lines = afterFrontmatter.split("\n");

            // Collect imports
            const imports = [];
            let firstHeadingIndex = -1;

            lines.forEach((line, i) => {
                if (line.trim().startsWith("import ")) {
                    imports.push(line);
                }
                if (firstHeadingIndex === -1 && line.trim().startsWith("#")) {
                    firstHeadingIndex = i;
                }
            });

            if (firstHeadingIndex === -1) {
                // No "#" found → skip file
                continue;
            }

            // Build new content: frontmatter + imports + from first heading
            const cleaned =
                frontmatter +
                "\n\n" +
                (imports.length > 0 ? imports.join("\n") + "\n\n" : "") +
                lines.slice(firstHeadingIndex).join("\n");

            await fs.writeFile(fullPath, cleaned, "utf-8");
        }
    }
}