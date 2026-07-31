import { readdir, readFile, writeFile, rename, rm } from "fs/promises";
import * as OpenAPI from "fumadocs-openapi";
import path from "path";
import { generateIndexFiles } from "./generate-index-files.mjs";

const out = "./content/Docs/API";
const swaggerFilePath = "./swagger.json";

// Clean generated files
await rm(out, { recursive: true, force: true });

// Fetch swagger.json, sanitize media types, and generate documentation
async function main() {
    console.log("Generating API documentation...");

    // Clean swagger.json to remove non-standard media types unsupported by Fumadocs
    try {
        const swaggerRaw = await readFile(swaggerFilePath, "utf-8");
        const swaggerJson = JSON.parse(swaggerRaw);
        
        function sanitizeObject(obj) {
            if (!obj || typeof obj !== "object") return;
            if (obj.content && typeof obj.content === "object") {
                const allowedTypes = ["application/json", "application/x-www-form-urlencoded", "multipart/form-data", "application/octet-stream", "image/png", "image/jpeg", "image/webp", "image/gif"];
                for (const mediaType of Object.keys(obj.content)) {
                    if (!allowedTypes.includes(mediaType)) {
                        delete obj.content[mediaType];
                    }
                }
                // If stripping left content empty, provide application/json fallback so Fumadocs doesn't crash on missing media type
                if (Object.keys(obj.content).length === 0) {
                    obj.content["application/json"] = { schema: { type: "object" } };
                }
            }
            for (const key of Object.keys(obj)) {
                if (typeof obj[key] === "object") {
                    sanitizeObject(obj[key]);
                }
            }
        }
        
        sanitizeObject(swaggerJson);
        await writeFile(swaggerFilePath, JSON.stringify(swaggerJson, null, 2), "utf-8");
    } catch (e) {
        console.warn("Could not sanitize swagger.json media types:", e);
    }
    
    const xmlSummaries = {};

    await OpenAPI.generateFiles({
        input: [swaggerFilePath],
        output: out,
        per: "operation",
        groupBy: "tag",
        transform: (operation) => {
            const opId = operation.operationId || `${operation.method}-${operation.path}`;
            xmlSummaries[opId.toLowerCase()] = operation.summary || "";

            const method = operation.method.toUpperCase();
            const cleanTitle = operation.operationId 
                ? operation.operationId.replace(/([A-Z])/g, ' $1').trim() 
                : "Endpoint";

            operation.summary = `${method} - ${cleanTitle}`;
            operation.slug = `${operation.tags?.[0] || "default"}-${operation.operationId || operation.method}`.toLowerCase();

            if (process.env.ApiUrl) {
                operation.servers = [{ url: process.env.ApiUrl, description: "API Gateway" }];
            }

            return operation;
        },
    });

    await flattenMDXFiles(out);
    
    await forceCleanFrontmatter(out, xmlSummaries);

    await renameControllerFolderNames(out);
    generateIndexFiles(out, "Controllers", true);

    console.log("API documentation generated successfully!");
}

async function forceCleanFrontmatter(baseDir, xmlSummaries) {
    const controllers = (await readdir(baseDir, { withFileTypes: true })).filter(d => d.isDirectory());

    for (const controller of controllers) {
        const controllerPath = path.join(baseDir, controller.name);
        const files = await readdir(controllerPath);

        for (const file of files) {
            if (!file.endsWith(".mdx")) continue;
            
            const filePath = path.join(controllerPath, file);
            let content = await readFile(filePath, "utf-8");

            const matchedOpId = Object.keys(xmlSummaries).find(id => 
                file.toLowerCase().includes(id.toLowerCase())
            );

            const xmlDescription = matchedOpId ? xmlSummaries[matchedOpId] : "";

            content = content.replace(/^---[\s\S]*?---/, "");

            let cleanTitle = file.replace(".mdx", "");
            
            if (cleanTitle.endsWith("-get")) cleanTitle = "GET - " + cleanTitle.replace("-get", "");
            if (cleanTitle.endsWith("-post")) cleanTitle = "POST - " + cleanTitle.replace("-post", "");
            if (cleanTitle.endsWith("-put")) cleanTitle = "PUT - " + cleanTitle.replace("-put", "");
            if (cleanTitle.endsWith("-delete")) cleanTitle = "DELETE - " + cleanTitle.replace("-delete", "");
            if (cleanTitle.endsWith("-patch")) cleanTitle = "PATCH - " + cleanTitle.replace("-patch", "");

            cleanTitle = cleanTitle
                .split("-")
                .map(word => word.charAt(0).toUpperCase() + word.slice(1))
                .join(" ");

            const newFrontmatter = `---\ntitle: "${cleanTitle}"\n---\n\n${xmlDescription}\n\n`;

            await writeFile(filePath, newFrontmatter + content, "utf-8");
        }
    }
}

// Function for moving mdx files all the way up to their controller folder (Recursief)
async function flattenMDXFiles(baseDir) {
    const controllers = (await readdir(baseDir, { withFileTypes: true })).filter((dirent) => dirent.isDirectory());

    for (const controller of controllers) {
        const controllerPath = path.join(baseDir, controller.name);

        async function getFilesRecursively(dir) {
            const dirents = await readdir(dir, { withFileTypes: true });
            const files = await Promise.all(dirents.map((dirent) => {
                const res = path.resolve(dir, dirent.name);
                return dirent.isDirectory() ? getFilesRecursively(res) : res;
            }));
            return files.flat();
        }

        const allFiles = await getFilesRecursively(controllerPath);
        const mdxFiles = allFiles.filter(file => file.endsWith('.mdx'));

        for (const oldPath of mdxFiles) {
            const relativePath = path.relative(controllerPath, oldPath);
            const pathParts = relativePath.split(path.sep);
            const fileName = pathParts.pop(); 
            
            let newFileName = "";
            if (pathParts.length > 0) {
                newFileName = `${pathParts.join('-')}-${fileName}`.toLowerCase();
            } else {
                newFileName = fileName;
            }

            if (!newFileName.endsWith(".mdx")) {
                newFileName += ".mdx";
            }

            const newPath = path.join(controllerPath, newFileName);
            await rename(oldPath, newPath);
        }

        const subDirs = (await readdir(controllerPath, { withFileTypes: true })).filter(dirent => dirent.isDirectory());
        for (const subDir of subDirs) {
            const subDirPath = path.join(controllerPath, subDir.name);
            await rm(subDirPath, { recursive: true, force: true });
        }
    }
}

// Function for fixing controller folder names
async function renameControllerFolderNames(baseDir) {
    const controllers = (await readdir(baseDir, { withFileTypes: true })).filter((dirent) => dirent.isDirectory());

    for (const controller of controllers) {
        const newName = controller.name
            .split("-")
            .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
            .join("");

        await rename(path.join(baseDir, controller.name), path.join(baseDir, newName));
    }
}

main().catch((error) => {
    console.error("Error generating documentation:", error);
    process.exit(1);
});